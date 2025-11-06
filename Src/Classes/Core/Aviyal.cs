using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class Window : IWindow, IMoveable
{
    private const int FOCUS_RETRIES = 10;

    private const SETWINDOWPOS defaultMoveFlags =
        SETWINDOWPOS.SWP_NOSENDCHANGING |
        SETWINDOWPOS.SWP_NOCOPYBITS |
        SETWINDOWPOS.SWP_ASYNCWINDOWPOS |
        SETWINDOWPOS.SWP_NOACTIVATE |
        SETWINDOWPOS.SWP_NOZORDER;

    private const SETWINDOWPOS slideFlag = defaultMoveFlags | SETWINDOWPOS.SWP_NOSIZE;

    private readonly int SHOWHIDE_RETRIES = 10;
    public int workspace;

    public Window(nint hWnd)
    {
        this.hWnd = hWnd;
    }

    public string? exeName => new(@$"{exe}"?.Split(@"\").Last().Reverse().Skip(4).Reverse().ToArray());

    public RECT relRect { get; set; } // position of window relative to workspace (without margins)

    public int pid
    {
        get
        {
            try
            {
                var _p = Process.GetProcessesByName(exeName).FirstOrDefault();
                return _p == null ? 0 : _p.Id;
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to get pid", ex);
                return 0;
            }
        }
    }

    /* whether the window process is relatively higher in process integrity than
     * aviyal.
     * */
    public bool elevated
    {
        get
        {
            if (!Environment.IsPrivilegedProcess &&
                /* absolute elevation of the process */
                Utils.IsProcessElevated(pid)) return true;
            return false;
        }
    }

    public int borderThickness
    {
        get
        {
            User32.GetWindowInfo(hWnd, out var info);
            return info.cxWindowBorders;
        }
    }

    public nint hWnd { get; }

    public string title => Utils.GetWindowTitleFromHWND(hWnd);

    public string className => Utils.GetClassNameFromHWND(hWnd);

    public string? exe
    {
        get
        {
            // EnumWindowProcess() is hella expensive, and anyway exe wont change
            // for a process once its found
            if (field == null)
                field = Utils.GetExePathFromHWND(hWnd) ??
                        Utils.EnumWindowProcesses()
                            .FirstOrDefault(wndProcess => wndProcess
                                .windows.Select(wndp => wndp.hWnd)
                                .Contains(hWnd))?
                            .process.MainModule?.FileName;
            return field;
        }
    }

    public RECT rect // absolute position
    {
        get
        {
            User32.GetWindowRect(hWnd, out var _rect);
            return _rect;
        }
    }

    public SHOWWINDOW state
    {
        get
        {
            WINDOWPLACEMENT wndPlmnt = new();
            User32.GetWindowPlacement(hWnd, ref wndPlmnt);
            var state = (SHOWWINDOW)wndPlmnt.showCmd;
            //Logger.Log($"state: {state}");
            return state;
        }
    }

    public bool resizeable
    {
        get
        {
            if (!styles.HasFlag(WINDOWSTYLE.WS_THICKFRAME)) return false;
            if (className.Contains("OperationStatusWindow") || // copy, paste status windows
                className.Contains("DS_MODALFRAME")
               ) return false;
            return true;
        }
    }

    public bool floating { get; set; }

    public WINDOWSTYLE styles => (WINDOWSTYLE)User32.GetWindowLong(hWnd, GETWINDOWLONG.GWL_STYLE);

    public WINDOWSTYLEEX exStyles => (WINDOWSTYLEEX)User32.GetWindowLong(hWnd, GETWINDOWLONG.GWL_EXSTYLE);

    public void Hide()
    {
        ToggleAnimation(false);
        var _retry = 0;
        while (User32.IsWindowVisible(hWnd))
        {
            if (++_retry > SHOWHIDE_RETRIES) break;
            User32.ShowWindow(hWnd, SHOWWINDOW.SW_HIDE);
        }

        ToggleAnimation(true);
    }

    public void Show()
    {
        ToggleAnimation(false);
        var _retry = 0;
        while (!User32.IsWindowVisible(hWnd))
        {
            if (++_retry > SHOWHIDE_RETRIES) break;
            User32.ShowWindow(hWnd, SHOWWINDOW.SW_SHOWNA);
        }

        ToggleAnimation(true);
    }

    public void Focus()
    {
        var _retry = 0;
        while (User32.GetForegroundWindow() != hWnd)
        {
            if (++_retry > FOCUS_RETRIES) break;
            User32.keybd_event(0, 0, 0, Globals.FOREGROUND_FAKE_KEY);
            User32.SetForegroundWindow(hWnd);
        }
    }

    public void Move(RECT pos, bool redraw = true)
    {
        // remove frame bounds
        var margin = GetFrameMargin();
        pos.Left -= margin.Left;
        pos.Top -= margin.Top;
        pos.Right -= margin.Right;
        pos.Bottom -= margin.Bottom;

        var flags = redraw switch
        {
            true => defaultMoveFlags,
            false => defaultMoveFlags | SETWINDOWPOS.SWP_NOREDRAW
        };

        User32.SetWindowPos(hWnd, 0, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, flags);
    }

    public void Move(int? x, int? y, bool redraw = true)
    {
        var flags = redraw switch
        {
            true => slideFlag,
            false => slideFlag | SETWINDOWPOS.SWP_NOREDRAW
        };
        User32.SetWindowPos(hWnd, 0, x ?? rect.Left, y ?? rect.Top, 0, 0, flags);
    }

    public void Close()
    {
        User32.SendMessage(hWnd, (uint)WINDOWMESSAGE.WM_CLOSE, 0, 0);
    }

    // force the window to redraw itself
    public void Redraw()
    {
        User32.RedrawWindow(hWnd, 0, 0,
            REDRAWWINDOW.INVALIDATE |
            REDRAWWINDOW.ALLCHILDREN |
            REDRAWWINDOW.UPDATENOW
        );
    }

    public override bool Equals(object? obj)
    {
        //if (base.Equals(obj)) return true;
        if (obj is null) return false;
        if (((Window)obj).hWnd == hWnd) return true;
        return false;
    }

    public static bool operator ==(Window? left, Window? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Window? left, Window? right)
    {
        if (left is null) return right is not null;
        return !left.Equals(right);
    }

    public void SetBottom()
    {
        User32.SetWindowPos(hWnd, (nint)SWPZORDER.HWND_BOTTOM, 0, 0, 0, 0,
            SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
    }

    public void SetFront()
    {
        User32.SetWindowPos(hWnd, (nint)SWPZORDER.HWND_TOP, 0, 0, 0, 0,
            SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
    }

    public void ToggleAnimation(bool flag)
    {
        var attr = 0;
        if (!flag) attr = 1;
        Dwmapi.DwmSetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, ref attr, sizeof(int));
    }

    public RECT GetFrameMargin()
    {
        User32.GetWindowRect(hWnd, out var rect);
        var size = Marshal.SizeOf<RECT>();
        var rectPtr = Marshal.AllocHGlobal(size);
        Dwmapi.DwmGetWindowAttribute(hWnd, (uint)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, rectPtr, (uint)size);
        var rect2 = Marshal.PtrToStructure<RECT>(rectPtr);
        Marshal.FreeHGlobal(rectPtr);

        return new RECT
        {
            Left = rect2.Left - rect.Left,
            Top = rect2.Top - rect.Top,
            Right = rect2.Right - rect.Right,
            Bottom = rect2.Bottom - rect.Bottom
        };
    }

    private RECT ScaleRect(RECT rect, double scale)
    {
        rect.Left = (int)(rect.Left * scale);
        rect.Top = (int)(rect.Top * scale);
        rect.Right = (int)(rect.Right * scale);
        rect.Bottom = (int)(rect.Bottom * scale);
        return rect;
    }

    private bool RectEqual(RECT a, RECT b)
    {
        return a.Left == b.Left &&
               a.Top == b.Top &&
               a.Right == b.Right &&
               a.Bottom == b.Bottom;
    }
}

public class Workspace : IWorkspace, IMoveable
{
    private const int MOVE_RETRIES = 10;
    private readonly (int, int) floatingWindowSize;

    private Config config;

    private Window? lastFocusedWindow;

    public Workspace(Config config)
    {
        this.config = config;
        var sizeStrs = config.floatingWindowSize.Split("x");
        floatingWindowSize.Item1 = Convert.ToInt32(sizeStrs[0]);
        floatingWindowSize.Item2 = Convert.ToInt32(sizeStrs[1]);
    }

    public Guid id { get; } = Guid.NewGuid();
    public List<Window?> windows { get; } = new();

    public Window? focusedWindow => windows.FirstOrDefault(_wnd => _wnd == new Window(User32.GetForegroundWindow()));

    public int? focusedWindowIndex
    {
        get
        {
            int? index = null;
            if (focusedWindow == null) return null;
            for (var i = 0; i < windows.Count; i++)
                if (windows[i] == focusedWindow)
                {
                    index = i;
                    break;
                }

            return index;
        }
    }

    public ILayout layout { get; set; }

    public void Add(Window wnd)
    {
        windows.Add(wnd);
        Update();
    }

    public void Remove(Window wnd)
    {
        windows.Remove(wnd);
        Update();
    }

    public void Show()
    {
        windows?.ForEach(wnd => wnd?.Show());
    }

    public void Hide()
    {
        windows?.ForEach(wnd => wnd?.Hide());
    }

    public void Focus()
    {
        Update();
        Show();
        SetFocusedWindow();
    }

    public void Redraw()
    {
        windows?.ForEach(wnd => wnd?.Redraw());
    }

    public void Move(int? x, int? y, bool redraw = true)
    {
        for (var i = 0; i < windows.Count; i++)
        {
            var absX = windows[i]!.relRect.Left + x;
            var absY = windows[i]!.relRect.Top + y;
            var _retry = 0;
            while (windows[i]!.rect.Left != absX || windows[i]!.rect.Top != absY)
            {
                if (++_retry > MOVE_RETRIES) break;
                windows[i]?.Move(absX, absY, redraw);
            }
        }
    }

    public void SetFocusedWindow()
    {
        if (lastFocusedWindow == null)
        {
            var wnd = windows?.FirstOrDefault();
            lastFocusedWindow = wnd;
            wnd?.Focus();
        }
        else
        {
            lastFocusedWindow.Focus();
        }
    }

    public void CloseFocusedWindow()
    {
        var fWnd = focusedWindow;
        var index = focusedWindowIndex;
        if (index == null) return;
        var toFocus = index > 0 ? index - 1 : 0;
        focusedWindow?.Close();
        windows.ElementAtOrDefault((int)toFocus)?.Focus();
        windows.Remove(fWnd);
        Update();
    }

    public void FocusAdjacentWindow(EDGE direction)
    {
        if (focusedWindowIndex == null) return;
        var index = layout.GetAdjacent((int)focusedWindowIndex, direction);
        if (index != null) windows?[(int)index]?.Focus();
    }

    public void SwapWindows(Window wnd1, Window wnd2)
    {
        if (!windows.Contains(wnd1) || !windows.Contains(wnd2)) return;
        var wnd1_index = windows.Index().First(iwnd => iwnd.Item == wnd1).Index;
        var wnd2_index = windows.Index().First(iwnd => iwnd.Item == wnd2).Index;
        windows[wnd1_index] = wnd2;
        windows[wnd2_index] = wnd1;
        Update();
    }

    public Window? GetWindowFromPoint(POINT pt)
    {
        return windows.FirstOrDefault(wnd =>
        {
            return wnd?.relRect.Left < pt.X && pt.X < wnd?.relRect.Right &&
                   wnd?.relRect.Top < pt.Y && pt.Y < wnd?.relRect.Bottom;
        });
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return this is null;
        if (((Workspace)obj).id == id) return true;
        return false;
    }

    public static bool operator ==(Workspace left, Workspace right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Workspace left, Workspace right)
    {
        if (left is null) return right is not null;
        return !left.Equals(right);
    }

    // applies updated relRects (provided by the layout) to the windows in the workspace
    public void Update()
    {
        var nonFloating = windows
            .Where(wnd => wnd?.resizeable == true)
            .Where(wnd => wnd?.floating == false)
            .Where(wnd => wnd?.elevated == false)
            .Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMAXIMIZED)
            .Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMINIMIZED)
            .ToList();

        // non floating
        var relRects = layout.GetRects(nonFloating.Count);
        var rects = layout.ApplyInner(layout.ApplyOuter(relRects.ToArray()));
        for (var i = 0; i < nonFloating.Count; i++)
        {
            nonFloating[i]?.Move(rects[i]);
            nonFloating[i]!.relRect = relRects[i];
        }

        // floating
        var floating = windows
            .Where(wnd => wnd?.resizeable == true)
            .Where(wnd => wnd?.floating == true)
            .Where(wnd => wnd?.elevated == false)
            .Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMAXIMIZED)
            .Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMINIMIZED)
            .ToList()!;

        for (var i = 0; i < floating.Count; i++) floating[i]!.relRect = floating[i]!.rect;
    }

    // changes the order of windows in the workspace
    public void ShiftFocusedWindowBy(int shiftBy)
    {
        var _fwnd = focusedWindow;
        var index = focusedWindowIndex;
        if (index == null) return;
        index += shiftBy;
        if (index < 0 || index > windows.Count - 1) return;
        windows.Remove(_fwnd);
        windows.Insert((int)index, _fwnd);
        Update();
    }

    public void MakeFloating(Window wnd)
    {
        if (!wnd.resizeable) return;
        wnd.Move(GetCenterRect(floatingWindowSize.Item1, floatingWindowSize.Item2));
    }

    public void ToggleFloating(Window? wnd = null)
    {
        if (wnd == null) wnd = focusedWindow;
        if (wnd == null) return;
        wnd.floating = !wnd.floating;
        if (wnd.floating && wnd.resizeable) MakeFloating(wnd);
        Update();
    }

    private RECT GetCenterRect(int w, int h)
    {
        var (sw, sh) = Utils.GetScreenSize();
        return new RECT
        {
            Left = (sw - w) / 2,
            Right = (sw + w) / 2,
            Top = (sh - h) / 2,
            Bottom = (sh + h) / 2
        };
    }
}

