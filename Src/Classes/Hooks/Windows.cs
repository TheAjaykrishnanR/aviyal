using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public class WindowEventsListener : IDisposable
{
    const int OBJID_WINDOW = 0;
    const int CHILDID_SELF = 0;

    public delegate void WindowEventHandler(Window wnd);

    public event WindowEventHandler WINDOW_SHOWN = (wnd) => { };
    public event WindowEventHandler WINDOW_HIDDEN = (wnd) => { };
    public event WindowEventHandler WINDOW_DESTROYED = (wnd) => { };
    public event WindowEventHandler WINDOW_MOVED = (wnd) => { };
    public event WindowEventHandler WINDOW_MAXIMIZED = (wnd) => { };
    public event WindowEventHandler WINDOW_MINIMIZED = (wnd) => { };
    public event WindowEventHandler WINDOW_RESTORED = (wnd) => { };
    public event WindowEventHandler WINDOW_FOCUSED = (wnd) => { };

    readonly Lock @eventLock = new();
    uint dt = 0;
    uint lastTime = 0;

    WINEVENTPROC winEventProcDelegate;

    void winEventProc(
        nint hWinEventHook,
        WINEVENT msg,
        nint hWnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime
    )
    {
        if (idObject == OBJID_WINDOW && idChild == CHILDID_SELF
        //&& !Utils.GetStylesFromHwnd(hWnd).Contains("WS_CHILD")
        )
        {
            dt = dwmsEventTime - lastTime;
            lastTime = dwmsEventTime;

            Window wnd = new(hWnd);

            //if (Aviyal.DEBUG)
            //	Logger.Log(
            //		$"WINEVENT: [{msg}], TITLE: {Utils.GetWindowTitleFromHWND(hWnd)}, {hWnd}, CLASS: {Utils.GetClassNameFromHWND(hWnd)}, STATE: {wnd.state}, dt: {dt}, time: {Utils.FastTime_milli()}"
            //	);

            lock (@eventLock)
            {
                switch (msg)
                {
                    case WINEVENT.OBJECT_CREATE:
                        break;
                    case WINEVENT.OBJECT_SHOW:
                        WINDOW_SHOWN(wnd);
                        break;
                    case WINEVENT.OBJECT_HIDE:
                        WINDOW_HIDDEN(wnd);
                        break;
                    case WINEVENT.OBJECT_DESTROY:
                        WINDOW_DESTROYED(wnd);
                        break;
                    case WINEVENT.EVENT_SYSTEM_MOVESIZEEND:
                        WINDOW_MOVED(wnd);
                        break;
                    case WINEVENT.EVENT_SYSTEM_MINIMIZESTART:
                        WINDOW_MINIMIZED(wnd);
                        break;
                    case WINEVENT.EVENT_SYSTEM_MINIMIZEEND:
                        WINDOW_RESTORED(wnd);
                        break;
                    case WINEVENT.EVENT_OBJECT_LOCATIONCHANGE:
                        WINDOWPLACEMENT wndPlmnt = new();
                        User32.GetWindowPlacement(hWnd, ref wndPlmnt);
                        SHOWWINDOW state = (SHOWWINDOW)wndPlmnt.showCmd;
                        if (state == SHOWWINDOW.SW_MAXIMIZE)
                        {
                            WINDOW_MAXIMIZED(wnd); // to catch windows that might not send OBJECT_SHOW
                        }
                        else if (state == SHOWWINDOW.SW_SHOWNORMAL)
                        {
                            // we query the windows own state for full screen because
                            // winevents doesnt directly report windows going full screen
                            if (wnd.state == WINDOWSTATE.FULLSCREEN)
                            {
                                Logger.Log($"WINDOW LAUNCHED IN FULLSCREEN: {wnd.title}");
                                break;
                            }

                            WINDOW_RESTORED(wnd);
                        }
                        break;
                    case WINEVENT.EVENT_SYSTEM_FOREGROUND:
                        WINDOW_FOCUSED(wnd);
                        break;
                    case WINEVENT.EVENT_OBJECT_UNCLOAKED:
                        WINDOW_SHOWN(wnd);
                        break;
                }
            }
        }
    }

    public Thread thread;
    nint hhook;
    bool running = true;

    public void Loop()
    {
        uint WINEVENT_OUTOFCONTEXT = 0;
        hhook = User32.SetWinEventHook(
            0x00000001,
            0x7FFFFFFF,
            0,
            winEventProcDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT | 0x0001 | 0x0002
        );
        // message loop
        while (running)
        {
            _ = User32.GetMessage(out MSG msg, 0, 0, 0);
            User32.TranslateMessage(ref msg);
            User32.DispatchMessage(ref msg);
        }
    }

    public WindowEventsListener()
    {
        winEventProcDelegate = new(winEventProc);
        thread = new(Loop);
        thread.Start();
    }

    public void Dispose()
    {
        User32.UnhookWinEvent(hhook);
        running = false;
    }
}

// https://learn.microsoft.com/en-us/windows/win32/winauto/event-constants
public enum WINEVENT : uint
{
    OBJECT_CREATE = 0x8000,
    OBJECT_DESTROY = 0x8001,
    OBJECT_SHOW = 0x8002,
    OBJECT_HIDE = 0x8003,
    EVENT_SYSTEM_MOVESIZEEND = 0x000B,
    EVENT_SYSTEM_MINIMIZESTART = 0x0016,
    EVENT_SYSTEM_MINIMIZEEND = 0x0017,

    // because windows doesnt have a maximize winevent
    EVENT_OBJECT_LOCATIONCHANGE = 0x800B,
    EVENT_SYSTEM_FOREGROUND = 0x0003,
    EVENT_OBJECT_UNCLOAKED = 0x8018,
}
