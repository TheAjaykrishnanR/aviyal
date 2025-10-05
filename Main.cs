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
		WindowEventsListener wel = new();
		KeyEventsListener kel = new();

		// in order to recieve window events for windows that
		// already exists while the application is run
		wm.initWindows.ForEach(wnd => wel.shown.Add(wnd.hWnd));

		wel.WINDOW_ADDED += wm.WindowAdded;
		wel.WINDOW_REMOVED += wm.WindowRemoved;
		kel.HOTKEY_PRESSED += wm.HotkeyPressed;

		while (Console.ReadLine() != ":q") { }
	}
}