public class WindowManager : IWindowManager
{
    public delegate void wmEventHandler(string message);

    private const int WINEVENT_DELAY = 100;
    private const int WINEVENT_RESTORE_TIMEOUT = 1000;
    public static string DEBUG_WND_NAME = "windowgen";

    /* all workspace/window actions must be executed inside this wrapper function
     * This is to ensure that our own actions dont trigger the window events recursively
     * and also to ensure that a new action isn't executed while an old one is going on.
     *
     * Only wrap non-atomic composite actions. All public window manager actions must be
     * wrapped.
     * */
    private readonly Lock addLock = new();

    private readonly Config config;
    private readonly List<Task> wmActions = new();
    private nint lasRestoredhWnd;
    private long lastRestoreTime;

    public WindowManager(Config config)
    {
        this.config = config;
    }

    public List<Window>? initWindows { get; set; } // initWindows := initial set of windows to start the WM with

    // window unmaximized
    public bool mouseDown { get; set; } = false;
    public List<Workspace?> workspaces { get; } = new();
    public Workspace focusedWorkspace { get; private set; }

    public int focusedWorkspaceIndex
    {
        get
        {
            var index = 0;
            for (var i = 0; i < workspaces.Count; i++)
                if (workspaces[i]! == focusedWorkspace)
                {
                    index = i;
                    break;
                }

            return index;
        }
    }

