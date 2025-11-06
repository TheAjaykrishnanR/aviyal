using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Server : IDisposable
{
    public delegate string RequestEventHandler(string request);

    private readonly List<Socket> clients = new();
    private readonly int port;
    private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public Server(Config config)
    {
        port = config.serverPort;
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
        socket.Listen(128);
        Logger.Log($"server: listening on {IPAddress.Any}:{port}");
        Task.Run(() =>
        {
            while (true)
            {
                var client = socket.Accept();
                clients.Add(client);
                Logger.Log("server: socket connected");
                Task.Run(() =>
                {
                    while (client.Connected)
                    {
                        var buffer = new byte[1024];
                        var bytesRead = client.Receive(buffer);
                        var request = Encoding.UTF8.GetString(buffer.Take(bytesRead).ToArray());
                        var response = REQUEST_RECEIVED(request);
                        var bytes = Encoding.UTF8.GetBytes(response);
                        client.Send(bytes);
                        Logger.Log($"server: request recieved: {request}, response: {response}");
                    }

                    client.Close();
                    clients.Remove(client);
                    Logger.Log("server: connection closed");
                });
            }
        });
    }

    // necessary for hot reloading (restarting)
    public void Dispose()
    {
        clients?.ForEach(client =>
        {
            client?.Close();
            client?.Dispose();
        });
        socket?.Close();
        socket?.Dispose();
    }

    public event RequestEventHandler REQUEST_RECEIVED = request => "";

    public void Broadcast(string message)
    {
        //Logger.Log($"[[[BROADCASTING TO {clients.Count}]]]");
        clients?.ForEach(client =>
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            if (client.Connected) client?.Send(bytes);
        });
    }
}