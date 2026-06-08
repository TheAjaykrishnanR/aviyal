using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Window : IWindow, IMoveable
{
    public int workspace;
    public nint hWnd { get; }
    public string title
    {
        get { return Utils.GetWindowTitleFromHWND(this.hWnd); }
    }

    public string className
    {
        get { return Utils.GetClassNameFromHWND(this.hWnd); }
    }
    public string? exe
    {
        get
        {
            // EnumWindowProcess() is hella expensive, and anyway exe wont change
            // for a process once its found
            if (field == null)
            {
                try
                {
                    field =
                        Utils.GetExePathFromHWND(this.hWnd)
                        ?? Utils
                            .EnumWindowProcesses()
                            .FirstOrDefault(wndProcess =>
                                wndProcess.windows.Select(wndp => wndp.hWnd).Contains(this.hWnd)
                            )
                            ?.process.MainModule?.FileName;
                }
                catch (Exception ex)
                {
                    Logger.Log("Couldn't find exe", ex: ex);
                }
            }
            return field;
        }
    }
    public string? exeName
    {
        get
        {
            return new string(@$"{exe}"?.Split(@"\").Last().Reverse().Skip(4).Reverse().ToArray());
        }
    }

    public RECT rect // absolute position
    {
        get
        {
            User32.GetWindowRect(this.hWnd, out RECT _rect);
            return _rect;
        }
    }

    // position of window relative to workspace (without margins)
    public RECT relRect { get; set; }

    public SHOWWINDOW _state
    {
        get
        {
            WINDOWPLACEMENT wndPlmnt = new();
            User32.GetWindowPlacement(this.hWnd, ref wndPlmnt);
            var state = (SHOWWINDOW)wndPlmnt.showCmd;
            return state;
        }
    }

    public WINDOWSTATE state
    {
        get
        {
            field = _state switch
            {
                SHOWWINDOW.SW_HIDE => WINDOWSTATE.HIDDEN,
                SHOWWINDOW.SW_MAXIMIZE => WINDOWSTATE.MAXIMIZED,
                SHOWWINDOW.SW_MINIMIZE => WINDOWSTATE.MINIMIZED,
                SHOWWINDOW.SW_SHOWMINIMIZED => WINDOWSTATE.MINIMIZED,
                SHOWWINDOW.SW_SHOWMINNOACTIVE => WINDOWSTATE.MINIMIZED,
                SHOWWINDOW.SW_FORCEMINIMIZE => WINDOWSTATE.MINIMIZED,
                _ => WINDOWSTATE.NORMAL,
            };

            RECT screen = Utils.GetScreenRect();
            if (field == WINDOWSTATE.NORMAL)
                if (
                    this.rect == screen
                    ||
                    /* windows 11 has a 1px gap at the bottom when taskbar is disabled
                     * or set to auto hide
                     * */
                    new RECT(this.rect) { Bottom = this.rect.Bottom - 1 } == screen
                    || new RECT(this.rect) { Bottom = this.rect.Top - 1 } == screen
                    || new RECT(this.rect) { Bottom = this.rect.Right - 1 } == screen
                    || new RECT(this.rect) { Bottom = this.rect.Left - 1 } == screen
                )
                    field = WINDOWSTATE.FULLSCREEN;

            return field;
        }
    }

    /* "To be or not to be"
     * i have been thinking about whether the should be about if the window CAN actually be resized
     * or if it SHOULD be ?
     * its probably the latter as achieving the former is not so easy.
     * */
    public bool? resizeable
    {
        get
        {
            if (
                this.className.Contains("OperationStatusWindow")
                || // copy, paste status windows
                this.className.Contains("DS_MODALFRAME")
            )
                return false;
            if (
                /* [Long monologue ahead]: only windows that have the WS_THICKFRAME flag are
                 * those that can be easily resized (resize functionality provided by the OS)
                 * However that doesn't mean that they can't be resized through other means i.e.
                 * if the window implements its own resizing through handling WM_NCHITTEST messages.
                 * This is likely how applications like FL Studio does resizing and probing it would
                 * be difficult.
                 * */
                this.styles.HasFlag(WINDOWSTYLE.WS_THICKFRAME)
                ||
                /* However the above problem is partly solved by looking for the WS_MAXIMIZEBOX flag
                 * If this flag exists we can infer that the window is MEANT to be resized irrespective
                 * of how it does so (OS or itself). If the window has this flag but is actually unresizeable
                 * then it will silently fail in the sizing operation so no worries, aaah ! maybe we can
                 * try checking it there ?!?
                 * */
                this.styles.HasFlag(WINDOWSTYLE.WS_MAXIMIZEBOX)
            )
                return true;
            return false;
        }
    }

    public NONTILEDSTATE nonTiledState { get; set; } = NONTILEDSTATE.NONE;

    public int pid
    {
        get
        {
            try
            {
                Process? _p = Process.GetProcessesByName(exeName).FirstOrDefault();
                return _p == null ? 0 : _p.Id;
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to get pid", ex: ex);
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
            if (
                !Environment.IsPrivilegedProcess
                &&
                /* absolute elevation of the process */
                Utils.IsProcessElevated(pid)
            )
                return true;
            return false;
        }
    }

    public WINDOWSTYLE styles
    {
        get { return (WINDOWSTYLE)User32.GetWindowLong(this.hWnd, GETWINDOWLONG.GWL_STYLE); }
    }

    public WINDOWSTYLEEX exStyles
    {
        get { return (WINDOWSTYLEEX)User32.GetWindowLong(this.hWnd, GETWINDOWLONG.GWL_EXSTYLE); }
    }

    public int borderThickness
    {
        get
        {
            User32.GetWindowInfo(this.hWnd, out WINDOWINFO info);
            return info.cxWindowBorders;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (((Window)obj).hWnd == this.hWnd)
            return true;
        return false;
    }

    public static bool operator ==(Window? left, Window? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Window? left, Window? right)
    {
        if (left is null)
            return right is not null;
        return !left.Equals(right);
    }

    public Window(nint hWnd)
    {
        this.hWnd = hWnd;
    }

    /* A useful wrapper for calling window functions multiple times until target condition is
     * achieved
     * */
    private bool DoUntil(
        Action func,
        Func<bool> targetCondition,
        int retries = 10,
        int dt = 10,
        Action? failure = null
    )
    {
        int i = 0;
        while (!targetCondition())
        {
            if (++i > retries)
                break;
            func();
            Thread.Sleep(dt);
        }
        if (!targetCondition())
        {
            failure?.Invoke();
            return false;
        }
        return true;
    }

    public bool Hide()
    {
        ToggleAnimation(false);
        bool success = DoUntil(
            () =>
            {
                User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_HIDE);
            },
            () => !User32.IsWindowVisible(this.hWnd)
        );
        ToggleAnimation(true);
        return success;
    }

    public bool Show()
    {
        ToggleAnimation(false);
        bool success = DoUntil(
            () =>
            {
                User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_SHOWNA);
            },
            () => User32.IsWindowVisible(this.hWnd)
        );
        ToggleAnimation(true);
        return success;
    }

    public bool Focus()
    {
        return DoUntil(
            () =>
            {
                User32.keybd_event(0, 0, 0, Globals.FOREGROUND_FAKE_KEY);
                User32.SetForegroundWindow(this.hWnd);
            },
            () => User32.GetForegroundWindow() == this.hWnd
        );
    }

    const SETWINDOWPOS defaultMoveFlags =
        SETWINDOWPOS.SWP_NOSENDCHANGING
        | SETWINDOWPOS.SWP_NOCOPYBITS
        | SETWINDOWPOS.SWP_ASYNCWINDOWPOS
        | SETWINDOWPOS.SWP_NOACTIVATE
        | SETWINDOWPOS.SWP_NOZORDER;

    public bool Move(RECT pos, bool verify = true, bool redraw = true)
    {
        // remove frame bounds
        RECT margin = GetFrameMargin();
        pos.Left -= margin.Left;
        pos.Top -= margin.Top;
        pos.Right -= margin.Right;
        pos.Bottom -= margin.Bottom;

        SETWINDOWPOS flags = redraw switch
        {
            true => defaultMoveFlags,
            false => defaultMoveFlags | SETWINDOWPOS.SWP_NOREDRAW,
        };

        RECT _before = this.rect; // we store a copy of the wnd rect before the move action
        // so that we can figure out if the action had any effect at all irrespective of
        // whether the target rect dimensions were achieved

        return DoUntil(
            () =>
            {
                User32.SetWindowPos(
                    this.hWnd,
                    0,
                    pos.Left,
                    pos.Top,
                    pos.Right - pos.Left,
                    pos.Bottom - pos.Top,
                    flags
                );
            },
            () => this.rect == pos
        );
    }

    /* set verify to false when doing animations as otherwise it will result in
     * unnecessary retrials as window state is not updated within the very short
     * time durations available in the animation frame interval
     * */

    const SETWINDOWPOS slideFlag = defaultMoveFlags | SETWINDOWPOS.SWP_NOSIZE;

    public bool Move(int? x, int? y, bool verify = true, bool redraw = true)
    {
        if (x == null && y == null)
            return true;
        SETWINDOWPOS flags = redraw switch
        {
            true => slideFlag,
            false => slideFlag | SETWINDOWPOS.SWP_NOREDRAW,
        };

        if (verify)
        {
            return DoUntil(
                () =>
                {
                    User32.SetWindowPos(this.hWnd, 0, x ?? rect.Left, y ?? rect.Top, 0, 0, flags);
                },
                () => this.rect.Left == x && this.rect.Top == y
            );
        }
        else
        {
            User32.SetWindowPos(this.hWnd, 0, x ?? rect.Left, y ?? rect.Top, 0, 0, flags);
        }

        return false;
    }

    public bool Close()
    {
        return DoUntil(
            () =>
            {
                User32.SendMessage(this.hWnd, (uint)WINDOWMESSAGE.WM_CLOSE, 0, 0);
            },
            () => this.exe == null
        );
    }

    // force the window to redraw itself
    public bool Redraw()
    {
        User32.RedrawWindow(
            this.hWnd,
            0,
            0,
            REDRAWWINDOW.INVALIDATE | REDRAWWINDOW.ALLCHILDREN | REDRAWWINDOW.UPDATENOW
        );
        return true;
    }

    public void SetBottom()
    {
        User32.SetWindowPos(
            this.hWnd,
            (nint)SWPZORDER.HWND_BOTTOM,
            0,
            0,
            0,
            0,
            SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE
        );
    }

    public void SetFront()
    {
        User32.SetWindowPos(
            this.hWnd,
            (nint)SWPZORDER.HWND_TOP,
            0,
            0,
            0,
            0,
            SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE
        );
    }

    public void ToggleAnimation(bool flag)
    {
        int attr = 0;
        if (!flag)
            attr = 1;
        Dwmapi.DwmSetWindowAttribute(
            this.hWnd,
            DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED,
            ref attr,
            sizeof(int)
        );
    }

    public RECT GetFrameMargin()
    {
        User32.GetWindowRect(this.hWnd, out RECT rect);
        int size = Marshal.SizeOf<RECT>();
        nint rectPtr = Marshal.AllocHGlobal(size);
        Dwmapi.DwmGetWindowAttribute(
            this.hWnd,
            (uint)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
            rectPtr,
            (uint)size
        );
        RECT rect2 = Marshal.PtrToStructure<RECT>(rectPtr);
        Marshal.FreeHGlobal(rectPtr);

        return (RECT)(rect2 - rect);
    }

    public bool Maximize()
    {
        return DoUntil(
            () =>
            {
                User32.ShowWindow(hWnd, SHOWWINDOW.SW_MAXIMIZE);
            },
            // ugh aesthetically i should be using this.state but whatever
            () => this._state == SHOWWINDOW.SW_MAXIMIZE
        );
    }

    public bool Minimize()
    {
        return DoUntil(
            () =>
            {
                User32.ShowWindow(hWnd, SHOWWINDOW.SW_MINIMIZE);
            },
            () => this._state == SHOWWINDOW.SW_MINIMIZE
        );
    }

    public bool Unmaximize()
    {
        return DoUntil(
            () =>
            {
                User32.ShowWindow(hWnd, SHOWWINDOW.SW_SHOWNORMAL);
            },
            () => this.state == WINDOWSTATE.NORMAL
        );
    }
}

public class Workspace : IWorkspace, IMoveable
{
    public Guid id { get; } = Guid.NewGuid();
    public List<Window?> windows { get; private set; } = new();
    public Window? focusedWindow
    {
        get
        {
            return windows.FirstOrDefault(_wnd => _wnd == new Window(User32.GetForegroundWindow()));
        }
        private set;
    }
    public int? focusedWindowIndex
    {
        get
        {
            int? index = null;
            if (focusedWindow == null)
                return null;
            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i] == focusedWindow)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }
    public ILayout layout { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return this is null;
        if (((Workspace)obj).id == this.id)
            return true;
        return false;
    }

    public static bool operator ==(Workspace left, Workspace right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Workspace left, Workspace right)
    {
        if (left is null)
            return right is not null;
        return !left.Equals(right);
    }

    Config config;
    (int, int) floatingWindowSize; // ideal floating window dimensions

    public Workspace(Config config)
    {
        this.config = config;
        var sizeStrs = config.floatingWindowSize.Split("x");
        floatingWindowSize.Item1 = Convert.ToInt32(sizeStrs[0]);
        floatingWindowSize.Item2 = Convert.ToInt32(sizeStrs[1]);
    }

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

    // applies updated relRects (provided by the layout) to the windows in the workspace
    // since this function has a recursive branch, adding a fail counter for safety
    int update_failCount = 0;
    int MAX_CALLS = 10;

    public void Update()
    {
        update_failCount++;
        if (Aviyal.DEBUG)
            Logger.Log($"update_failCount: {update_failCount}");

        /* all windows in the window manager which are in
         * a workable state.
         * */
        List<Window?> workableWindows = windows
            .Where(wnd => wnd?.resizeable == true)
            .Where(wnd => wnd?.elevated == false)
            .Where(wnd => wnd?.state != WINDOWSTATE.MAXIMIZED)
            .Where(wnd => wnd?.state != WINDOWSTATE.MINIMIZED)
            .Where(wnd => wnd?.state != WINDOWSTATE.FULLSCREEN)
            .ToList();

        /* windows to tile
         * */
        List<Window?> wndsToTile = workableWindows
            .Where(wnd => wnd?.nonTiledState == NONTILEDSTATE.NONE)
            .ToList();

        RECT[] relRects = layout.GetRects(wndsToTile.Count);
        RECT[] rects = layout.ApplyInner(layout.ApplyOuter(relRects.ToArray()));
        for (int i = 0; i < wndsToTile.Count; i++)
        {
            // if a move action fails on a window, ignore it in tiling (set it to floating or somthing)
            // then drop everything and recompute from the beginning
            if (!(bool)wndsToTile[i]?.Move(rects[i]))
            {
                // remember, that this is a window that IS resizeable. windows that end up here
                // usually has problems resizing to the sizes specified by the layout usually because
                // they have some minimum or maximum dimensions set
                wndsToTile[i]?.nonTiledState = NONTILEDSTATE.FLOATING;
                if (update_failCount < MAX_CALLS)
                    Update();
                return;
            }
            wndsToTile[i]!.relRect = relRects[i];
        }

        /* set the relRects of floating windows as their absolute position,
         * this is required so that window animations can move floating windows
         * from their current positions, we must also update the relRects of
         * floating windows from the WindowMoved() event handler.
         * */
        List<Window?> floatingWnds = workableWindows
            .Where(wnd => wnd?.nonTiledState == NONTILEDSTATE.FLOATING)
            .ToList();
        for (int i = 0; i < floatingWnds.Count; i++)
        {
            floatingWnds[i]!.relRect = floatingWnds[i]!.rect;
        }

        /* windows to stack (in non stacked layouts)
         * */
        List<Window?> wndsToStack = workableWindows
            .Where(wnd => wnd?.nonTiledState == NONTILEDSTATE.STACKED)
            .ToList();
        (int sw, int sh) = Utils.GetScreenSize();
        for (int i = 0; i < wndsToStack.Count; i++)
        {
            RECT rect = new()
            {
                Left = config.left,
                Top = config.top,
                Right = sw - config.right,
                Bottom = sh - config.bottom,
            };
            wndsToStack[i]?.Move(rect);
            wndsToStack[i]!.relRect = rect;
        }

        // if the function has reached here that means the update was successful, so
        // reset the fail counter
        update_failCount = 0;
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

    public bool Move(int? x, int? y, bool verify = true, bool redraw = true)
    {
        bool success = true;
        for (int i = 0; i < windows.Count; i++)
        {
            int? absX = windows[i]!.relRect.Left + x;
            int? absY = windows[i]!.relRect.Top + y;
            if (!(bool)windows[i]?.Move(absX, absY, redraw))
                success = false;
        }
        return success;
    }

    private Window? lastFocusedWindow
    {
        get
        {
            // always check if last focused window is actually a window in our
            // current workspace. It is possible that this window have been shifted
            // to another workspace and all of a sudden you will wonder why workspaces
            // that should be empty suddenly have windows. And yes focusing
            // (SetForegroundWindow) can activate hidden windows
            if (windows.Contains(field))
                return field;
            return null;
        }
        set;
    }

    public void SetFocusedWindow()
    {
        if (lastFocusedWindow != null)
            lastFocusedWindow.Focus();
        else
        {
            var wnd = windows?.FirstOrDefault();
            lastFocusedWindow = wnd;
            wnd?.Focus();
        }
    }

    public void CloseFocusedWindow()
    {
        Window? fWnd = focusedWindow;
        int? index = focusedWindowIndex;
        if (index == null)
            return;
        int? toFocus = index > 0 ? index - 1 : 0;
        focusedWindow?.Close();
        windows.ElementAtOrDefault((int)toFocus)?.Focus();
        windows.Remove(fWnd);
        Update();
    }

    public void ToggleFocusedWindowMaximization()
    {
        if (focusedWindow?.state == WINDOWSTATE.FULLSCREEN)
            return;

        if (focusedWindow?.state == WINDOWSTATE.NORMAL)
            focusedWindow?.Maximize();
        else if (focusedWindow?.state == WINDOWSTATE.MAXIMIZED)
            focusedWindow?.Unmaximize();

        Update();
    }

    public void MinimizeFocusedWindow()
    {
        focusedWindow?.Minimize();
        Update();
    }

    public void FocusAdjacentWindow(EDGE direction)
    {
        if (focusedWindowIndex == null)
            return;
        int? index = layout.GetAdjacent((int)focusedWindowIndex, direction);
        if (index != null)
            windows?[(int)index]?.Focus();
    }

    // changes the order of windows in the workspace
    public void ShiftFocusedWindowBy(int shiftBy)
    {
        Window? _fwnd = focusedWindow;
        int? index = focusedWindowIndex;
        if (index == null)
            return;
        index += shiftBy;
        if (index < 0 || index > windows.Count - 1)
            return;
        windows.Remove(_fwnd);
        windows.Insert((int)index, _fwnd);
        Update();
    }

    public void MakeFloating(Window wnd)
    {
        if (!(bool)wnd.resizeable || wnd.state == WINDOWSTATE.MAXIMIZED)
            return;
        wnd.Move(GetCenterRect(floatingWindowSize.Item1, floatingWindowSize.Item2));
    }

    public void ToggleFloating(Window? wnd = null)
    {
        wnd ??= focusedWindow;
        if (wnd == null)
            return;
        wnd.nonTiledState =
            wnd.nonTiledState != NONTILEDSTATE.FLOATING
                ? NONTILEDSTATE.FLOATING
                : NONTILEDSTATE.NONE;
        if (wnd.nonTiledState == NONTILEDSTATE.FLOATING)
            MakeFloating(wnd);
        Update();
    }

    // only one stacked window in a workspace
    private Window? stackedWnd
    {
        get
        {
            if (windows.Contains(field))
                return field;
            return null;
        }
        set;
    }

    public void ToggleStacked(Window? wnd = null)
    {
        if (config.layout == "stack")
            return;
        wnd ??= focusedWindow;
        if (wnd == null)
            windows.ForEach(_wnd =>
            {
                if (_wnd!.nonTiledState == NONTILEDSTATE.STACKED)
                    _wnd!.nonTiledState = NONTILEDSTATE.NONE;
            });
        else
        {
            if (stackedWnd == null)
            {
                wnd.nonTiledState =
                    wnd.nonTiledState != NONTILEDSTATE.STACKED
                        ? NONTILEDSTATE.STACKED
                        : NONTILEDSTATE.NONE;
                windows
                    .Where(_wnd => _wnd != wnd)
                    .ToList()
                    .ForEach(_wnd =>
                    {
                        if (_wnd!.nonTiledState == NONTILEDSTATE.STACKED)
                            _wnd!.nonTiledState = NONTILEDSTATE.NONE;
                    });
                stackedWnd = wnd;
            }
            else
            {
                // if there alread is a stacked window, unstack it instead of
                // making the provided window stacked
                stackedWnd.nonTiledState = NONTILEDSTATE.NONE;
                stackedWnd = null;
            }
        }
        Update();
    }

    RECT GetCenterRect(int w, int h)
    {
        (int sw, int sh) = Utils.GetScreenSize();
        return new()
        {
            Left = (int)((sw - w) / 2),
            Right = (int)((sw + w) / 2),
            Top = (int)((sh - h) / 2),
            Bottom = (int)((sh + h) / 2),
        };
    }

    public void SwapWindows(Window wnd1, Window wnd2)
    {
        if (!windows.Contains(wnd1) || !windows.Contains(wnd2))
            return;
        int wnd1_index = windows.Index().First(iwnd => iwnd.Item == wnd1).Index;
        int wnd2_index = windows.Index().First(iwnd => iwnd.Item == wnd2).Index;
        windows[wnd1_index] = wnd2;
        windows[wnd2_index] = wnd1;
        Update();
    }

    public Window? GetWindowFromPoint(POINT pt)
    {
        return windows.FirstOrDefault(wnd =>
        {
            return wnd?.relRect.Left < pt.X
                && pt.X < wnd?.relRect.Right
                && wnd?.relRect.Top < pt.Y
                && pt.Y < wnd?.relRect.Bottom;
        });
    }
}

public class WindowManager : IWindowManager
{
    public List<Window>? initWindows { get; set; } // initWindows := initial set of windows to start the WM with
    public List<Workspace?> workspaces { get; } = new();
    public Workspace focusedWorkspace { get; private set; }

    // all windows managed by the wm
    public List<Window?> windows
    {
        get
        {
            List<Window?> windows = new();
            foreach (var wksp in workspaces)
            foreach (var wnd in wksp!.windows)
                windows.Add(wnd);
            return windows;
        }
    }

    public int focusedWorkspaceIndex
    {
        get
        {
            int index = 0;
            for (int i = 0; i < workspaces.Count; i++)
            {
                if (workspaces[i]! == focusedWorkspace)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }

    Config config;
    public static string? DEBUG_WND_NAME;

    public WindowManager(Config config)
    {
        this.config = config;
    }

    public void Start()
    {
        if (initWindows == null)
        {
            this.initWindows = GetVisibleWindows()!;
            this.initWindows = this.initWindows.Where(wnd => !ShouldWindowBeIgnored(wnd)).ToList();
            this.initWindows.ForEach(wnd => ApplyConfigsToWindow(wnd));
        }

        /* when running in debug mode, only window containing the title passed after
         * --debug flag will be managed by the program. This is so that your ide or
         * terminal is left free while testing
         * */
        if (Aviyal.DEBUG && DEBUG_WND_NAME != null)
        {
            Logger.Log($"DebugWndName: {DEBUG_WND_NAME}", file: false);
            this.initWindows = this
                .initWindows.Where(wnd => wnd.title.Contains(DEBUG_WND_NAME))
                .ToList();
        }

        for (int i = 0; i < this.config.workspaces; i++)
        {
            Workspace wksp = new(config);
            wksp.layout = config.layout switch
            {
                "dwindle" => new Dwindle(config),
                "stack" => new Stack(config),
                "master" => new Master(config),
                _ => new Dwindle(config),
            };
            workspaces.Add(wksp);
        }
        // add all windows to 1st workspace
        this.initWindows.ForEach(wnd =>
        {
            wnd.workspace = 0;
            workspaces.FirstOrDefault()?.windows.Add(wnd);
        });
        FocusWorkspace(workspaces?.FirstOrDefault()!, "Start()");
    }

    public List<Window?> GetVisibleWindows()
    {
        List<Window?> windows = new();
        List<nint>? hWnds = Utils.GetAllTaskbarWindows();
        hWnds?.ForEach(hWnd =>
        {
            windows.Add(new(hWnd));
        });
        return windows;
    }

    /* search for the window in our workspace and give a local reference that
     * has all the valid states set, the window instance emmitted by window event
     * listener gives a blank window that only matches the stateless properties
     * call this in all event handlers that deal with windows events of windows
     * that already exist in the workspace so basically every one except WindowShown
     * */
    Window? GetAlreadyStoredWindow(Window wnd)
    {
        return focusedWorkspace?.windows?.FirstOrDefault(_wnd => _wnd == wnd);
    }

    /* Atomic actions
     * */
    private void FocusWorkspace(Workspace wksp, string? dbgStr = null)
    {
        workspaces.ForEach(wksp => wksp?.Hide());
        wksp.Focus();
        focusedWorkspace = wksp;
        Logger.Log($"Focusing wksp to {focusedWorkspaceIndex} by {dbgStr}");
    }

    private void ShiftFocusedWindowToWorkspace(int index)
    {
        if (index < 0 || index > workspaces.Count - 1)
            return;
        Window? wnd = focusedWorkspace.focusedWindow;
        if (wnd == null)
            return;
        focusedWorkspace.Remove(wnd);
        wnd.workspace = index;
        workspaces[index]?.Add(wnd);
        FocusWorkspace(workspaces[index]!, "ShiftFocusedWindowToWorkspace()");
        focusedWorkspace = workspaces[index]!;
        wnd.Focus();
    }

    /* Only wrap non-atomic composite actions. All public window manager actions must be
     * wrapped.
     * */

    /* wndQueue: list of windows queued for actions
     * */
    bool wmBusy = false;
    ConcurrentQueue<Action> actionQueue = new();
    readonly Lock queueLock = new();
    const int WINEVENT_DELAY = 100;

    void RunQueued(Action action)
    {
        actionQueue.Enqueue(action);
        /* thread filter: only one thread passes through
         * */
        lock (queueLock)
        {
            if (wmBusy)
            {
                Logger.Log($"wm is busy..., actionQueue.Count: {actionQueue.Count}");
                return;
            }
            wmBusy = true;
        }
        while (true)
        {
            while (actionQueue.TryDequeue(out var _action))
            {
                try
                {
                    _action();
                    Thread.Sleep(WINEVENT_DELAY);
                }
                catch (Exception ex)
                {
                    Logger.Log("wm: action() failed", ex: ex, logType: LogType.ERROR);
                }
            }
            lock (queueLock)
            {
                if (actionQueue.IsEmpty)
                {
                    wmBusy = false;
                    break;
                }
            }
        }
    }

    /*
     * Public actions offered by the window manager
     * */

    public void FocusWorkspace(int workspaceIndex)
    {
        if (workspaceIndex < 0 || workspaceIndex > workspaces.Count - 1)
            return;
        //SuppressEvents(() => FocusWorkspace(workspaces[workspaceIndex]!, "WmPublic"));
        RunQueued(() => FocusWorkspace(workspaces[workspaceIndex]));

        WM_EVENT("FocusWorkspace");
    }

    public void FocusNextWorkspace()
    {
        int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
        int prev = focusedWorkspaceIndex > 0 ? focusedWorkspaceIndex - 1 : workspaces.Count - 1;

        RunQueued(() =>
        {
            if (config.workspaceAnimations)
            {
                // slide windows left -> if horizontal
                // slide windows up -> if vertical
                (int w, int h) = Utils.GetScreenSize();
                if (config.workspaceAnimationsDirection == "horizontal")
                    workspaces[next]?.Move(w, null);
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

                Animation<Workspace> workspaceAnimation = new(
                    config.workspaceAnimationsDuration,
                    "easeOutQuint"
                );
                if (config.workspaceAnimationsDirection == "horizontal")
                {
                    workspaceAnimation.Add(
                        focusedWorkspace,
                        new POINT2() { X = 0, Y = null },
                        new POINT2() { X = -w, Y = null }
                    );
                    workspaceAnimation.Add(
                        workspaces[next],
                        new POINT2() { X = w, Y = null },
                        new POINT2() { X = 0, Y = null }
                    );
                }
                else if (config.workspaceAnimationsDirection == "vertical")
                {
                    workspaceAnimation.Add(
                        focusedWorkspace,
                        new POINT2() { X = null, Y = 0 },
                        new POINT2() { X = null, Y = -h }
                    );
                    workspaceAnimation.Add(
                        workspaces[next],
                        new POINT2() { X = null, Y = h },
                        new POINT2() { X = null, Y = 0 }
                    );
                }

                workspaceAnimation.Play();
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
        int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
        int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;

        RunQueued(() =>
        {
            if (config.workspaceAnimations)
            {
                // move right
                // move down
                (int w, int h) = Utils.GetScreenSize();
                if (config.workspaceAnimationsDirection == "horizontal")
                    workspaces[prev]?.Move(-w, null);
                else if (config.workspaceAnimationsDirection == "vertical")
                    workspaces[prev]?.Move(null, -h);

                workspaces[prev]?.Show();

                Animation<Workspace> workspaceAnimation = new(
                    config.workspaceAnimationsDuration,
                    "easeOutQuint"
                );
                if (config.workspaceAnimationsDirection == "horizontal")
                {
                    workspaceAnimation.Add(
                        focusedWorkspace,
                        new POINT2() { X = 0, Y = null },
                        new POINT2() { X = w, Y = null }
                    );
                    workspaceAnimation.Add(
                        workspaces[prev],
                        new POINT2() { X = -w, Y = null },
                        new POINT2() { X = 0, Y = null }
                    );
                }
                else if (config.workspaceAnimationsDirection == "vertical")
                {
                    workspaceAnimation.Add(
                        focusedWorkspace,
                        new POINT2() { X = null, Y = 0 },
                        new POINT2() { X = null, Y = h }
                    );
                    workspaceAnimation.Add(
                        workspaces[prev],
                        new POINT2() { X = null, Y = -h },
                        new POINT2() { X = null, Y = 0 }
                    );
                }

                workspaceAnimation.Play();
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

    public void ShiftFocusedWindowToNextWorkspace()
    {
        int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
        RunQueued(() => ShiftFocusedWindowToWorkspace(next));

        WM_EVENT("ShiftWindowToNextWorkspace");
    }

    public void ShiftFocusedWindowToPreviousWorkspace()
    {
        int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;
        RunQueued(() => ShiftFocusedWindowToWorkspace(prev));

        WM_EVENT("ShiftWindowToPreviousWorkspace");
    }

    public void ShiftFocusedWindowToNumWorkspace(int num)
    {
        RunQueued(() => ShiftFocusedWindowToWorkspace(num));

        WM_EVENT($"ShiftWindowToNumWorkspace, wksp: {num}");
    }

    public void CloseFocusedWindow()
    {
        RunQueued(() =>
        {
            focusedWorkspace.CloseFocusedWindow();
        });
        WM_EVENT("CloseFocusedWindow");
    }

    public void FocusAdjacentWindow(EDGE direction)
    {
        RunQueued(() =>
        {
            focusedWorkspace.FocusAdjacentWindow(direction);
        });
        WM_EVENT("FocusAdjacentWindow");
    }

    public void ToggleFloating()
    {
        RunQueued(() =>
        {
            focusedWorkspace.ToggleFloating();
        });
        WM_EVENT("ToggleFloating");
    }

    public void ToggleFocusedWindowMaximization()
    {
        RunQueued(() => focusedWorkspace?.ToggleFocusedWindowMaximization());
        WM_EVENT("MaximizeFocusedWindow");
    }

    public void MinimizeFocusedWindow()
    {
        RunQueued(() => focusedWorkspace?.MinimizeFocusedWindow());
        WM_EVENT("MinimizeFocusedWindow");
    }

    public void ToggleStacked()
    {
        RunQueued(() =>
        {
            focusedWorkspace.ToggleStacked();
        });
        WM_EVENT("ToggleStack");
    }

    public void Update()
    {
        RunQueued(() =>
        {
            focusedWorkspace.Update();
        });
        WM_EVENT("Update");
    }

    public void ShiftFocusedWindowBy(int shiftBy)
    {
        RunQueued(() =>
        {
            focusedWorkspace.ShiftFocusedWindowBy(shiftBy);
        });
        WM_EVENT("ShiftFocusedWindowBy");
    }

    /*
     * Window events apparatus
     * */

    bool IsWindowInConfigRules(Window wnd, string ruleType)
    {
        var rules = config.rules.Where(rule => rule.type == ruleType).ToList();

        foreach (var rule in rules)
        {
            Func<string, string, bool> condition = rule.method switch
            {
                "equals" => (wndAttribute, identifier) => wndAttribute == identifier,
                "contains" => (wndAttribute, identifier) => wndAttribute.Contains(identifier),
                _ => (x, y) => false,
            };

            string? wndAttribute = rule.identifierType switch
            {
                "windowProcess" => wnd.exeName,
                "windowTitle" => wnd.title,
                "windowClass" => wnd.className,
                _ => "",
            };
            if (condition(wndAttribute!, rule.identifier))
                return true;
        }
        return false;
    }

    /* filter out windows that should never be interacted with.
     * This is our guardian, the first line of defence keeping unwanted and evil
     * windows from entering into our manager.
     * */
    bool ShouldWindowBeIgnored(Window wnd)
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
        if (!wnd.styles.HasFlag(WINDOWSTYLE.WS_VISIBLE))
            return IgnoreWindow("INVISIBLE WINDOW");
        if (wnd.styles.HasFlag(WINDOWSTYLE.WS_CHILD))
            return IgnoreWindow("CHILD WINDOW");

        /* all normal top level windows must have either "WS_OVERLAPPED" - OR - "WS_POPUP"
         * so kick out windows that dont have neither
         * WS_OVERLAPPED is the default style with which you get a normal window
         * since WS_OVERLAPPED = 0x00000000L it must be checked by the absence of both
         * WS_POPUP and WS_CHILD
         * */
        bool isOverlapped =
            ((uint)wnd.styles & ((uint)WINDOWSTYLE.WS_POPUP | (uint)WINDOWSTYLE.WS_CHILD)) == 0;
        if (!isOverlapped && !wnd.styles.HasFlag(WINDOWSTYLE.WS_POPUP))
            return IgnoreWindow("NEITHER OVERLAPPED NOR POPUP");

        /* ignore all toolwindows and topmost windows since these generally are supposed
         * to be visible at all times.
         * */
        if (wnd.exStyles.HasFlag(WINDOWSTYLEEX.WS_EX_TOOLWINDOW))
            return IgnoreWindow("TOOLWINDOW");
        if (wnd.exStyles.HasFlag(WINDOWSTYLEEX.WS_EX_TOPMOST))
            return IgnoreWindow("TOPMOST");

        if (wnd.className == null || wnd.className == "")
            return IgnoreWindow("NO CLASSNAME");

        if (
            wnd.className.Contains("#32770")
            && !wnd.styles.HasFlag(WINDOWSTYLE.WS_SYSMENU)
            && (wnd.rect.Bottom - wnd.rect.Top < 50 || wnd.rect.Right - wnd.rect.Left < 50)
        )
            return IgnoreWindow("DIALOG"); // dialogs

        // tooltips
        // https://learn.microsoft.com/en-us/windows/win32/controls/common-control-window-classes
        if (
            wnd.className.Contains("MicrosoftWindowsTooltip")
            || wnd.className.Contains("tooltips_class32")
        )
            return IgnoreWindow("TOOLTIP");

        // menus
        // https://learn.microsoft.com/en-us/windows/win32/winmsg/about-window-classes
        if (wnd.className.Contains("#32768") || wnd.className.Contains("#32772"))
            return IgnoreWindow("MENUS");

        /*
         * - filter out windows without the normal/default border thickness -
         *   had this earlier to prevent pop ups and application startup dialogs be ignored
         *   but filters out real borderless windows like FL Studio
         * */
        // const int SM_CXSIZEFRAME = 32;
        // if (wnd.borderThickness < User32.GetSystemMetrics(SM_CXSIZEFRAME))
        //     return IgnoreWindow("BORDERLESS");

        if (IsWindowInConfigRules(wnd, "ignore"))
            return IgnoreWindow("IN CONFIG RULES");

        return false;
    }

    readonly Lock @addLock = new();

    public void CleanGhostWindows()
    {
        lock (@addLock)
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
                var ghostWindows = focusedWorkspace
                    .windows.Where(wnd => !visibleWindows.Contains(wnd))
                    .ToList();
                ghostWindows.ForEach(wnd => focusedWorkspace.Remove(wnd!));
                focusedWorkspace.Update();
            }

            // windows that have been added but has gone bad and should be removed
            var rottenWindows = focusedWorkspace
                .windows.Where(wnd => ShouldWindowBeIgnored(wnd!))
                .ToList();
            rottenWindows.ForEach(wnd => focusedWorkspace.Remove(wnd!));
        }
    }

    void ApplyConfigsToWindow(Window wnd)
    {
        if (IsWindowInConfigRules(wnd, "floating"))
            wnd.nonTiledState = NONTILEDSTATE.FLOATING;
    }

    public delegate void wmEventHandler(string message);
    public event wmEventHandler WM_EVENT = (message) => { };

    /* Basic Event Handler Layout:
     * 1. Reject invalid windows using ShouldWindowBeIgnored()
     * 2. check if window is already in, if so just update focusedWorkspace
     * */

    public void WindowShown(Window wnd)
    {
        if (wmBusy)
            return;
        if (ShouldWindowBeIgnored(wnd))
            return;
        if (windows.Contains(wnd))
        {
            Workspace? wksp = workspaces.FirstOrDefault(wksp => wksp!.windows.Contains(wnd))!;
            /* This is for cases where an already added window gets focused without direct interaction
             * for eg say you click a link on your terminal and your default browser is open
             * in another workspace. The reason why we are handling it here instead of
             * WindowFocused is because the event emmited is OBJECT_SHOW rather than
             * EVENT_FOREGROUND_CHANGED
             * */
            if (wksp != focusedWorkspace && wksp != null)
                RunQueued(() => FocusWorkspace(wksp, "WindowShown()"));

            return;
        }

        // Add() and CleanGhostWindows() can cause windows to be re added if they
        // occur while the other hasnt completed, so lock them
        lock (@addLock)
        {
            ApplyConfigsToWindow(wnd);
            wnd.workspace = focusedWorkspaceIndex;
            focusedWorkspace.Add(wnd);
            switch (wnd.nonTiledState)
            {
                case NONTILEDSTATE.FLOATING:
                    focusedWorkspace.MakeFloating(wnd);
                    break;
            }
            RunQueued(() => focusedWorkspace.Update());
        }

        CleanGhostWindows();
        WM_EVENT($"WindowShown, wnd: {wnd.title}, hWnd: {wnd.hWnd}, exe: {wnd.exe}");
    }

    public void WindowHidden(Window wnd)
    {
        /* we shouldn'd filter out by ShouldWindowBeIgnored() and in WindowDestroyed
         * here because windows that get hidden or destroyed might meet the
         * ignorable criteria
         * */
        if (wmBusy)
            return;
        if ((wnd = GetAlreadyStoredWindow(wnd)!) == null)
            return;

        if (focusedWorkspace.windows.Contains(wnd))
        {
            focusedWorkspace.Remove(wnd);
            RunQueued(() => focusedWorkspace.Update());
        }

        CleanGhostWindows();
        WM_EVENT($"WindowHidden, {wnd.title}, hWnd: {wnd.hWnd}, exe: {wnd.exe}");
    }

    public void WindowDestroyed(Window wnd)
    {
        if (wmBusy)
            return;
        if ((wnd = GetAlreadyStoredWindow(wnd)!) == null)
            return;

        if (focusedWorkspace.windows.Contains(wnd))
        {
            focusedWorkspace.Remove(wnd);
            RunQueued(() => focusedWorkspace.Update());
        }

        CleanGhostWindows();
        WM_EVENT($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    /* This is the best way to capture windows that have been missed by WindowShown(),
     * and by missed I mean those windows which upon arriving at WindowShown were
     * rejected by ShouldWindowBeIgnored() for whatever reason. It is possible for
     * certain windows to appear ignorable for a while (especially at launching)
     * to then be a normal window that should be included. A window could become normal
     * by a lot of means such as EVENT_OBJECT_NAMECHANGE or something and could be handled
     * that way but this is better because if one were to call AddToStoreIfMissed() on
     * events that only fire on "real windows" such as WindowMoved, WindowFocused,
     * WindowRestored, WindowMin and Max, then we'll add the window there.
     *
     * Since we are adding the window by firing the WindowShown() event handler, we do not
     * need to check if the window is a valid one using ShouldWindowBeIgnored(), i.e. only
     * call ShouldWindowBeIgnored() if the window isn't already inside the wm.
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

    // window handlers should only check window properties of the the already stored window
    public void WindowMoved(Window wnd)
    {
        if (wmBusy)
            return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null)
            return;

        /* wnd -> window being moved
         * cursorPos
         * wndEnclosingCursor -> window enclosing cursor
         * */
        if (wnd.nonTiledState == NONTILEDSTATE.NONE && (bool)wnd.resizeable)
        {
            User32.GetCursorPos(out POINT pt);
            Window? wndUnderCursor = focusedWorkspace.GetWindowFromPoint(pt);
            if (wndUnderCursor == null)
                return;
            RunQueued(() => focusedWorkspace.SwapWindows(wnd, wndUnderCursor));
        }
        else if (wnd.nonTiledState == NONTILEDSTATE.FLOATING)
            wnd.relRect = wnd.rect;

        RunQueued(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowMoved, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowMaximized(Window wnd)
    {
        if (wmBusy)
            return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null)
            return;

        RunQueued(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowMaximized, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowMinimized(Window wnd)
    {
        if (wmBusy)
            return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null)
            return;

        // render only after state has updated (winevent and GetWindowPlacement() is not synchronous)
        TaskEx.WaitUntil(() => wnd.state == WINDOWSTATE.MINIMIZED).Wait();

        RunQueued(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowMinimized, {wnd.title}, hWnd: {wnd.hWnd}");
    }

    // window unmaximized
    public bool mouseDown { get; set; } = false;
    const int WINEVENT_RESTORE_TIMEOUT = 500; // milliseconds
    nint lasRestoredhWnd = 0;
    long lastRestoreTime = 0;

    /* a simple class that emits an event with the last member of a rapidly firing event stream
     * */
    class EventStream<TEventObj>
    {
        private System.Timers.Timer _t;
        public delegate void EventStreamOver(TEventObj obj);
        public event EventStreamOver OVER = (_) => { };
        public bool disposed { get; private set; } = false;

        public EventStream(int interval)
        {
            _t = new(interval);
            _t.Elapsed += (o, e) =>
            {
                disposed = true;
                _t.Stop();
                _t.Close();
                if (lastEventObj != null)
                    OVER(lastEventObj);
            };
        }

        private TEventObj lastEventObj;

        public void Add(TEventObj obj)
        {
            _t.Stop();
            lastEventObj = obj;
            _t.Start();
        }
    }

    EventStream<Window> restoreStream;

    public void WindowRestored(Window wnd)
    {
        /* To catch window being restored to normal from mazimized state.
         * will fire continuously, can gobble events that are supposed to be handled by MOVESIZEEND
         * the time filter is important because we dont want to capture movement here
         * only the one-off restore action
         * */

        // +[CHANGE: resize event stream] except the last one as some fricking windows upon
        // being moved wont fire a MOVESIZEEND (FL Studio)
        if (restoreStream == null || restoreStream.disposed)
        {
            restoreStream = new(WINEVENT_RESTORE_TIMEOUT);
            restoreStream.OVER += WindowRestored;
        }
        // ignore window restore events that appear in rapid succession
        if (
            Utils.FastTime_milli() - lastRestoreTime < WINEVENT_RESTORE_TIMEOUT
            && wnd.hWnd == lasRestoredhWnd
        )
        {
            lasRestoredhWnd = wnd.hWnd;
            lastRestoreTime = Utils.FastTime_milli();
            restoreStream?.Add(wnd); // [+ for firing for the last event]
            if (Aviyal.DEBUG)
                Logger.Log($"ignore window restore, {wnd.title}, {wnd.hWnd}");
            return;
        }
        lasRestoredhWnd = wnd.hWnd;
        lastRestoreTime = Utils.FastTime_milli();

        if (wmBusy)
            return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null)
            return;
        if (mouseDown)
            return;

        RunQueued(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowRestored, wnd: {wnd.title}, hWnd: {wnd.hWnd}");
    }

    public void WindowFocused(Window wnd)
    {
        if (wmBusy)
            return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null)
            return;

        RunQueued(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowFocused, {wnd.title}, {wnd.hWnd}");
    }

    public void WindowFullscreened(Window wnd)
    {
        if (wmBusy)
            return;
        if ((wnd = AddToStoreIfMissed(wnd)!) == null)
            return;

        RunQueued(() => focusedWorkspace.Update());
        CleanGhostWindows();
        WM_EVENT($"WindowFullscreened, {wnd.title}, {wnd.hWnd}");
    }
}

public enum WINDOWSTATE
{
    NORMAL,
    MAXIMIZED,
    MINIMIZED,
    FULLSCREEN,
    HIDDEN,
}

enum FillDirection
{
    HORIZONTAL,
    VERTICAL,
}

// a window that is managed without being tiled can be either of these
public enum NONTILEDSTATE
{
    NONE,
    FLOATING,
    STACKED,
}
