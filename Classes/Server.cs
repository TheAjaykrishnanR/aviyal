using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class Sender
{
	Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

	static int port = 6969;
	static IPEndPoint ip = new(IPAddress.Any, port);

	public Sender()
	{
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.Bind(ip);
	}

	private bool connected = false;
	int RETRY = 1000;
	int TIMEOUT = 5000;
	bool timeout = false;

	Task connectingTask;

	private string recieverIp;
	public string RecieverIp
	{
		get
		{
			return recieverIp;
		}
		set
		{
			recieverIp = value;
			connectingTask = ConnectToReciever(IPAddress.Parse(value));
		}
	}

	public async Task ConnectToReciever(IPAddress recieverIp)
	{
		IPEndPoint reciever = new(recieverIp, Reciever.RECIEVING_PORT);

		System.Timers.Timer timer = new();
		timer.Interval = TIMEOUT;
		timer.Elapsed += (sender, e) => { timeout = true; };
		timer.Start();

		while (!connected)
		{
			if (timeout)
			{
				Console.WriteLine($"[TIMEOUT] Connection attempts timedout");
				break;
			}

			try
			{
				Console.WriteLine($"[EVENT] Connecting to {IPAddress.Parse(reciever.Address.ToString())} ...");
				await socket.ConnectAsync(reciever);
				connected = true;
				Console.WriteLine($"[EVENT] Connected !");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FAIL] {ex.Message}");
			}
			await Task.Delay(RETRY);
		}
		timer.Stop();
		timer.Close();
	}

	public async Task Send(string text)
	{
		await connectingTask;

		if (connected)
		{
			try
			{
				Console.WriteLine($"[EVENT] Sending message ...");

				byte[] bytesToSend = Encoding.UTF8.GetBytes(jsonmessage);
				await socket.SendAsync(bytesToSend);

				Console.WriteLine($"[STATE] Message Sent, wating for reply...");

				byte[] replyBytes = new byte[1024];
				await socket.ReceiveAsync(replyBytes);

			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] {ex.Message}");
			}
		}
		else
		{
			Console.WriteLine($"[EVENT] Not connected to any recievers");
		}
	}
}

public class Reciever
{
	private Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

	private IPAddress localIPAddress = Utils.GetLANIP();
	public static int RECIEVING_PORT = 4242;
	private IPEndPoint localEndPoint;

	public Reciever()
	{
		localEndPoint = new(localIPAddress, RECIEVING_PORT);
		socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		socket.Bind(localEndPoint);

		ConnectToSender();
	}

	public async Task ConnectToSender()
	{
		socket.Listen();

		Console.WriteLine($"[EVENT] Wating for connections");

		//Socket connection = await socket.AcceptAsync();
		List<Task> _ts = new();
		while (true)
		{
			var _socket = await socket.AcceptAsync();
			Console.WriteLine($"[EVENT] Connected !");
			_ts.Add(Recieve(_socket));
		}

	}

	public async Task Recieve(Socket connectedSocket)
	{
		while (true)
		{
			if (!connectedSocket.Connected)
			{
				break;
			}

			byte[] bytesRecieved = new byte[1024];
			await connectedSocket.ReceiveAsync(bytesRecieved);
		}
		connectedSocket.Close();
	}
}
