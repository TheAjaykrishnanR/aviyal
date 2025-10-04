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
		WindowManager wm = new();
	}
}

public class Window : IWindow
{
	public nint hWnd { get; set; }
	public string title
	{
		get
		{
			return Utils.GetWindowTitleFromHWND(this.hWnd);
		}
		set;
	}
	public string className { get; set; }
	public string exe
	{
		get
		{
			return Utils.GetExePathFromHWND(this.hWnd);
		}
		set;
	}
	public RECT rect
	{
		get
		{
			User32.GetWindowRect(this.hWnd, out RECT _rect);
			return _rect;

		}
		set;
	}
	public SHOWWINDOW state
	{
		get
		{
			WINDOWPLACEMENT wndPlmnt = new();
			User32.GetWindowPlacement(this.hWnd, ref wndPlmnt);
			return (SHOWWINDOW)wndPlmnt.showCmd;
		}
		set;
	}

	public void Hide()
	{
		User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_HIDE);
	}
	public void Show()
	{
		User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_SHOW);
	}
	public void Focus()
	{
		User32.SetForegroundWindow(this.hWnd);
	}
	public void Move(RECT pos)
	{
		User32.SetWindowPos(this.hWnd, 0, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public Window(nint hWnd)
	{
		this.hWnd = hWnd;
	}
}

public class Workspace : IWorkspace
{
	public List<Window> windows { get; set; } = new();
	public Window? focusedWindow { get; set; } = null;
	public ILayout layout { get; set; } = new Dwindle();

	public void Add(Window wnd) { windows.Add(wnd); }
	public void Remove(nint hWnd)
	{
		int search = windows.Index().First(iwnd => iwnd.Item2.hWnd == hWnd).Item1;
		if (search != null) windows.RemoveAt(search);
	}

	// main renderer
	public void Focus()
	{
		RECT[] rects = layout.GetRect(windows.Count);
		for (int i = 0; i < windows.Count; i++)
		{
			windows[i].Move(rects[i]);
			windows[i].Show();
		}
	}

	public void FocusWindow(Window wnd)
	{
		wnd.Focus();
		focusedWindow = wnd;
	}
}

public class Dwindle : ILayout
{
	public RECT[] GetRect(int count)
	{
		RECT[] rects = new RECT[count];
		(int width, int height) = Utils.GetScreenSize();
		FillDirection fillDirection = FillDirection.HORIZONTAL;
		// where the nth window will go
		RECT fillRect = new() { Left = 0, Top = 0, Right = width, Bottom = height };
		for (int i = 0; i < count; i++)
		{
			rects[i] = fillRect;

			// modify the fillRect
			switch (fillDirection)
			{
				case FillDirection.HORIZONTAL:
					if (i - 1 >= 0) rects[i - 1] = TopHalf(rects[i - 1]);
					fillRect.Left += (int)((fillRect.Right - fillRect.Left) / 2);
					break;
				case FillDirection.VERTICAL:
					if (i - 1 >= 0) rects[i - 1] = LeftHalf(rects[i - 1]);
					fillRect.Top += (int)((fillRect.Bottom - fillRect.Top) / 2);
					break;
			}
			fillDirection = fillDirection == FillDirection.HORIZONTAL ? FillDirection.VERTICAL : FillDirection.HORIZONTAL;
		}
		rects.Index().ToList().ForEach(irect => Console.WriteLine($"{irect.Item1}. L:{irect.Item2.Left} R:{irect.Item2.Right} T:{irect.Item2.Top} B:{irect.Item2.Bottom}"));
		return rects;
	}
	public int outer { get; set; } = 5;
	public int inner { get; set; } = 5;

	RECT LeftHalf(RECT rect)
	{
		rect.Right -= (int)((rect.Right - rect.Left) / 2);
		return rect;
	}
	RECT TopHalf(RECT rect)
	{
		rect.Bottom -= (int)((rect.Bottom - rect.Top) / 2);
		return rect;
	}
}

public class WindowManager : IWindowManager
{
	public List<Window> windows { get; set; } = new();
	public List<Workspace> workspaces { get; set; } = new();
	public Workspace? focusedWorkspace { get; set; } = null;

	public WindowManager()
	{
		List<nint>? hWnds = Utils.GetAllTaskbarWindows();
		hWnds.ForEach(hWnd =>
		{
			windows.Add(new(hWnd));
		});
		windows = windows.Where(wnd => wnd.title.Contains("windowgen")).ToList();
		windows.ForEach(wnd => Console.WriteLine($"Title: {wnd.title}, hWnd: {wnd.hWnd}"));

		// add all windows to 1st workspace
		Workspace wksp = new();
		workspaces.Add(wksp);
		windows.ForEach(wnd => wksp.windows.Add(wnd));
		FocusWorkspace(wksp);
	}

	public void FocusWorkspace(Workspace wksp)
	{
		windows.ForEach(wnd => wnd.Hide());
		wksp.Focus();
		focusedWorkspace = wksp;
	}
}

enum FillDirection
{
	HORIZONTAL,
	VERTICAL
}
