/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

public class MouseEventsListener
{
	delegate int MouseProc(int code, nint wparam, nint lparam);

	[DllImport("user32.dll", SetLastError = true)]
	static extern nint SetWindowsHookExA(int idHook, MouseProc lpfn, nint hmod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	static extern int CallNextHookEx(nint hhk, int nCode, nint wparam, nint lparam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetMessage(out uint msg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool TranslateMessage(ref uint msg);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DispatchMessage(ref uint msg);

	readonly Lock @eventLock = new();
	int MouseCallback(int code, nint wparam, nint lparam)
	{
		lock (@eventLock)
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
			//Console.WriteLine($"mouseEvent: {(WINDOWMESSAGE)wparam}");
			return CallNextHookEx(0, code, wparam, lparam);
		}
	}

	void Loop()
	{
		const int WH_MOUSE_LL = 14;
		// hmod = 0, hook function is in code
		// dwThreadId = 0, hook all threads
		nint hhook = SetWindowsHookExA(WH_MOUSE_LL, MouseCallback, Process.GetCurrentProcess().MainModule.BaseAddress, 0);
		// always use a message pump, instead of: while(Console.ReadLine() != ":q") { }
		while (true)
		{
			int _ = GetMessage(out uint msg, 0, 0, 0);
			TranslateMessage(ref msg);
			DispatchMessage(ref msg);
		}
	}

	public delegate void MouseEventHandler();
	public event MouseEventHandler MOUSE_DOWN = () => { };
	public event MouseEventHandler MOUSE_UP = () => { };

	Thread thread;
	public MouseEventsListener()
	{
		thread = new(Loop);
		thread.Start();
	}
}

public class MouseEvent
{
}

[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
	POINT pt;
	uint mouseData;
	uint flags;
	uint time;
	nint dwExtraInfo;
}



