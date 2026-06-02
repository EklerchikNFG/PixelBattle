using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using Common;
using System.Runtime.CompilerServices;

namespace ClientPB
{
    public partial class Form1 : Form
    {
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private string _clientid;
        private int _currentWorldId = -1;
        private WorldState _currentWorld;
        private int _selectedColor = 1;
        private int _cellSize = 10;
        private DateTime _lastUpdate = DateTime.MinValue;
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
        public void HighlightSelectedColor(int index)
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

        private async Task btnConnect_Click(object sender, EventArgs e)
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
                _clientid = hello.Split('|')[1];
                lblStatus.Text = $"Подключение к {serverIP}.ID:{_clientid}";
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
            if (worldList.SelectedIndex == null)
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

            if (_currentWorld?.Pixels== null) return;

            for (int x = 0; x < _currentWorld.Width; x++)
            {
                for (int y = 0; y < _currentWorld.Height; y++)
                {
                    var color = ColorPalette.GetColor(_currentWorld.Pixels[x, y]);
                    using var brush = new SolidBrush(color);
                    e.Graphics.FillRectangle(brush, x* _cellSize, y * _cellSize, _cellSize, _cellSize);
                }
            }
        }
    }
}
    

