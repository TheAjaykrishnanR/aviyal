using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class MouseEventsListener : IDisposable
{
    MOUSEPROC mouseProcDelegate;

    bool letEventPass = true;

    int MouseProc(int code, nint wparam, nint lparam)
    {
        letEventPass = true;
        var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lparam);
        switch ((WINDOWMESSAGE)wparam)
        {
            case WINDOWMESSAGE.WM_LBUTTONDOWN:
                Task.Run(() => MOUSE_DOWN());
                if (Aviyal.instance!.wm.windowMoveMode || Aviyal.instance.wm.windowResizeMode)
                    letEventPass = false;
                break;
            case WINDOWMESSAGE.WM_LBUTTONUP:
                Task.Run(() => MOUSE_UP());
                break;
        }
        ////Console.WriteLine($"mouseEvent: {(WINDOWMESSAGE)wparam}");
        return letEventPass ? User32.CallNextHookEx(0, code, wparam, lparam) : 1;
    }

    nint hhook;
    bool running = true;

    void Loop()
    {
        const int WH_MOUSE_LL = 14;
        // hmod = 0, hook function is in code
        // dwThreadId = 0, hook all threads
        hhook = User32.SetWindowsHookExA(
            WH_MOUSE_LL,
            mouseProcDelegate,
            Process.GetCurrentProcess().MainModule.BaseAddress,
            0
        );
        // always use a message pump, instead of: while(Console.ReadLine() != ":q") { }
        while (running)
        {
            int _ = User32.GetMessage(out uint msg, 0, 0, 0);
            User32.TranslateMessage(ref msg);
            User32.DispatchMessage(ref msg);
        }
    }

    public delegate void MouseEventHandler();
    public event MouseEventHandler MOUSE_DOWN = () => { };
    public event MouseEventHandler MOUSE_UP = () => { };

    public Thread thread;

    public MouseEventsListener()
    {
        mouseProcDelegate = new(MouseProc);
        thread = new(Loop);
        thread.Start();
    }

    public void Dispose()
    {
        User32.UnhookWindowsHookEx(hhook);
        running = false;
    }
}

public class MouseEvent { }

[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
    POINT pt;
    uint mouseData;
    uint flags;
    uint time;
    nint dwExtraInfo;
}
