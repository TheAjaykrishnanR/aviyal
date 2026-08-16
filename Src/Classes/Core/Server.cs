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
    public const int RECV_BUFFER_SIZE = 4096;
    const int MAX_CLIENTS = 128;

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
        socket.Listen(MAX_CLIENTS);
        Logger.Log($"Server: listening on {IPAddress.Any}:{port}");
        Task.Run(() =>
        {
            while (true)
            {
                Socket client = socket.Accept();
                lock (_listLock)
                    clients.Add(client);
                Logger.Log("Server: socket connected");
                Task.Run(() =>
                {
                    byte[] buffer = new byte[RECV_BUFFER_SIZE];
                    int bytesRead = 0;
                    /* change: client.Connected does not really reflect the current state
                     * */
                    while ((bytesRead = client.Receive(buffer)) > 0)
                    {
                        try
                        {
                            string request = Encoding.UTF8.GetString(
                                buffer.Take(bytesRead).ToArray()
                            );
                            string response = REQUEST_RECEIVED(request);
                            byte[] bytes = Encoding.UTF8.GetBytes(response);
                            client.Send(bytes);
                            Logger.Log(
                                $"Server: request recieved: {request}, response: {response}"
                            );
                            Array.Clear(buffer);
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
                    Logger.Log("Server: connection closed");
                });
            }
        });
    }

    public void Broadcast(string message)
    {
        Logger.Log($"Broadcasting to [{clients.Count}] clients...");
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
