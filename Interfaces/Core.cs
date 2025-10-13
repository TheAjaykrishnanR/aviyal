using System;
using System.Collections.Generic;

public interface IWindow
{
	public nint hWnd { get; }
	public string title { get; }
	public string className { get; }
	public string exe { get; }
	public RECT rect { get; }
	public SHOWWINDOW state { get; }
	public bool floating { get; set; }

	public void Hide();
	public void Show();
	public void Focus();
	public void Move(RECT pos);
	public void Move(int? x, int? y);
	public void Close();
}

public interface IWorkspace
{
	public List<Window> windows { get; }
	public Window? focusedWindow { get; }
	public int? focusedWindowIndex { get; }
	public ILayout layout { get; set; }

	public void Add(Window wnd);
	public void Remove(Window wnd);

	public void Focus();
	public void Hide();
	public void CloseFocusedWindow();
	public void FocusAdjacentWindow(EDGE direction);
	public void Move(int? x, int? y);
	public void SwapWindows(Window wnd1, Window wnd2);
	public Window? GetWindowFromPoint(POINT pt);
}

public interface IWindowManager
{
	public List<Workspace> workspaces { get; }
	public Workspace focusedWorkspace { get; }
	public int focusedWorkspaceIndex { get; }

	public void FocusWorkspace(Workspace wksp);
	public void FocusNextWorkspace() { }
	public void FocusPreviousWorkspace() { }

	public void WindowAdded(Window wnd);
	public void WindowRemoved(Window wnd);
	public void WindowMoved(Window wnd);
	public void WindowMaximized(Window wnd);
	public void WindowMinimized(Window wnd);
	public void WindowRestored(Window wnd);
}

public interface ILayout
{
	public int inner { get; set; }
	public int outer { get; set; }

	public RECT[] GetRects(int index);
	public RECT[] ApplyInner(RECT[] rects);
	public RECT[] ApplyOuter(RECT[] rects);
	public int? GetAdjacent(int index, EDGE direction);
}

public interface IAnimation<T>
{
	public T GetValue(double progress);
}

