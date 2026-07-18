using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public class MouseEventsListener : IDisposable
{
    delegate int MOUSEPROC(int code, nint wparam, nint lparam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern nint SetWindowsHookExA(int idHook, MOUSEPROC lpfn, nint hmod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int UnhookWindowsHookEx(nint hhook);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int CallNextHookEx(nint hhk, int nCode, nint wparam, nint lparam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetMessage(
        out uint msg,
        nint hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax
    );

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool TranslateMessage(ref uint msg);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DispatchMessage(ref uint msg);

    MOUSEPROC mouseProcDelegate;

    bool letEventPass;

    int MouseProc(int code, nint wparam, nint lparam)
    {
        letEventPass = true;
        var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lparam);
        switch ((WINDOWMESSAGE)wparam)
        {
            case WINDOWMESSAGE.WM_LBUTTONDOWN:
                Task.Run(() => MOUSE_DOWN());
                if (Aviyal.instance!.wm.windowMoveMode)
                    letEventPass = false;
                break;
            case WINDOWMESSAGE.WM_LBUTTONUP:
                Task.Run(() => MOUSE_UP());
                break;
        }
        ////Console.WriteLine($"mouseEvent: {(WINDOWMESSAGE)wparam}");
        return letEventPass ? CallNextHookEx(0, code, wparam, lparam) : 1;
    }

    nint hhook;
    bool running = true;

    void Loop()
    {
        const int WH_MOUSE_LL = 14;
        // hmod = 0, hook function is in code
        // dwThreadId = 0, hook all threads
        hhook = SetWindowsHookExA(
            WH_MOUSE_LL,
            mouseProcDelegate,
            Process.GetCurrentProcess().MainModule.BaseAddress,
            0
        );
        // always use a message pump, instead of: while(Console.ReadLine() != ":q") { }
        while (running)
        {
            int _ = GetMessage(out uint msg, 0, 0, 0);
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
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
        UnhookWindowsHookEx(hhook);
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
