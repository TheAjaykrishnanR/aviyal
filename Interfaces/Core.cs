using System;
using System.Collections.Generic;

public interface IWindow
{
	public nint hWnd { get; set; }
	public string title { get; set; }
	public string className { get; set; }
	public string exe { get; set; }
	public RECT rect { get; set; }
	public SHOWWINDOW state { get; set; }

	public void Hide();
	public void Show();
	public void Focus();
	public void Move(RECT pos);
}

public interface IWorkspace
{
	public List<Window> windows { get; set; }
	public Window? focusedWindow { get; set; }
	public ILayout layout { get; set; }

	public void Add(Window wnd);
	public void Remove(nint hWnd);

	public void Focus();
	public void FocusWindow(Window wnd);
}

public interface IWindowManager
{
	public List<Window> windows { get; set; }
	public List<Workspace> workspaces { get; set; }
	public Workspace? focusedWorkspace { get; set; }

	public void FocusWorkspace(Workspace wksp);
}

public interface ILayout
{
	public RECT[] GetRect(int index);
	public int outer { get; set; }
	public int inner { get; set; }
}
