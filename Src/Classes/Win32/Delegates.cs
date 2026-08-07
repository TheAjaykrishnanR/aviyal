public delegate bool EnumWindowProc(nint hWnd, nint lParam);
public delegate nint WNDPROC(nint hWnd, WINDOWMESSAGE uMsg, nint wParam, nint lParam);
public delegate void TIMERPROC(nint hWnd, uint param2, nint param3, ulong param4);
public delegate int KEYBOARDPROC(int code, nint wparam, nint lparam);
public delegate int MOUSEPROC(int code, nint wparam, nint lparam);
public delegate void WINEVENTPROC(
    nint hWinEventHook,
    WINEVENT msg,
    nint hWnd,
    int idObject,
    int idChild,
    uint idEventThread,
    uint dwmsEventTime
);
