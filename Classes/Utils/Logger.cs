/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.IO;

public class Logger
{
	public static bool DEBUG = true;
	public static bool CONSOLE = true;
	public static bool FILE = true;

	public static FileStream logFile = File.Open(Paths.logFile, FileMode.OpenOrCreate);
	public static StreamWriter logFileWriter = new(logFile);

	public static void Log(string? text, Exception? ex = null, bool debug = true, bool console = true, bool file = true)
	{
		if (ex != null) text += $"\n{ex.Message}" + $"\n{ex.StackTrace}" + $"\n{ex?.InnerException?.StackTrace}";
		if (DEBUG && debug) Debug.WriteLine(text);
		if (CONSOLE && console) Console.WriteLine(text);
		if (FILE && file) logFileWriter.WriteLine(text);
	}

	public static void Log(List<string> array)
	{
		foreach (var arr in array) Log(arr);
	}
}

public class LoggerWindow
{
	Window? wnd;
	FrameworkElement? content;
	TextBlock? debugConsole;
	bool initialized = false;
	public LoggerWindow(Type? contentType = null, dynamic[]? contentConstructorArgs = null, Action<Window>? wndInit = null)
	{
		// window runs on a separate thread so that UI heavy updates dont
		// slow down the main UI thread.
		Thread thread = new(() =>
		{
			wnd = new();
			wnd.Width = 800;
			wnd.Height = 400;

			//wnd.WindowStyle = WindowStyle.None;
			//wnd.AllowsTransparency = true;
			//wnd.Background = new SolidColorBrush(Colors.Transparent);
			wndInit?.Invoke(wnd);

			Grid grid = new();
			RowDefinition _row1 = new() { Height = new GridLength(1, GridUnitType.Star) };
			RowDefinition _row2 = new() { Height = new GridLength(0.25, GridUnitType.Star) };

			grid.RowDefinitions.Add(_row1);
			grid.RowDefinitions.Add(_row2);

			if (contentType != null)
			{
				// all this so that this windows content UIElement can be created in this thread
				content = (FrameworkElement?)Activator.CreateInstance(contentType, contentConstructorArgs);
				content!.Height = 3 * wnd.Height / 4;
				content!.Width = wnd.Width;
				wnd.SizeChanged += (s, e) =>
				{
					content.Height = 3 * e.NewSize.Height / 4;
					content.Width = e.NewSize.Width;
				};
				Grid.SetRow(content, 0);
				grid.Children.Add(content);
			}

			debugConsole = new();
			ScrollViewer scrollViewer = new();
			scrollViewer.Margin = new(5);
			scrollViewer.Content = debugConsole;

			Grid.SetRow(scrollViewer, 1);
			grid.Children.Add(scrollViewer);

			wnd.Content = grid;
			wnd.Show();

			initialized = true;
			System.Windows.Threading.Dispatcher.Run();
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
	}

	// to get a reference to the window's primary content to write to it
	public FrameworkElement? GetContent()
	{
		while (!initialized) Thread.Sleep(1);
		return content;
	}

	public void Log(string message)
	{
		if (debugConsole == null) return;
		wnd?.Dispatcher.Invoke(() =>
		{
			debugConsole.Text += "\n" + message;
		});
	}

	// to set the properties of windows primary content because it is owned by a separate thread
	public void SetContentProperty(Action<FrameworkElement?> contentPropertySetterLambda)
	{
		while (!initialized) Thread.Sleep(1);
		wnd?.Dispatcher.Invoke(() =>
		{
			contentPropertySetterLambda(content);
		});
	}
}

