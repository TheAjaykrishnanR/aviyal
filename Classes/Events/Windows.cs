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
	List<nint> created = new();
	List<nint> shown = new();

	public delegate void WindowAddedEventHandler(Window wnd);
	public event WindowAddedEventHandler WINDOW_ADDED = (wnd) => { };

	public delegate void WindowRemovedEventHandler(Window wnd);
	public event WindowRemovedEventHandler WINDOW_REMOVED = (wnd) => { };

	void winEventProc(
		nint hWinEventHook,
		WINEVENT msg,
		nint hWnd,
		int idObject,
		int idChild,
		uint idEventThread,
		uint dwmsEventTime)
	{
		if ((
			msg == WINEVENT.OBJECT_CREATE ||
			msg == WINEVENT.OBJECT_SHOW ||
			msg == WINEVENT.OBJECT_DESTROY) &&
			idObject == OBJID_WINDOW &&
			idChild == CHILDID_SELF &&
			!Utils.GetStylesFromHwnd(hWnd).Contains("WS_CHILD") &&
			Utils.GetStylesFromHwnd(hWnd).Contains("WS_CAPTION")
		)
		{
			switch (msg)
			{
				case WINEVENT.OBJECT_CREATE:
					created.Add(hWnd);
					break;
				case WINEVENT.OBJECT_SHOW:
					if (created.Remove(hWnd))
					{
						shown.Add(hWnd);
						WINDOW_ADDED(new Window(hWnd));
					}
					break;
				case WINEVENT.OBJECT_DESTROY:
					if (shown.Remove(hWnd))
						WINDOW_REMOVED(new Window(hWnd));
					break;
			}
			Console.WriteLine($"WINEVENT: [{msg}], TITLE: {Utils.GetWindowTitleFromHWND(hWnd)}");
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

enum WINEVENT : uint
{
	OBJECT_CREATE = 0x8000,
	OBJECT_DESTROY = 0x8001,
	OBJECT_SHOW = 0x8002,
	OBJECT_HIDE = 0x8003,
}
