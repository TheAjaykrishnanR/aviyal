/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

public delegate bool EnumWindowProc(nint hWnd, nint lParam);
public delegate nint WNDPROC(nint hWnd, WINDOWMESSAGE uMsg, nint wParam, nint lParam);
public delegate void TIMERPROC(nint hWnd, uint param2, nint param3, ulong param4);


