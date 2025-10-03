using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

class _Main
{
	static void Main(string[] args)
	{
		//nint hWnd = (nint)Convert.ToInt64(args[0]);
		//if (Convert.ToInt32(args[1]) == 0) User32.ShowWindow(hWnd, SHOWWINDOW.SW_HIDE);
		//else User32.ShowWindow(hWnd, SHOWWINDOW.SW_SHOWNORMAL);

		List<nint>? hWnds = Utils.GetAllTaskbarWindows();
		List<Window> windows = new();
		hWnds.ForEach(hWnd =>
		{
			windows.Add(new(hWnd));
		});
		windows = windows.Where(wnd => wnd.title.Contains("windowgen")).ToList();
		windows.ForEach(wnd => Console.WriteLine($"Title: {wnd.title}, hWnd: {wnd.hWnd}"));
	}
}

public class Window
{
	public nint hWnd { get; private set; }
	public string title
	{
		get
		{
			return Utils.GetWindowTitleFromHWND(hWnd);
		}
		private set;
	}
	public string className { get; private set; }
	public string exe
	{
		get
		{
			return Utils.GetExePathFromHWND(hWnd);
		}
		private set;
	}
	public RECT rect
	{
		get
		{
			User32.GetWindowRect(hWnd, out RECT _rect);
			return _rect;

		}
		private set;
	}
	public SHOWWINDOW state
	{
		get
		{
			WINDOWPLACEMENT wndPlmnt = new();
			User32.GetWindowPlacement(hWnd, ref wndPlmnt);
			return (SHOWWINDOW)wndPlmnt.showCmd;
		}
		private set;
	}

	public void Hide() { }
	public void Show(RECT pos) { }
	public void Focus() { }
	public void Move(RECT target) { }

	public Window(nint hWnd)
	{
		this.hWnd = hWnd;
	}
}

