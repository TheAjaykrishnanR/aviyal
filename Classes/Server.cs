/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

public class Server : IDisposable
{
	Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	int port;
	public delegate string RequestEventHandler(string request);
	public event RequestEventHandler REQUEST_RECEIVED = (request) => "";

	List<Socket> clients = new();
	public Server(Config config)
	{
		port = config.serverPort;
		socket.Bind(new IPEndPoint(IPAddress.Any, port));
		socket.Listen(128);
		Console.WriteLine($"server: listening on {IPAddress.Any}:{port}");
		Task.Run(() =>
		{
			while (true)
			{
				Socket client = socket.Accept();
				clients.Add(client);
				Console.WriteLine("server: socket connected");
				Task.Run(() =>
				{
					while (client.Connected)
					{
						byte[] buffer = new byte[1024];
						int bytesRead = client.Receive(buffer);
						string request = Encoding.UTF8.GetString(buffer.Take(bytesRead).ToArray());
						string response = REQUEST_RECEIVED(request);
						byte[] bytes = Encoding.UTF8.GetBytes(response);
						client.Send(bytes);
						Console.WriteLine($"server: request recieved: {request}, response: {response}");
					}
					client.Close();
					clients.Remove(client);
					Console.WriteLine("server: connection closed");
				});
			}
		});
	}

	public void Broadcast(string message)
	{
		//Console.WriteLine($"[[[BROADCASTING TO {clients.Count}]]]");
		clients?.ForEach(client =>
		{
			byte[] bytes = Encoding.UTF8.GetBytes(message);
			if (client.Connected) client?.Send(bytes);
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
}
