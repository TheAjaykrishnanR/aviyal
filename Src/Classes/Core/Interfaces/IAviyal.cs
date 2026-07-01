using System;
using System.Collections.Generic;

public interface IWindow
{
    public nint hWnd { get; }
    public bool exists { get; }
    public string title { get; }
    public string className { get; }
    public string? exe { get; }
    public RECT rect { get; }
    public WINDOWSTATE state { get; }
    public bool? resizeable { get; }
    public NONTILEDSTATE nonTiledState { get; set; }
    public WINDOWSTYLE styles { get; }
    public WINDOWSTYLEEX exStyles { get; }

    /* Window actions
     * returns: bool (if success)
     * */
    public bool Hide();
    public bool Show();
    public bool Maximize();
    public bool Unmaximize();
    public bool Minimize();
    public bool Focus();
    public bool Move(RECT pos, bool verify, bool redraw);
    public bool Move(int? x, int? y, bool verify, bool redraw);
    public bool Close();
    public bool Redraw();
}

public interface IWorkspace
{
    public List<Window> windows { get; }
    public Window? focusedWindow { get; }
    public int? focusedWindowIndex { get; }
    public ILayout layout { get; set; }

    public void Add(Window wnd);
    public void Remove(Window wnd);

    public void Show();
    public void Hide();
    public void Focus();
    public void Redraw();
    public void SetFocusedWindow();
    public void CloseFocusedWindow();
    public void ToggleFocusedWindowMaximization();
    public void MinimizeFocusedWindow();
    public void FocusAdjacentWindow(EDGE direction);
    public bool Move(int? x, int? y, bool verify, bool redraw);
    public void SwapWindows(Window wnd1, Window wnd2);
    public Window? GetWindowFromPoint(POINT pt);
}

public interface IWindowManager
{
    public List<Workspace> workspaces { get; }
    public Workspace focusedWorkspace { get; }
    public int focusedWorkspaceIndex { get; }
    public List<Window?> windows { get; }

    public void FocusWorkspace(int index);
    public void FocusNextWorkspace() { }
    public void FocusPreviousWorkspace() { }

    /* EVENTS */

    public void WindowShown(Window wnd);
    public void WindowHidden(Window wnd);
    public void WindowDestroyed(Window wnd);
    public void WindowMoved(Window wnd);
    public void WindowMaximized(Window wnd);
    public void WindowMinimized(Window wnd);
    public void WindowRestored(Window wnd);
    public void WindowFullscreened(Window wnd);
}

public interface ILayout
{
    public int inner { get; set; }
    public int left { get; set; }
    public int top { get; set; }
    public int right { get; set; }
    public int bottom { get; set; }

    public RECT[] GetRects(int index);
    public RECT[] ApplyInner(RECT[] rects);
    public RECT[] ApplyOuter(RECT[] rects);
    public int? GetAdjacent(int index, EDGE direction);
}