    /*
     * Public actions offered by the window manager
     * */

    public void FocusWorkspace(int workspaceIndex)
    {
        if (workspaceIndex < 0 || workspaceIndex > workspaces.Count - 1) return;
        SuppressEvents(() => FocusWorkspace(workspaces[workspaceIndex]!));

        WM_EVENT("FocusWorkspace");
    }

    public void FocusNextWorkspace()
    {
        var next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
        var prev = focusedWorkspaceIndex > 0 ? focusedWorkspaceIndex - 1 : workspaces.Count - 1;

        SuppressEvents(() =>
        {
            if (config.workspaceAnimations)
            {
                // slide windows left -> if horizontal
                // slide windows up -> if vertical
                var (w, h) = Utils.GetScreenSize();
                if (config.workspaceAnimationsDirection == "horizontal")
                {
                    workspaces[next]?.Move(w, null);
                }
                else if (config.workspaceAnimationsDirection == "vertical")
                {
                    Logger.Log($"next workspace set down at h: {h}");
                    workspaces[next]?.Move(null, h);
                }

                /* we call Show() here instead of Focus() because Focus() has a call to Update()
                 * if we Update() our Workspace then all the windows will be set to their
                 * appropriate relRect effectively reversing Move(w, null). Hence as a result
                 * you will see a flash of the next/prev workspace before it appears sliding.
                 * So whats exactly going on ? Move(w, null) moves your workspace out of screen,
                 * Focus() brings it back using Update() and Shows it until WorkspaceAnimate()
                 * takes it out of screen as part of the animation start position which is also
                 * beyond the screen.
                 * */
                workspaces[next]?.Show();

                Animation<Workspace> workspaceAnimation = new(config.workspaceAnimationsDuration, "easeOutQuint");
                if (config.workspaceAnimationsDirection == "horizontal")
                {
                    workspaceAnimation.Add(focusedWorkspace, new POINT2 { X = 0, Y = null },
                        new POINT2 { X = -w, Y = null });
                    workspaceAnimation.Add(workspaces[next], new POINT2 { X = w, Y = null },
                        new POINT2 { X = 0, Y = null });
                }
                else if (config.workspaceAnimationsDirection == "vertical")
                {
                    workspaceAnimation.Add(focusedWorkspace, new POINT2 { X = null, Y = 0 },
                        new POINT2 { X = null, Y = -h });
                    workspaceAnimation.Add(workspaces[next], new POINT2 { X = null, Y = h },
                        new POINT2 { X = null, Y = 0 });
                }

                workspaceAnimation.Play().Wait();
                focusedWorkspace.Hide();
                focusedWorkspace = workspaces[next]!;
                focusedWorkspace?.Update(); // when animation finishes, margins dont match
                focusedWorkspace?.Redraw(); // manually redraw
                focusedWorkspace?.SetFocusedWindow();
            }
            else
            {
                FocusWorkspace(workspaces[next]!);
            }
        });

        WM_EVENT("FocusNextWorkspace");
    }

