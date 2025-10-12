using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

class _
{
	[STAThread]
	static void Main(string[] args)
	{
		Application app = new();
		Window wnd = new();
		wnd.Title = "windowgen";
		wnd.Background = Utils.BrushFromHex(args[0]);
		wnd.ShowActivated = false;
		nint hWnd = new WindowInteropHelper(wnd).EnsureHandle();
		wnd.Loaded += (s, e) =>
		{
			User32.SetWindowPos(hWnd, 1, 0, 0, 0, 0, SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOACTIVATE);
		};
		bool toggled = true;
		wnd.MouseDown += (s, e) =>
		{
			if (toggled)
				wnd.Background = new SolidColorBrush(Colors.Blue);
			else
				wnd.Background = Utils.BrushFromHex(args[0]);
			toggled = !toggled;
		};
		app.Run(wnd);
	}
}
