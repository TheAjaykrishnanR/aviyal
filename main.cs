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

public class Window
{
	public nint hWnd { get; private set; }
	public string title
	{
		get
		{
			return Utils.GetWindowTitleFromHWND(this.hWnd);
		}
		private set;
	}
	public string className { get; private set; }
	public string exe
	{
		get
		{
			return Utils.GetExePathFromHWND(this.hWnd);
		}
		private set;
	}
	public RECT rect
	{
		get
		{
			User32.GetWindowRect(this.hWnd, out RECT _rect);
			return _rect;

		}
		private set;
	}
	public SHOWWINDOW state
	{
		get
		{
			WINDOWPLACEMENT wndPlmnt = new();
			User32.GetWindowPlacement(this.hWnd, ref wndPlmnt);
			return (SHOWWINDOW)wndPlmnt.showCmd;
		}
		private set;
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

public class Workspace
{
	public List<Window> windows = new();
	Window? focusedWindow = null;
	ILayout layout = new Dwindle();

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
		for (int i = 0; i < count; i++)
		{
			// if count = 1
			RECT rect = new();
			rect.Left = outer;
			rect.Right = width - outer;
			rect.Top = outer;
			rect.Bottom = height - outer;
			rects[i] = rect;
		}
		return rects;
	}
	public int outer { get; set; } = 5;
	public int inner { get; set; } = 5;
}

public interface ILayout
{
	public RECT[] GetRect(int index);
	public int outer { get; set; }
	public int inner { get; set; }
}

public class WindowManager
{
	List<Window> windows = new();
	List<Workspace> workspaces = new();
	Workspace? focusedWorkspace = null;

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