    public void FocusPreviousWorkspace()
    {
        var next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
        var prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;

        SuppressEvents(() =>
        {
            if (config.workspaceAnimations)
            {
                // move right
                // move down
                var (w, h) = Utils.GetScreenSize();
                if (config.workspaceAnimationsDirection == "horizontal")
                    workspaces[prev]?.Move(-w, null);
                else if (config.workspaceAnimationsDirection == "vertical")
                    workspaces[prev]?.Move(null, -h);

                workspaces[prev]?.Show();

                Animation<Workspace> workspaceAnimation = new(config.workspaceAnimationsDuration, "easeOutQuint");
                if (config.workspaceAnimationsDirection == "horizontal")
                {
                    workspaceAnimation.Add(focusedWorkspace, new POINT2 { X = 0, Y = null },
                        new POINT2 { X = w, Y = null });
                    workspaceAnimation.Add(workspaces[prev], new POINT2 { X = -w, Y = null },
                        new POINT2 { X = 0, Y = null });
                }
                else if (config.workspaceAnimationsDirection == "vertical")
                {
                    workspaceAnimation.Add(focusedWorkspace, new POINT2 { X = null, Y = 0 },
                        new POINT2 { X = null, Y = h });
                    workspaceAnimation.Add(workspaces[prev], new POINT2 { X = null, Y = -h },
                        new POINT2 { X = null, Y = 0 });
                }

                workspaceAnimation.Play().Wait();
                focusedWorkspace.Hide();
                focusedWorkspace = workspaces[prev]!;
                focusedWorkspace?.Update();
                focusedWorkspace?.Redraw();
                focusedWorkspace?.SetFocusedWindow();
            }
            else
            {
                FocusWorkspace(workspaces[prev]!);
            }
        });

        WM_EVENT("FocusPreviousWorkspace");
    }

