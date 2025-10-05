using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

class Aviyal
{
	static void Main(string[] args)
	{
		WindowManager wm = new();
		WindowEventsListener wndListener = new();
		KeyEventsListener kbdListener = new();

		// in order to recieve window events for windows that
		// already exists while the application is run
		wm.initWindows.ForEach(wnd => wndListener.shown.Add(wnd.hWnd));

		wndListener.WINDOW_ADDED += wm.WindowAdded;
		wndListener.WINDOW_REMOVED += wm.WindowRemoved;
		kbdListener.HOTKEY_PRESSED += wm.HotkeyPressed;

		while (Console.ReadLine() != ":q") { }
	}
}
