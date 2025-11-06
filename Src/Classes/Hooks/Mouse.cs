using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public class MouseEventsListener : IDisposable
{
    public delegate void MouseEventHandler();

    private readonly Lock eventLock = new();

    private nint hhook;
    private bool running = true;

    public Thread thread;

    public MouseEventsListener()
    {
        thread = new Thread(Loop);
        thread.Start();
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(hhook);
        running = false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookExA(int idHook, MouseProc lpfn, nint hmod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int UnhookWindowsHookEx(nint hhook);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int CallNextHookEx(nint hhk, int nCode, nint wparam, nint lparam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetMessage(out uint msg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool TranslateMessage(ref uint msg);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DispatchMessage(ref uint msg);

    private int MouseCallback(int code, nint wparam, nint lparam)
    {
        lock (eventLock)
        {
            var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lparam);
            switch ((WINDOWMESSAGE)wparam)
            {
                case WINDOWMESSAGE.WM_LBUTTONDOWN:
                    MOUSE_DOWN();
                    break;
                case WINDOWMESSAGE.WM_LBUTTONUP:
                    MOUSE_UP();
                    break;
            }

            ////Console.WriteLine($"mouseEvent: {(WINDOWMESSAGE)wparam}");
            return CallNextHookEx(0, code, wparam, lparam);
        }
    }

    private void Loop()
    {
        const int WH_MOUSE_LL = 14;
        // hmod = 0, hook function is in code
        // dwThreadId = 0, hook all threads
        hhook = SetWindowsHookExA(WH_MOUSE_LL, MouseCallback, Process.GetCurrentProcess().MainModule.BaseAddress, 0);
        // always use a message pump, instead of: while(Console.ReadLine() != ":q") { }
        while (running)
        {
            var _ = GetMessage(out var msg, 0, 0, 0);
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    public event MouseEventHandler MOUSE_DOWN = () => { };
    public event MouseEventHandler MOUSE_UP = () => { };

    private delegate int MouseProc(int code, nint wparam, nint lparam);
}

public class MouseEvent
{
}

[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
    private POINT pt;
    private uint mouseData;
    private uint flags;
    private uint time;
    private nint dwExtraInfo;
}