    public void WindowShown(Window wnd)
    {
        if (wmActions.Count > 0) return;
        if (ShouldWindowBeIgnored(wnd)) return;
        foreach (var wksp in workspaces)
            if (wksp!.windows.Contains(wnd))
            {
                /* This is for cases where an already added window gets focused without direct interaction
                 * for eg say you click a link on your terminal and your default browser is open
                 * in another workspace. The reason why we are handling it here instead of
                 * WindowFocused is because the event emmited is OBJECT_SHOW rather than
                 * EVENT_FOREGROUND_CHANGED
                 * */
                if (wksp != focusedWorkspace) FocusWorkspace(wksp);
                return;
            }

        // Add() and CleanGhostWindows() can cause windows to be re added if they
        // occur while the other hasnt completed, so lock them
        lock (addLock)
        {
            ApplyConfigsToWindow(wnd);
            wnd.workspace = focusedWorkspaceIndex;
            focusedWorkspace.Add(wnd);
            if (wnd.floating) focusedWorkspace.MakeFloating(wnd);
            SuppressEvents(() => focusedWorkspace.Update());
        }

        CleanGhostWindows();
        WM_EVENT($"WindowShown, wnd: {wnd.title}, exe: {wnd.exe}");
    }

    public void WindowHidden(Window wnd)
    {
        /* we shouldn'd filter out by ShouldWindowBeIgnored() and in WindowDestroyed
         * here because windows that get hidden or destroyed might meet the
         * ignorable criteria
         * */
        if (wmActions.Count > 0) return;
        if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;

        if (focusedWorkspace.windows.Contains(wnd))
        {
            focusedWorkspace.Remove(wnd);
            SuppressEvents(() => focusedWorkspace.Update());
        }

        CleanGhostWindows();
        WM_EVENT($"WindowHidden, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowDestroyed(Window wnd)
    {
        if (wmActions.Count > 0) return;
        if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;

        if (focusedWorkspace.windows.Contains(wnd))
        {
            focusedWorkspace.Remove(wnd);
            SuppressEvents(() => focusedWorkspace.Update());
        }

        CleanGhostWindows();
        WM_EVENT($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    // window handlers should onlu check window properties of the the already stored window
    public void WindowMoved(Window wnd)
    {
        if (wmActions.Count > 0) return;
        if (ShouldWindowBeIgnored(wnd)) return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null) return;

        //Logger.Log($"WindowMoved, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

        /* wnd -> window being moved
         * cursorPos
         * wndEnclosingCursor -> window enclosing cursor
         * */
        if (!wnd.floating && wnd.resizeable)
        {
            User32.GetCursorPos(out var pt);
            var wndUnderCursor = focusedWorkspace.GetWindowFromPoint(pt);
            if (wndUnderCursor == null) return;
            SuppressEvents(() => focusedWorkspace.SwapWindows(wnd, wndUnderCursor));
        }

        SuppressEvents(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowMoved, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowMaximized(Window wnd)
    {
        if (wmActions.Count > 0) return;
        if (ShouldWindowBeIgnored(wnd)) return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null) return;

        SuppressEvents(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowMaximized, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowMinimized(Window wnd)
    {
        if (wmActions.Count > 0) return;
        if (ShouldWindowBeIgnored(wnd)) return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null) return;

        // render only after state has updated (winevent and GetWindowPlacement() is not synchronous)
        TaskEx.WaitUntil(() => wnd.state == SHOWWINDOW.SW_SHOWMINIMIZED).Wait();

        SuppressEvents(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowMinimized, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowRestored(Window wnd)
    {
        /* To catch window being restored to normal from mazimized state.
         * will fire continuously, can gobble events that are supposed to be handled by MOVESIZEEND
         * the time filter is important because we dont want to capture movement here
         * only the one-off restore action
         * */

        // ignore window restore events that appear in rapid succession
        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastRestoreTime < WINEVENT_RESTORE_TIMEOUT &&
            wnd.hWnd == lasRestoredhWnd
           )
        {
            lasRestoredhWnd = wnd.hWnd;
            lastRestoreTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (Aviyal.DEBUG) Logger.Log($"ignore window restore, {wnd.title}, {wnd.hWnd}");
            return;
        }

        lasRestoredhWnd = wnd.hWnd;
        lastRestoreTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        if (wmActions.Count > 0) return;
        if (ShouldWindowBeIgnored(wnd)) return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null) return;
        if (mouseDown) return;

        SuppressEvents(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowRestored, wnd: {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void Start()
    {
        if (initWindows == null)
        {
            initWindows = GetVisibleWindows()!;
            initWindows = initWindows
                .Where(wnd => !ShouldWindowBeIgnored(wnd))
                .ToList();
            initWindows.ForEach(wnd => ApplyConfigsToWindow(wnd));
        }

        /* when running in debug mode, only window containing the title "windowgen" will
         * be managed by the program. This is so that your ide or terminal is left free
         * while testing
         * */
        if (Aviyal.DEBUG) initWindows = initWindows.Where(wnd => wnd.title.Contains(DEBUG_WND_NAME)).ToList();

        for (var i = 0; i < config.workspaces; i++)
        {
            Workspace wksp = new(config);
            switch (config.layout)
            {
                case "dwindle":
                    wksp.layout = new Dwindle(config);
                    break;
            }

            workspaces.Add(wksp);
        }

        // add all windows to 1st workspace
        initWindows.ForEach(wnd =>
        {
            wnd.workspace = 0;
            workspaces.FirstOrDefault()?.windows.Add(wnd);
        });
        FocusWorkspace(workspaces?.FirstOrDefault()!);
    }

    public List<Window?> GetVisibleWindows()
    {
        List<Window?> windows = new();
        var hWnds = Utils.GetAllTaskbarWindows();
        hWnds?.ForEach(hWnd => { windows.Add(new Window(hWnd)); });
        return windows;
    }

    public List<Window?> GetAllWindows()
    {
        List<Window?> windows = new();
        foreach (var wksp in workspaces)
        foreach (var wnd in wksp!.windows)
            windows.Add(wnd);
        return windows;
    }

    /* search for the window in our workspace and give a local reference that
     * has all the valid states set, the window instance emmitted by window event
     * listener gives a blank window that only matches the stateless properties
     * call this in all event handlers that deal with windows events of windows
     * that already exist in the workspace so basically every one except WindowShown
     * */
    private Window? GetAlreadyStoredWindow(Window wnd)
    {
        return focusedWorkspace?.windows?.FirstOrDefault(_wnd => _wnd == wnd);
    }

    /* Atomic actions
     * */
    private void FocusWorkspace(Workspace wksp)
    {
        workspaces.ForEach(wksp => wksp?.Hide());
        wksp.Focus();
        focusedWorkspace = wksp;
    }

    private void ShiftFocusedWindowToWorkspace(int index)
    {
        if (index < 0 || index > workspaces.Count - 1) return;
        var wnd = focusedWorkspace.focusedWindow;
        if (wnd == null) return;
        focusedWorkspace.Remove(wnd);
        wnd.workspace = index;
        workspaces[index]?.Add(wnd);
        FocusWorkspace(workspaces[index]!);
        focusedWorkspace = workspaces[index]!;
        wnd.Focus();
    }

    private void SuppressEvents(Action func)
    {
        if (wmActions.Count > 0)
        {
            Logger.Log("suppressing action because another is going on");
            return;
        }

        Task _t = new(func);
        wmActions.Add(_t);
        _t.Start();
        _t.Wait();
        Thread.Sleep(WINEVENT_DELAY);
        wmActions.Remove(_t);
    }

    public void ShiftFocusedWindowToNextWorkspace()
    {
        var next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
        SuppressEvents(() => ShiftFocusedWindowToWorkspace(next));

        WM_EVENT("ShiftWindowToNextWorkspace");
    }

    public void ShiftFocusedWindowToPreviousWorkspace()
    {
        var prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;
        SuppressEvents(() => ShiftFocusedWindowToWorkspace(prev));

        WM_EVENT("ShiftWindowToPreviousWorkspace");
    }

    public void CloseFocusedWindow()
    {
        SuppressEvents(() =>
        {
            focusedWorkspace.CloseFocusedWindow();
            WM_EVENT("CloseFocusedWindow");
        });
    }

    public void FocusAdjacentWindow(EDGE direction)
    {
        SuppressEvents(() =>
        {
            focusedWorkspace.FocusAdjacentWindow(direction);
            WM_EVENT("FocusAdjacentWindow");
        });
    }

    public void ToggleFloating()
    {
        SuppressEvents(() =>
        {
            focusedWorkspace.ToggleFloating();
            WM_EVENT("ToggleFloating");
        });
    }

    public void Update()
    {
        SuppressEvents(() =>
        {
            focusedWorkspace.Update();
            WM_EVENT("Update");
        });
    }

    public void ShiftFocusedWindowBy(int shiftBy)
    {
        SuppressEvents(() =>
        {
            focusedWorkspace.ShiftFocusedWindowBy(shiftBy);
            WM_EVENT("ShiftFocusedWindowBy");
        });
    }

    /*
     * Window events apparatus
     * */

    private bool IsWindowInConfigRules(Window wnd, string ruleType)
    {
        var rules = config.rules.Where(rule => rule.type == ruleType).ToList();

        foreach (var rule in rules)
        {
            Func<string, string, bool> condition = rule.method switch
            {
                "equals" => (wndAttribute, identifier) => wndAttribute == identifier,
                "contains" => (wndAttribute, identifier) => wndAttribute.Contains(identifier),
                _ => (x, y) => false
            };

            var wndAttribute = rule.identifierType switch
            {
                "windowProcess" => wnd.exeName,
                "windowTitle" => wnd.title,
                "windowClass" => wnd.className,
                _ => ""
            };
            if (condition(wndAttribute, rule.identifier)) return true;
        }

        return false;
    }

    /* filter out windows that should never be interacted with.
     * This is our guardian, the first line of defence keeping unwanted and evil
     * windows from entering into our manager.
     * */
    private bool ShouldWindowBeIgnored(Window wnd)
    {
        bool IgnoreWindow(string reason)
        {
            if (Aviyal.DEBUG)
                Logger.Log($"Ignoring wnd, [{wnd.title}, {wnd.className}] due to: {reason}");
            return true;
        }

        /* not required actually because WINDOW_ADDED only fires on OBJECT_SHOW
         * however adding for completeness.
         * The reason we check for visibility despite the fact that a normal window
         * can also be invisible is because ShouldWindowBeIgnored() is basically an event
         * filter, and only events emitted by visible windows should be managed. Any normal
         * invisible window (the ones we hide ourselves as part of managing it) would anyway
         * emit events such as OBJECT_SHOW. i.e. we only manage windows in a valid state,
         * merely being normal is not enough
         * */
        if (!wnd.styles.HasFlag(WINDOWSTYLE.WS_VISIBLE)) return IgnoreWindow("INVISIBLE WINDOW");
        if (wnd.styles.HasFlag(WINDOWSTYLE.WS_CHILD)) return IgnoreWindow("CHILD WINDOW");

        /* all normal top level windows must have either "WS_OVERLAPPED" - OR - "WS_POPUP"
         * so kick out windows that dont have neither
         * WS_OVERLAPPED is the default style with which you get a normal window
         * since WS_OVERLAPPED = 0x00000000L it must be checked by the absence of both
         * WS_POPUP and WS_CHILD
         * */
        var isOverlapped = ((uint)wnd.styles & ((uint)WINDOWSTYLE.WS_POPUP | (uint)WINDOWSTYLE.WS_CHILD)) == 0;
        if (!isOverlapped &&
            !wnd.styles.HasFlag(WINDOWSTYLE.WS_POPUP)
           ) return IgnoreWindow("NEITHER OVERLAPPED NOR POPUP");

        /* ignore all toolwindows and topmost windows since these generally are supposed
         * to be visible at all times.
         * */
        if (wnd.exStyles.HasFlag(WINDOWSTYLEEX.WS_EX_TOOLWINDOW)) return IgnoreWindow("TOOLWINDOW");
        if (wnd.exStyles.HasFlag(WINDOWSTYLEEX.WS_EX_TOPMOST)) return IgnoreWindow("TOPMOST");

        if (wnd.className == null || wnd.className == "") return IgnoreWindow("NO CLASSNAME");

        if (wnd.className.Contains("#32770") &&
            !wnd.styles.HasFlag(WINDOWSTYLE.WS_SYSMENU) &&
            (wnd.rect.Bottom - wnd.rect.Top < 50 ||
             wnd.rect.Right - wnd.rect.Left < 50)
           ) return IgnoreWindow("DIALOG"); // dialogs

        // tooltips
        // https://learn.microsoft.com/en-us/windows/win32/controls/common-control-window-classes
        if (wnd.className.Contains("MicrosoftWindowsTooltip") ||
            wnd.className.Contains("tooltips_class32")
           ) return IgnoreWindow("TOOLTIP");

        // menus
        // https://learn.microsoft.com/en-us/windows/win32/winmsg/about-window-classes
        if (wnd.className.Contains("#32768") ||
            wnd.className.Contains("#32772")
           ) return IgnoreWindow("MENUS");

        // filter out windows without the normal/default border thickness
        const int SM_CXSIZEFRAME = 32;
        if (wnd.borderThickness < User32.GetSystemMetrics(SM_CXSIZEFRAME))
            return IgnoreWindow("BORDERLESS");

        if (IsWindowInConfigRules(wnd, "ignore")) return IgnoreWindow("IN CONFIG RULES");

        return false;
    }

    public void CleanGhostWindows()
    {
        lock (addLock)
        {
            var visibleWindows = GetVisibleWindows();

            /* visible windows will give all alt-tab programs, even tool windows
             * which we dont need and for whom winevents would typically not fire.
             * That is why whe have an '>' instead of an '!='
             * The reason we are doing all this is that for some windows such as
             * the file explorer, win events wont fire an OBJECT_SHOW when closing
             * */
            if (focusedWorkspace.windows.Count > visibleWindows.Count)
            {
                var ghostWindows = focusedWorkspace.windows.Where(wnd => !visibleWindows.Contains(wnd)).ToList();
                ghostWindows.ForEach(wnd => focusedWorkspace.Remove(wnd!));
                focusedWorkspace.Update();
            }

            // windows that have been added but has gone bad and should be removed
            var rottenWindows = focusedWorkspace.windows.Where(wnd => ShouldWindowBeIgnored(wnd!)).ToList();
            rottenWindows.ForEach(wnd => focusedWorkspace.Remove(wnd!));
        }
    }

    private void ApplyConfigsToWindow(Window wnd)
    {
        wnd.floating = IsWindowInConfigRules(wnd, "floating");
    }

    public event wmEventHandler WM_EVENT = message => { };

    /* This is the best way to capture windows that have been missed by WindowShown(),
     * and by missed I mean those windows which upon arriving at WindowShown were
     * rejected by ShouldWindowBeIgnored() for whatever reason. It is possible for
     * certain windows to appear ignorable for a while (especially at launching)
     * to then be a normal window that should be included. A window could become normal
     * by a lot of means such as EVENT_OBJECT_NAMECHANGE or something and could be handled
     * that way but this is better because if one were to call AddToStoreIfMissed() on
     * events that only fire on "real windows" such as WindowMoved, WindowFocused,
     * WindowRestored, WindowMin and Max, then we'll add the window there.
     * */

    public Window? AddToStoreIfMissed(Window _wnd)
    {
        Window? wnd;
        if ((wnd = GetAlreadyStoredWindow(_wnd)!) == null)
        {
            WindowShown(_wnd!);
            wnd = GetAlreadyStoredWindow(wnd!)!;
        }

        return wnd;
    }

    private Workspace? GetWindowWorkspace(Window wnd)
    {
        return workspaces.FirstOrDefault(wksp => wksp!.windows.Contains(wnd));
    }

    public void WindowFocused(Window wnd)
    {
        if (wmActions.Count > 0) return;
        if (ShouldWindowBeIgnored(wnd)) return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null) return;

        SuppressEvents(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowFocused, {wnd.title}, {wnd.hWnd}");
    }
}

internal enum FillDirection
{
    HORIZONTAL,
    VERTICAL
}