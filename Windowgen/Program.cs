using System.Windows;
using System.Windows.Interop;

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
		app.Run(wnd);
	}
}
