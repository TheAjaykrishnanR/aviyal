/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public class WindowEventsListener
{
	delegate void WINEVENTPROC(
		nint hWinEventHook,
		WINEVENT msg,
		nint hWnd,
		int idObject,
		int idChild,
		uint idEventThread,
		uint dwmsEventTime
	);

	[DllImport("user32.dll", SetLastError = true)]
	static extern nint SetWinEventHook(
		uint eventMin,
		uint eventMax,
		nint hMod,
		WINEVENTPROC proc,
		uint idProcess,
		uint idThread,
		uint dwFlags
	);

	int OBJID_WINDOW = 0;
	int CHILDID_SELF = 0;

	public delegate void WindowEventHandler(Window wnd);

	public event WindowEventHandler WINDOW_ADDED = (wnd) => { };
	public event WindowEventHandler WINDOW_REMOVED = (wnd) => { };
	public event WindowEventHandler WINDOW_MOVED = (wnd) => { };
	public event WindowEventHandler WINDOW_MAXIMIZED = (wnd) => { };
	public event WindowEventHandler WINDOW_MINIMIZED = (wnd) => { };
	public event WindowEventHandler WINDOW_RESTORED = (wnd) => { };
	public event WindowEventHandler WINDOW_FOCUSED = (wnd) => { };

	readonly Lock @eventLock = new();
	void winEventProc(
		nint hWinEventHook,
		WINEVENT msg,
		nint hWnd,
		int idObject,
		int idChild,
		uint idEventThread,
		uint dwmsEventTime)
	{
		if (
			idObject == OBJID_WINDOW &&
			idChild == CHILDID_SELF &&
		)
		{
			lock (@eventLock)
			{
				Console.WriteLine($"WINEVENT: [{msg}], TITLE: {Utils.GetWindowTitleFromHWND(hWnd)}, {hWnd}, CLASS: {Utils.GetClassNameFromHWND(hWnd)}");

				switch (msg)
				{
					case WINEVENT.OBJECT_CREATE:
						break;
					case WINEVENT.OBJECT_SHOW:
						Console.WriteLine($"WINDOW_ADDED: {hWnd}");
						WINDOW_ADDED(new Window(hWnd));
						break;
					case WINEVENT.OBJECT_DESTROY:
						WINDOW_REMOVED(new Window(hWnd));
						break;
					case WINEVENT.EVENT_SYSTEM_MOVESIZEEND:
						WINDOW_MOVED(new Window(hWnd));
						break;
					case WINEVENT.EVENT_SYSTEM_MINIMIZESTART:
						WINDOW_MINIMIZED(new Window(hWnd));
						break;
					case WINEVENT.EVENT_SYSTEM_MINIMIZEEND:
						WINDOW_RESTORED(new Window(hWnd));
						break;
					case WINEVENT.EVENT_OBJECT_LOCATIONCHANGE:
						WINDOWPLACEMENT wndPlmnt = new();
						User32.GetWindowPlacement(hWnd, ref wndPlmnt);
						SHOWWINDOW state = (SHOWWINDOW)wndPlmnt.showCmd;
						if (state == SHOWWINDOW.SW_MAXIMIZE)
						{
							WINDOW_MAXIMIZED(new Window(hWnd)); // to catch windows that might not send OBJECT_SHOW
						}
						if (state == SHOWWINDOW.SW_SHOWNORMAL)
						{
							WINDOW_RESTORED(new Window(hWnd)); // will fire continuously, can gobble events that are supposed to be handled by MOVESIZEEND
						}
						break;
					case WINEVENT.EVENT_SYSTEM_FOREGROUND:
						WINDOW_FOCUSED(new Window(hWnd));
						break;
					case WINEVENT.EVENT_OBJECT_UNCLOAKED:
						WINDOW_ADDED(new Window(hWnd));
						break;
				}
			}
		}
	}

	public Thread thread;
	public void Loop()
	{
		uint WINEVENT_OUTOFCONTEXT = 0;
		Console.WriteLine("SetWinEventHook...");
		nint ret = SetWinEventHook(0x00000001, 0x7FFFFFFF, 0, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT | 0x0001 | 0x0002);
		Console.WriteLine($"hook: {ret}");
		// message loop
		while (true)
		{
			int _ = User32.GetMessage(out MSG msg, 0, 0, 0);
			User32.TranslateMessage(ref msg);
			User32.DispatchMessage(ref msg);
		}
	}

	public WindowEventsListener()
	{
		thread = new(Loop);
		thread.Start();
	}
}

// https://learn.microsoft.com/en-us/windows/win32/winauto/event-constants
enum WINEVENT : uint
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
