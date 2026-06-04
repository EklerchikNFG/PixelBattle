using System.Net;
using serverPB;

var server = new GameServer();

Console.WriteLine("Сервер запущен. Доступные IP для подключения:");
foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
{
    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        Console.WriteLine($" {ip}");
}

server.Start();
