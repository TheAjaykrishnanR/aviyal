using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class Window : IWindow
{
	public nint hWnd { get; }
	public string title
	{
		get
		{
			return Utils.GetWindowTitleFromHWND(this.hWnd);
		}

	}
	public string className { get; }
	public string exe
	{
		get
		{
			return Utils.GetExePathFromHWND(this.hWnd);
		}

	}
	public RECT rect
	{
		get
		{
			User32.GetWindowRect(this.hWnd, out RECT _rect);
			return _rect;
		}

	}
	public SHOWWINDOW state
	{
		get
		{
			WINDOWPLACEMENT wndPlmnt = new();
			User32.GetWindowPlacement(this.hWnd, ref wndPlmnt);
			return (SHOWWINDOW)wndPlmnt.showCmd;
		}
	}

	public override bool Equals(object? obj)
	{
		if (((Window)obj).hWnd == this.hWnd) return true;
		return false;
	}

	public static bool operator ==(Window left, Window right) { return left.Equals(right); }

	public static bool operator !=(Window left, Window right) { return !left.Equals(right); }

	public void Hide()
	{
		User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_HIDE);
	}
	public void Show()
	{
		User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_SHOWNA);
	}
	public void Focus()
	{
		User32.SetForegroundWindow(this.hWnd);
	}
	public void Move(RECT pos)
	{
		// remove frame bounds
		RECT margin = GetFrameMargin();
		pos.Left -= margin.Left;
		pos.Top -= margin.Top;
		pos.Right -= margin.Right;
		pos.Bottom -= margin.Bottom;

		User32.SetWindowPos(this.hWnd, (nint)SWPZORDER.HWND_BOTTOM, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public RECT GetFrameMargin()
	{
		User32.GetWindowRect(this.hWnd, out RECT rect);
		Console.WriteLine($"GWR [L: {rect.Left} R: {rect.Right} T: {rect.Top} B:{rect.Bottom}]");
		int size = Marshal.SizeOf<RECT>();
		nint rectPtr = Marshal.AllocHGlobal(size);
		Dwmapi.DwmGetWindowAttribute(this.hWnd, (uint)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, rectPtr, (uint)size);
		RECT rect2 = Marshal.PtrToStructure<RECT>(rectPtr);
		Marshal.FreeHGlobal(rectPtr);
		Console.WriteLine($"DWM [L: {rect2.Left} R: {rect2.Right} T: {rect2.Top} B:{rect2.Bottom}]");
		// scale dwm rect2 to take into account display scaling
		double scale = Utils.GetDisplayScaling();

		return new RECT()
		{
			Left = rect2.Left - rect.Left,
			Top = rect2.Top - rect.Top,
			Right = rect2.Right - rect.Right,
			Bottom = rect2.Bottom - rect.Bottom,
		};
	}

	RECT ScaleRect(RECT rect, double scale)
	{
		rect.Left = (int)(rect.Left * scale);
		rect.Top = (int)(rect.Top * scale);
		rect.Right = (int)(rect.Right * scale);
		rect.Bottom = (int)(rect.Bottom * scale);
		return rect;
	}

	public void Close()
	{
		User32.SendMessage(this.hWnd, (uint)WINDOWMESSAGE.WM_CLOSE, 0, 0);
	}

	public Window(nint hWnd)
	{
		this.hWnd = hWnd;
	}
}

public class Workspace : IWorkspace
{
	public Guid id { get; } = Guid.NewGuid();
	public List<Window> windows { get; } = new();
	public Window? focusedWindow
	{
		get
		{
			Window wnd = new(User32.GetForegroundWindow());
			if (windows.Contains(wnd)) return wnd;
			return windows.First();
		}
		private set;
	}
	public int focusedWindowIndex
	{
		get
		{
			int index = 0;
			for (int i = 0; i < windows.Count; i++)
			{
				if (windows[i] == focusedWindow) index = i;
			}
			return index;
		}
	}
	public ILayout layout { get; set; } = new Dwindle();

	public override bool Equals(object? obj)
	{
		if (((Workspace)obj).id == this.id) return true;
		return false;
	}

	public static bool operator ==(Workspace left, Workspace right) { return left.Equals(right); }

	public static bool operator !=(Workspace left, Workspace right) { return !left.Equals(right); }

	public void Add(Window wnd) { windows.Add(wnd); }
	//public void Remove(nint hWnd)
	public void Remove(Window wnd)
	{
		//(int, Window)? search = windows.Index().First(iwnd => iwnd.Item2.hWnd == hWnd);
		//int? index = search?.Item1;
		//if (index != null) windows.RemoveAt((int)index);
		windows.Remove(wnd);
	}

	// main renderer
	public void Focus()
	{
		RECT[] rects = layout.GetRects(windows.Count);
		for (int i = 0; i < windows.Count; i++)
		{
			windows[i].Move(rects[i]);
			windows[i].Show();
		}
	}

	public void FocusWindow(Window wnd)
	{
		wnd.Focus();
		//focusedWindow = wnd;
	}

	public void FocusAdjacentWindow(EDGE direction)
	{
		int? indexOfWindowToFocus = layout.GetAdjacent(focusedWindowIndex, direction);
		FocusWindow(windows[(int)indexOfWindowToFocus]);
		Console.WriteLine($"Focusing window in [ {direction} ], windowToFocus: [ {indexOfWindowToFocus} ], currentlyFocused: {focusedWindowIndex}");
	}
}

public class WindowManager : IWindowManager
{
	public List<Window> initWindows { get; } = new();
	public List<Workspace> workspaces { get; } = new();
	public Workspace? focusedWorkspace { get; private set; } = null;
	public int focusedWorkspaceIndex
	{
		get
		{
			int index = 0;
			for (int i = 0; i < workspaces.Count; i++)
			{
				if (workspaces[i] == focusedWorkspace) index = i;
			}
			return index;
		}
	}
	public int WORKSPACES = 9;

	public WindowManager()
	{
		List<nint>? hWnds = Utils.GetAllTaskbarWindows();
		hWnds.ForEach(hWnd =>
		{
			initWindows.Add(new(hWnd));
		});
		initWindows = initWindows.Where(wnd => wnd.title.Contains("windowgen")).ToList();
		initWindows.ForEach(wnd => Console.WriteLine($"Title: {wnd.title}, hWnd: {wnd.hWnd}"));

		for (int i = 0; i < WORKSPACES; i++)
		{
			Workspace wksp = new();
			workspaces.Add(wksp);
		}
		// add all windows to 1st workspace
		initWindows.ForEach(wnd => workspaces[0].windows.Add(wnd));
		FocusWorkspace(workspaces[0]);
	}

	public void FocusWorkspace(Workspace wksp)
	{
		workspaces.ForEach(wksp => wksp.windows.ForEach(wnd => wnd.Hide()));
		wksp.Focus();
		focusedWorkspace = wksp;
	}

	public void FocusNextWorkspace()
	{
		if (focusedWorkspaceIndex < workspaces.Count - 1) FocusWorkspace(workspaces[focusedWorkspaceIndex + 1]);
		else FocusWorkspace(workspaces.First());
		Console.WriteLine($"next, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
	}
	public void FocusPreviousWorkspace()
	{
		if (focusedWorkspaceIndex > 0) FocusWorkspace(workspaces[focusedWorkspaceIndex - 1]);
		else FocusWorkspace(workspaces.Last());
		Console.WriteLine($"previous, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
	}
	public void CloseFocusedWindow() => focusedWorkspace.focusedWindow.Close();

	public void WindowAdded(Window wnd)
	{
		Console.WriteLine($"WindowAdded, {wnd.title}, hWnd: {wnd.hWnd}, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
		focusedWorkspace.Add(wnd);
		focusedWorkspace.Focus();
	}
	public void WindowRemoved(Window wnd)
	{
		Console.WriteLine($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Remove(wnd);
		focusedWorkspace.Focus();
	}
	public void WindowMoved(Window wnd) { }
}

enum FillDirection
{
	HORIZONTAL,
	VERTICAL
}
