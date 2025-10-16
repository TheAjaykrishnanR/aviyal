using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

class Server
{
	Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
	int port = 6969;
	public delegate string RequestEventHandler(string request);
	public event RequestEventHandler REQUEST_RECEIVED = (request) => "";

	List<Socket> clients = new();
	public Server()
	{
		socket.Bind(new IPEndPoint(IPAddress.Any, port));
		socket.Listen(10);
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
					clients.Remove(client);
					Console.WriteLine("server: connection closed");
				});
			}
		});
	}

	public void Broadcast(string message)
	{
		clients?.ForEach(client =>
		{
			Console.WriteLine("[[[BROADCASTING]]]");
			byte[] bytes = Encoding.UTF8.GetBytes(message);
			client.Send(bytes);
		});
	}
}
