using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Server : IDisposable
{
    Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    int port;
    public delegate string RequestEventHandler(string request);
    public event RequestEventHandler REQUEST_RECEIVED = (request) => "";

    List<Socket> clients = new();
    private readonly Lock _listLock = new();

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
                Socket client = socket.Accept();
                lock (_listLock)
                    clients.Add(client);
                Logger.Log("server: socket connected");
                Task.Run(() =>
                {
                    while (client.Connected)
                    {
                        try
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead = client.Receive(buffer);
                            string request = Encoding.UTF8.GetString(
                                buffer.Take(bytesRead).ToArray()
                            );
                            string response = REQUEST_RECEIVED(request);
                            byte[] bytes = Encoding.UTF8.GetBytes(response);
                            client.Send(bytes);
                            Logger.Log(
                                $"server: request recieved: {request}, response: {response}"
                            );
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Error recieving/responding to client", ex: ex);
                        }
                    }
                    lock (_listLock)
                    {
                        client.Close();
                        clients.Remove(client);
                    }
                    Logger.Log("server: connection closed");
                });
            }
        });
    }

    public void Broadcast(string message)
    {
        //Logger.Log($"[[[BROADCASTING TO {clients.Count}]]]");
        clients?.ForEach(client =>
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                if (client.Connected)
                    client?.Send(bytes);
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to broadcast to client", ex: ex);
            }
        });
    }

    // necessary for hot reloading (restarting)
    public void Dispose()
    {
        lock (_listLock)
        {
            clients?.ForEach(client =>
            {
                client?.Close();
                client?.Dispose();
            });
            socket?.Close();
            socket?.Dispose();
        }
    }
}
