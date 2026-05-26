using System.Net.Sockets;
using System.Text;
using Common;

namespace serverPB;

public class ClientHandler
{
    private TcpClient _tcpClient;
    private GameServer _server;
    private StreamReader _reader;
    private StreamWriter _writer;
    public string ClientId { get; private set; }
    public int CurrentWorldId { get; set; } = -1;

    public ClientHandler(TcpClient client, GameServer server)
    {
        _tcpClient = client;
        _server = server;
        ClientId = Guid.NewGuid().ToString()[..8];
        var stream = client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public async void Handle(object state)
    {
        try
        {
            _writer.WriteLine($"HELLO|{ClientId}");
            string line;
            while ((line = await _reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"[Client {ClientId}] {line}");
                ProcessCommand(line);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Client {ClientId}] Ошибка: {ex.Message}");
        }
        finally
        {
            _server.RemoveClient(this);
            _tcpClient.Close();
        }
    }

    private void ProcessCommand(string cmd)
    {
        var parts = cmd.Split('|');
        switch (parts[0])
        {
            case ClientCommands.CreateWorld:
                // var (created, result) = _server.HandleCreateWorld(ClientId, parts[1], int.Parse(parts[2]), int.Parse(parts[3]));
                // _writer.WriteLine(created ? $"{ServerCommands.WorldState}|{result}" : $"{ServerCommands.Error}|{result}");
                _writer.WriteLine($"{ServerCommands.Error}|Создание миров временно отключено. Используйте Demo Field");
                break;

            case ClientCommands.JoinWorld:
                var world = _server.JoinWorld(ClientId, int.Parse(parts[1]));
                if (world == null)
                    _writer.WriteLine($"{ServerCommands.Error}|Мир не найден");
                else
                {
                    CurrentWorldId = world.Id;
                    _server.SendWorldState(this, world);
                }
                break;

            case ClientCommands.PlacePixel:
                var (success, error) = _server.TryPlacePixel(ClientId, int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]));
                _writer.WriteLine(success ? $"{ServerCommands.PixelPlaced}|OK" : $"{ServerCommands.Error}|{error}");
                break;

            case ClientCommands.ListWorlds:
                _server.SendWorldList(this);
                break;
        }
    }

    public void SendMessage(string message)
    {
        try { _writer.WriteLine(message); }
        catch { }
    }
}