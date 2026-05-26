using System.Net;
using System.Net.Sockets;
using System.Text;
using PixelBattle.Common;

namespace PixelBattle.Server;

public class GameServer
{
    private TcpListener _listener;
    private List<ClientHandler> _clients = new();
    private Dictionary<int, World> _worlds = new();
    private int _nextWorldId = 1;
    private readonly object _lock = new();

    public void Start(int port = 8888)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        Console.WriteLine($"[Server] Запущен на порту {port}");

        CreateDemoWorld();

        while (true)
        {
            var client = _listener.AcceptTcpClient();
            var handler = new ClientHandler(client, this);
            lock (_lock) _clients.Add(handler);

            // Запускаем в отдельной задаче с небольшой задержкой
            Task.Run(async () =>
            {
                await Task.Delay(50);  // Даём время на инициализацию
                handler.Handle(null);
            });
        }
    }

    private void CreateDemoWorld()
    {
        var demo = new World(_nextWorldId++, "Demo Field", 50, 50);
        _worlds[demo.Id] = demo;
        Console.WriteLine($"[Server] Создан демо-мир: {demo.Name} (ID: {demo.Id}, {demo.Width}x{demo.Height})");
    }

    public (bool success, string error) HandleCreateWorld(string clientId, string name, int width, int height)
    {
        if (width < 10 || width > 200 || height < 10 || height > 200)
            return (false, "Размер должен быть 10-200");

        lock (_lock)
        {
            var world = new World(_nextWorldId++, name, width, height);
            _worlds[world.Id] = world;
            Console.WriteLine($"[Server] {clientId} создал мир {world.Id}: {name}");
            return (true, world.Id.ToString());
        }
    }

    public World JoinWorld(string clientId, int worldId)
    {
        lock (_lock)
        {
            if (!_worlds.TryGetValue(worldId, out var world))
                return null;

            // Добавляем игрока в список, если его там ещё нет
            if (!world.Players.Contains(clientId))
                world.Players.Add(clientId);

            Console.WriteLine($"[Server] {clientId} присоединился к миру {worldId}");
            return world;
        }
    }

    public (bool success, string error) TryPlacePixel(string clientId, int worldId, int x, int y, int colorIndex)
    {
        lock (_lock)
        {
            if (!_worlds.TryGetValue(worldId, out var world))
                return (false, "Мир не найден");

            // Проверка границ
            if (x < 0 || x >= world.Width || y < 0 || y >= world.Height)
                return (false, "За пределами холста");

            // Проверка цвета
            if (colorIndex < 0 || colorIndex >= ColorPalette.Colors.Length)
                return (false, "Неверный цвет");

            // Проверка cooldown (3 секунды)
            var now = DateTime.UtcNow;
            if (world.LastPixelTime.TryGetValue(clientId, out var lastTime))
            {
                if ((now - lastTime).TotalSeconds < 3)
                    return (false, $"Подождите {3 - (now - lastTime).TotalSeconds:F1} секунд");
            }

            // Ставим пиксель
            world.Pixels[x, y] = (byte)colorIndex;
            world.LastPixelTime[clientId] = now;

            // Обновляем счёт
            world.PlayerScores[clientId] = world.PlayerScores.GetValueOrDefault(clientId) + 1;

            Console.WriteLine($"[Server] {clientId} поставил пиксель в ({x},{y}) цвет {colorIndex}");

            // Рассылаем обновление всем в этом мире
            BroadcastPixelUpdate(worldId, clientId, x, y, colorIndex);

            return (true, "OK");
        }
    }

    private void BroadcastPixelUpdate(int worldId, string senderId, int x, int y, int colorIndex)
    {
        var message = $"{ServerCommands.PixelPlaced}|{worldId}|{x}|{y}|{colorIndex}";
        lock (_lock)
        {
            foreach (var client in _clients)
            {
                if (client.CurrentWorldId == worldId && client.ClientId != senderId)
                    client.SendMessage(message);
            }
        }
    }

    public void SendWorldState(ClientHandler client, World world)
    {
        var sb = new StringBuilder();
        sb.Append($"{ServerCommands.WorldState}|{world.Id}|{world.Name}|{world.Width}|{world.Height}|");

        // Сериализуем пиксели построчно
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                sb.Append(world.Pixels[x, y]);
            }
        }
        client.SendMessage(sb.ToString());
    }

    public void SendWorldList(ClientHandler client)
    {
        lock (_lock)
        {
            if (_worlds.Count == 0)
            {
                client.SendMessage($"{ServerCommands.WorldList}|");
                return;
            }

            var worldsStrings = new List<string>();
            foreach (var world in _worlds.Values)
            {
                worldsStrings.Add($"{world.Id}|{world.Name}|{world.Width}x{world.Height}|{world.Players.Count}");
            }

            var combined = string.Join(";", worldsStrings);
            var message = $"{ServerCommands.WorldList}|{combined}";

            Console.WriteLine($"[Server] Отправляю список миров: {message}");
            client.SendMessage(message);
        }
    }

    public void RemoveClient(ClientHandler client)
    {
        lock (_lock)
        {
            _clients.Remove(client);
        }
    }
}