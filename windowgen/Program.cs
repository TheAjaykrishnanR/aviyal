using System.Windows;

class _
{
	[STAThread]
	static void Main(string[] args)
	{
		Application app = new();
		Window wnd = new();
		wnd.Title = "WindowGen";
		wnd.Background = Utils.BrushFromHex(args[0]);
		app.Run(wnd);
	}
}
