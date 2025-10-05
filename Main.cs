using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

class Aviyal
{
	WindowManager wm = new();
	WindowEventsListener wndListener = new();
	KeyEventsListener kbdListener;

	Dictionary<COMMAND, Action> actions { get; }

	public Aviyal()
	{
		actions = new()
		{
			{ COMMAND.FOCUS_NEXT_WORKSPACE, () => wm.FocusNextWorkspace() },
			{ COMMAND.FOCUS_PREVIOUS_WORKSPACE, () => wm.FocusPreviousWorkspace() },
		};
		// in order to recieve window events for windows that
		// already exists while the application is run
		wm.initWindows.ForEach(wnd => wndListener.shown.Add(wnd.hWnd));
		wndListener.WINDOW_ADDED += wm.WindowAdded;
		wndListener.WINDOW_REMOVED += wm.WindowRemoved;

		List<Keymap> keymaps = [
			new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.L], command= COMMAND.FOCUS_NEXT_WORKSPACE },
			new() { keys= [VK.LCONTROL, VK.LSHIFT, VK.H], command= COMMAND.FOCUS_PREVIOUS_WORKSPACE },
		];
		kbdListener = new(keymaps);
		kbdListener.HOTKEY_PRESSED += HotkeyPressed;
	}

	public void HotkeyPressed(Keymap keymap)
	{
		actions[keymap.command]?.Invoke();
	}

	static void Main(string[] args)
	{
		Shcore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
		Aviyal aviyal = new();
		while (Console.ReadLine() != ":q") { }
	}
}

public enum COMMAND
{
	FOCUS_NEXT_WORKSPACE,
	FOCUS_PREVIOUS_WORKSPACE
}
