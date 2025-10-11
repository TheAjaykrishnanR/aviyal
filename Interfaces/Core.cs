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

	public void Hide();
	public void Show();
	public void Focus();
	public void Move(RECT pos);
	public void Close();
}

public interface IWorkspace
{
	public List<Window> windows { get; }
	public Window focusedWindow { get; }
	public int focusedWindowIndex { get; }
	public ILayout layout { get; set; }

	public void Add(Window wnd);
	public void Remove(Window wnd);

	public void Focus();
	public void CloseFocusedWindow();
	public void FocusAdjacentWindow(EDGE direction);
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
}

public interface ILayout
{
	public RECT[] GetRects(int index);
	public int outer { get; set; }
	public int inner { get; set; }
	public int? GetAdjacent(int index, EDGE direction);
}

