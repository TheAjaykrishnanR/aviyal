using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;

class _Main
{
	static void Main(string[] args)
	{
		nint hWnd = (nint)Convert.ToInt64(args[0]);
		if (Convert.ToInt32(args[1]) == 0) User32.ShowWindow(hWnd, SHOWWINDOW.SW_HIDE);
		else User32.ShowWindow(hWnd, SHOWWINDOW.SW_SHOWNORMAL);
	}
}

