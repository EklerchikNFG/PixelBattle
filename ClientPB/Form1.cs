using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using Common;
//using.System.Seurity.Cryptography;


namespace ClientPB
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private string _clientId;
        private int _currentWorldId = -1;
        private WorldState _currentWorld;
        private int _selectedColor = 1;
        private int _cellSize = 10;
        private DateTime _lastPixelTime = DateTime.MinValue;
        private Dictionary<int, string> _availableWorlds = new();
        public Form1()
        {
            InitializeComponent();
            cooldownTimer.Start();



            btnColor0.Tag = 0;
            btnColor1.Tag = 1;
            btnColor2.Tag = 2;
            btnColor3.Tag = 3;
            btnColor4.Tag = 4;
            btnColor5.Tag = 5;
            btnColor6.Tag = 6;
            btnColor7.Tag = 7;

            btnColor0.Click += ColorButton_Click;
            btnColor1.Click += ColorButton_Click;
            btnColor2.Click += ColorButton_Click;
            btnColor3.Click += ColorButton_Click;
            btnColor4.Click += ColorButton_Click;
            btnColor5.Click += ColorButton_Click;
            btnColor6.Click += ColorButton_Click;
            btnColor7.Click += ColorButton_Click;

            HighlightSelectedColor(_selectedColor);
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var colorIndex = int.Parse(btn.Tag.ToString());
            _selectedColor = colorIndex;
            lblStatus.Text = $"Выбран цвет: {btn.BackColor.Name}";
            HighlightSelectedColor(colorIndex);
        }
        private void HighlightSelectedColor(int index)
        {
            Button[] colorButtons = { btnColor0, btnColor1, btnColor2, btnColor3, btnColor4, btnColor5, btnColor6, btnColor7 };

            foreach (var btn in colorButtons)
            {
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.Gray;
            }

            colorButtons[index].FlatAppearance.BorderSize = 3;
            colorButtons[index].FlatAppearance.BorderColor = Color.Yellow;
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            string serverIP = txtServerIP.Text;
            if (string.IsNullOrEmpty(serverIP)) return;
            btnConnect.Enabled = false;
            lblStatus.Text = $"Подключение к {serverIP}";

            try
            {
                _client = new TcpClient();

                await _client.ConnectAsync(serverIP, 8888);
                var stream = _client.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                var hello = await _reader.ReadLineAsync();
                _clientId = hello.Split('|')[1];
                lblStatus.Text = $"Подключение к {serverIP}.ID:{_clientId}";
                _ = Task.Run(ListenToServer);
                await RequestWorldListWithRetry();
                worldList.Enabled = true;
                btnJoin.Enabled = true;
                btnCreate.Enabled = true;
                btnRefresh.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка:{ex.Message}");
                btnConnect.Enabled = true;
            }
        }
        private async void btnJoin_Click(object sender, EventArgs e)
        {
            if (worldList.SelectedItem == null)
            {
                lblStatus.Text = "Выберите мир";
                return;
            }

            var selected = (WorldItem)worldList.SelectedItem;
            await _writer.WriteLineAsync($"{ClientCommands.JoinWorld}|{selected.Id}");
            lblStatus.Text = $"Присоединяемся к миру{selected.Name}...";
        }

        private async void btnCreate_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите название мира:",
                "Cоздать мир",
                $"World_{DateTime.Now.Ticks}");

            if (string.IsNullOrEmpty(name)) { return; }
            await _writer.WriteLineAsync($"{ClientCommands.CreateWorld}|{name}|50|50");
            lblStatus.Text = $"Создается мир{name}...";
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await _writer.WriteLineAsync(ClientCommands.ListWorlds);
            lblStatus.Text = "Обновление списка миров..";
        }

        private async Task RequestWorldListWithRetry()
        {
            for (int i = 0; i < 3; i++)
            {
                await _writer.WriteLineAsync(ClientCommands.ListWorlds);
                await Task.Delay(200);

                if (worldList.Items.Count > 0)
                    return;
            }
            lblStatus.Text = "Не удалось загрузить список миров после 3 попыток";
        }

        class WorldItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;

        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {

            if (_currentWorld?.Pixels == null) return;

            for (int x = 0; x < _currentWorld.Width; x++)
            {
                for (int y = 0; y < _currentWorld.Height; y++)
                {
                    var color = ColorPalette.GetColor(_currentWorld.Pixels[x, y]);
                    using var brush = new SolidBrush(color);
                    e.Graphics.FillRectangle(brush, x * _cellSize, y * _cellSize, _cellSize, _cellSize);
                }
            }
        }

        private async void canvas_MouseClick(object sender, MouseEventArgs e)
        {

            if (_currentWorld == null) return;
            var x = e.X / _cellSize;
            var y = e.Y / _cellSize;
            if (x >= 0 && x < _currentWorld.Width && y >= 0 && y < _currentWorld.Height)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastPixelTime).TotalSeconds < 3)
                {
                    lblStatus.Text = $"Подожди {3 - (now - _lastPixelTime).TotalSeconds:F1} ceкунд";
                    return;
                }

                await _writer.WriteLineAsync($"{ClientCommands.PlacePixel}|{_currentWorld.Id}|{x}|{y}|{_selectedColor}");

                _lastPixelTime = now;
                _currentWorld.Pixels[x, y] = (byte)_selectedColor;
                canvas.Invalidate();
            }

        }

        private void cooldownTimer_Tick(object sender, EventArgs e)
        {
            var elapsed = (DateTime.UtcNow - _lastPixelTime).TotalSeconds;
            if (elapsed < 3)
                lblCooldown.Text = $"КД: {(3 - elapsed):F1} сек";
            else
                lblCooldown.Text = "Готов к пикселю!";
        }

        private async Task ListenToServer()
        {
            try
            {
                string line;
                while ((line = await _reader.ReadLineAsync()) != null)
                {
                    this.Invoke(() => ProcessServerMessage(line));

                }
            }
            catch { }
        }

        private void ProcessServerMessage(string msg)
        {
            var parts = msg.Split('|');
            switch (parts[0])
            {
                case ServerCommands.WorldList:
                    _availableWorlds.Clear();
                    worldList.Items.Clear();

                    // Получаем всю строку после "WORLDS"
                    string worldsData = msg.Substring("WORLDS".Length + 1);

                    if (string.IsNullOrEmpty(worldsData))
                    {
                        lblStatus.Text = "Нет доступных миров";
                        break;
                    }

                    // Разделяем по точке с запятой (между мирами)
                    string[] worldEntries = worldsData.Split(';');

                    foreach (string entry in worldEntries)
                    {
                        // Каждый мир: "ID|Name|Size|Players"
                        string[] worldInfo = entry.Split('|');
                        if (worldInfo.Length >= 4)
                        {
                            int id = int.Parse(worldInfo[0]);
                            string name = worldInfo[1];
                            string size = worldInfo[2];
                            int players = int.Parse(worldInfo[3]);

                            _availableWorlds[id] = name;
                            worldList.Items.Add(new WorldItem { Id = id, Name = $"{name} [{size}, игроков: {players}]" });
                        }
                        else
                        {
                            Console.WriteLine($"Ошибка парсинга мира: {entry}");
                        }
                    }

                    lblStatus.Text = $"Загружено миров: {worldList.Items.Count}";
                    break;
                case ServerCommands.WorldState:
                    _currentWorldId = int.Parse(parts[1]);
                    _currentWorld = new WorldState
                    {
                        Id = _currentWorldId,
                        Name = parts[2],
                        Width = int.Parse(parts[3]),
                        Height = int.Parse(parts[4])
                    };
                    // Парсинг пикселя (строка длиной width*height)
                    var pixelData = parts[5];
                    _currentWorld.Pixels = new byte[_currentWorld.Width, _currentWorld.Height];
                    for (int y = 0; y < _currentWorld.Height; y++)
                    {
                        for (int x = 0; x < _currentWorld.Width; x++)
                        {
                            _currentWorld.Pixels[x, y] = byte.Parse(pixelData[y * _currentWorld.Width + x].ToString());
                        }
                    }
                    this.Text = $"Pixel Battle - {_currentWorld.Name} ({_currentWorld.Width}x{_currentWorld.Height})";
                    canvas.Invalidate();
                    break;
                case ServerCommands.PixelPlaced:
                    if (parts.Length >= 5 && int.Parse(parts[1]) == _currentWorldId)
                    {
                        int x = int.Parse(parts[2]);
                        int y = int.Parse(parts[3]);
                        int color = int.Parse(parts[4]);
                        if (_currentWorld?.Pixels != null && x >= 0 && x < _currentWorld.Width && y < _currentWorld.Height)
                        {
                            _currentWorld.Pixels[x, y] = (byte)color;
                            canvas.Invalidate();
                        }
                    }
                    break;
                case ServerCommands.Error:
                    lblStatus.Text = $"ошибка :{parts[1]}";
                    break;
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
        }

    }

}

    

