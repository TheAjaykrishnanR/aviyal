using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class Aviyal
{
	WindowManager wm = new();
	WindowEventsListener wndListener = new();
	KeyEventsListener kbdListener;

	Dictionary<COMMAND, Action> actions { get; }

	public Aviyal(List<Keymap> keymaps)
	{
		actions = new()
		{
			// focus workspaces
			{ COMMAND.FOCUS_NEXT_WORKSPACE, () => wm.FocusNextWorkspace() },
			{ COMMAND.FOCUS_PREVIOUS_WORKSPACE, () => wm.FocusPreviousWorkspace() },
			// close window
			{ COMMAND.CLOSE_FOCUSED_WINDOW, () => wm.CloseFocusedWindow() },
			// focus window
			{ COMMAND.FOCUS_LEFT_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.LEFT) },
			{ COMMAND.FOCUS_TOP_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.TOP) },
			{ COMMAND.FOCUS_RIGHT_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.RIGHT) },
			{ COMMAND.FOCUS_BOTTOM_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.BOTTOM) },

		};
		// in order to recieve window events for windows that
		// already exists while the application is run
		wm.initWindows.ForEach(wnd => wndListener.shown.Add(wnd.hWnd));
		wndListener.WINDOW_ADDED += wm.WindowAdded;
		wndListener.WINDOW_REMOVED += wm.WindowRemoved;

		kbdListener = new(keymaps);
		kbdListener.HOTKEY_PRESSED += HotkeyPressed;
	}

	public void HotkeyPressed(Keymap keymap)
	{
		actions[keymap.command]?.Invoke();
	}

	static void Main(string[] args)
	{
		if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
		{
			Console.WriteLine("an instance is already running, exiting...");
			return;
		}

		Config config = null;
		if (File.Exists(Paths.configFile))
		{
			string jsonString = File.ReadAllText(Paths.configFile);
			config = Config.FromJson(jsonString);
			Console.WriteLine("Read config: ");
		}
		else
		{
			config = new();
			Console.WriteLine("Default config: ");
			File.AppendAllText(Paths.configFile, config.ToJson());
		}
		Console.WriteLine(config.ToJson());

		Shcore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);

		Aviyal aviyal = new(config.keymaps);
		while (Console.ReadLine() != ":q") { }
	}
}

public enum COMMAND
{
	FOCUS_NEXT_WORKSPACE,
	FOCUS_PREVIOUS_WORKSPACE,
	CLOSE_FOCUSED_WINDOW,
	FOCUS_RIGHT_WINDOW,
	FOCUS_TOP_WINDOW,
	FOCUS_LEFT_WINDOW,
	FOCUS_BOTTOM_WINDOW,

	EXEC,
}
