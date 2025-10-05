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
		WindowEventsListener wel = new();
		KeyEventsListener kel = new();

		WindowManager wm = new();
		wel.WINDOW_ADDED += wm.WindowAdded;
		wel.WINDOW_REMOVED += wm.WindowRemoved;
		while (Console.ReadLine() != ":q") { }
	}
}

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
		// remove frame bounds
		RECT margin = GetFrameMargin();
		pos.Left -= margin.Left;
		pos.Top -= margin.Top;
		pos.Right -= margin.Right;
		pos.Bottom -= margin.Bottom;

		User32.SetWindowPos(this.hWnd, 0, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public RECT GetFrameMargin()
	{
		User32.GetWindowRect(this.hWnd, out RECT rect);
		//Console.WriteLine($"GWR [L: {rect.Left} R: {rect.Right} T: {rect.Top} B:{rect.Bottom}]");
		int size = Marshal.SizeOf<RECT>();
		nint rectPtr = Marshal.AllocHGlobal(size);
		Dwmapi.DwmGetWindowAttribute(this.hWnd, (uint)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, rectPtr, (uint)size);
		RECT rect2 = Marshal.PtrToStructure<RECT>(rectPtr);
		Marshal.FreeHGlobal(rectPtr);
		//Console.WriteLine($"DWM [L: {rect2.Left} R: {rect2.Right} T: {rect2.Top} B:{rect2.Bottom}]");

		return new RECT()
		{
			Left = rect2.Left - rect.Left,
			Top = rect2.Top - rect.Top,
			Right = rect2.Right - rect.Right,
			Bottom = rect2.Bottom - rect.Bottom,
		};
	}

	public Window(nint hWnd)
	{
		this.hWnd = hWnd;
	}
}

public class Workspace : IWorkspace
{
	public List<Window> windows { get; } = new();
	public Window? focusedWindow { get; private set; } = null;
	public ILayout layout { get; set; } = new Dwindle();

	public void Add(Window wnd) { windows.Add(wnd); }
	public void Remove(nint hWnd)
	{
		int search = windows.Index().First(iwnd => iwnd.Item2.hWnd == hWnd).Item1;
		Console.WriteLine($"removed: {search}");
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
		// rects with margin
		RECT[] rects = new RECT[count];
		// rects with no margins
		RECT[] fillRects = new RECT[count];
		(int width, int height) = Utils.GetScreenSize();
		FillDirection fillDirection = FillDirection.HORIZONTAL;
		// where the nth window will go
		RECT fillRect = new() { Left = 0, Top = 0, Right = width, Bottom = height };
		for (int i = 0; i < count; i++)
		{
			fillRects[i] = fillRect;

			// modify the fillRect
			switch (fillDirection)
			{
				case FillDirection.HORIZONTAL:
					if (i - 1 >= 0)
					{
						fillRects[i - 1] = TopHalf(fillRects[i - 1]);
					}
					fillRect.Left += (int)((fillRect.Right - fillRect.Left) / 2);
					break;
				case FillDirection.VERTICAL:
					if (i - 1 >= 0)
					{
						fillRects[i - 1] = LeftHalf(fillRects[i - 1]);
					}
					fillRect.Top += (int)((fillRect.Bottom - fillRect.Top) / 2);
					break;
			}
			fillDirection = fillDirection == FillDirection.HORIZONTAL ? FillDirection.VERTICAL : FillDirection.HORIZONTAL;

		}
		fillRects.Index().ToList().ForEach(irect => Console.WriteLine($"{irect.Item1}. L:{irect.Item2.Left} R:{irect.Item2.Right} T:{irect.Item2.Top} B:{irect.Item2.Bottom}"));
		return ApplyInner(ApplyOuter(fillRects));
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

	// applies outer margins
	RECT[] ApplyOuter(RECT[] fillRects)
	{
		(int width, int height) = Utils.GetScreenSize();
		for (int i = 0; i < fillRects.Length; i++)
		{
			if (fillRects[i].Left == 0) fillRects[i].Left += outer;
			if (fillRects[i].Top == 0) fillRects[i].Top += outer;
			if (fillRects[i].Right == width) fillRects[i].Right -= outer;
			if (fillRects[i].Bottom == height) fillRects[i].Bottom -= outer;
		}
		return fillRects;
	}

	// applies inner margins (apply only after outer)
	RECT[] ApplyInner(RECT[] fillRects)
	{
		(int width, int height) = Utils.GetScreenSize();
		for (int i = 0; i < fillRects.Length; i++)
		{
			if (fillRects[i].Left != outer) fillRects[i].Left += (int)(inner / 2);
			if (fillRects[i].Top != outer) fillRects[i].Top += (int)(inner / 2);
			if (fillRects[i].Right != width - outer) fillRects[i].Right -= (int)(inner / 2);
			if (fillRects[i].Bottom != height - outer) fillRects[i].Bottom -= (int)(inner / 2);
		}
		return fillRects;
	}
}

public class WindowManager : IWindowManager
{
	public List<Window> windows { get; } = new();
	public List<Workspace> workspaces { get; } = new();
	public Workspace? focusedWorkspace { get; private set; } = null;

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

	public void WindowAdded(Window wnd)
	{
		Console.WriteLine($"WindowAdded, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Add(wnd);
		focusedWorkspace.Focus();
	}
	public void WindowRemoved(Window wnd)
	{
		Console.WriteLine($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Remove(wnd.hWnd);
		focusedWorkspace.Focus();
	}
	public void WindowMoved(Window wnd) { }
}

enum FillDirection
{
	HORIZONTAL,
	VERTICAL
}
