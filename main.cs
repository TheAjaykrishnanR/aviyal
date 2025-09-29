using System;
using System.Runtime.InteropServices;

class _Main
{
	[DllImport("user32.dll", SetLastError = true)]
	public static extern int ShowWindow(nint hWnd, SHOWWINDOW nCmdShow);

	static void Main(string[] args)
	{
		nint hWnd = (nint)Convert.ToInt64(args[0]);
		if (Convert.ToInt32(args[1]) == 0) ShowWindow(hWnd, SHOWWINDOW.SW_HIDE);
		else ShowWindow(hWnd, SHOWWINDOW.SW_SHOWNORMAL);
	}
}

public enum SHOWWINDOW
{
	SW_HIDE = 0,
	SW_SHOWNORMAL = 1,
	SW_MAXIMIZE = 3,
	SW_SHOW = 5
}
