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
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }
    class WorldItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
        
    }
}
