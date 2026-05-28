using System.Net.Sockets;
using Common;

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
        private int _cellsize = 10;
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
    class WorldItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;

        }
    }
}
