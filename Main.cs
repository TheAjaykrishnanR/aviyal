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
	WindowManager wm;
	WindowEventsListener wndListener = new();
	KeyEventsListener kbdListener;

	Dictionary<COMMAND, Action> actions { get; }

	public Aviyal(Config config)
	{
		wm = new(config);

		actions = new()
		{
			{ COMMAND.FOCUS_NEXT_WORKSPACE, () => wm.FocusNextWorkspace() },
			{ COMMAND.FOCUS_PREVIOUS_WORKSPACE, () => wm.FocusPreviousWorkspace() },
			{ COMMAND.CLOSE_FOCUSED_WINDOW, () => wm.focusedWorkspace.CloseFocusedWindow() },
			{ COMMAND.FOCUS_LEFT_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.LEFT) },
			{ COMMAND.FOCUS_TOP_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.TOP) },
			{ COMMAND.FOCUS_RIGHT_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.RIGHT) },
			{ COMMAND.FOCUS_BOTTOM_WINDOW, () => wm.focusedWorkspace.FocusAdjacentWindow(EDGE.BOTTOM) },

			{ COMMAND.SHIFT_FOCUSED_WINDOW_RIGHT, () => wm.focusedWorkspace.ShiftFocusedWindow(+1) },
			{ COMMAND.SHIFT_FOCUSED_WINDOW_LEFT, () => wm.focusedWorkspace.ShiftFocusedWindow(-1) },
			{ COMMAND.SHIFT_WINDOW_NEXT_WORKSPACE, () => wm.ShiftFocusedWindowToWorkspace(wm.focusedWorkspaceIndex+1) },
			{ COMMAND.SHIFT_WINDOW_PREVIOUS_WORKSPACE, () => wm.ShiftFocusedWindowToWorkspace(wm.focusedWorkspaceIndex-1) },
			{ COMMAND.TOGGLE_FLOATING_WINDOW, () => wm.focusedWorkspace.ToggleFloating() },
		};
		// in order to recieve window events for windows that
		// already exists while the application is run
		wm.initWindows.ForEach(wnd => wndListener.shown.Add(wnd.hWnd));
		wndListener.WINDOW_ADDED += wm.WindowAdded;
		wndListener.WINDOW_REMOVED += wm.WindowRemoved;
		wndListener.WINDOW_MOVED += wm.WindowMoved;
		wndListener.WINDOW_MAXIMIZED += wm.WindowMaximized;
		wndListener.WINDOW_MINIMIZED += wm.WindowMinimized;
		wndListener.WINDOW_RESTORED += wm.WindowRestored;

		kbdListener = new(config);
		kbdListener.HOTKEY_PRESSED += HotkeyPressed;

		// just make all windows reappear if crashes
		//AppDomain currentDomain = AppDomain.CurrentDomain;
		//currentDomain.UnhandledException += (s, e) =>
		//{
		//	int i = 0;
		//	wm.workspaces.ForEach(wksp => wksp.windows.ForEach(wnd => { wnd.Show(); i++; }));
		//	Console.WriteLine($"Crash: Restored {i} windows...");

		//	Exception ex = (Exception)e.ExceptionObject;
		//	string text = ex.Message + "\n" + ex.StackTrace;
		//	Console.WriteLine(text);
		//	User32.MessageBox(0, text, "CRASH", 0);
		//	errored = true;
		//};
	}

	public void HotkeyPressed(Keymap keymap)
	{
		if (keymap.command == COMMAND.EXEC) Exec(keymap.arguments);
		else actions[keymap.command]?.Invoke();
	}

	static bool errored = false;
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

		Aviyal aviyal = new(config);

		while (Console.ReadLine() != ":q" && !errored) { }
	}

	public void Exec(List<string> args)
	{
		if (args.Count == 0) return;
		try
		{
			ProcessStartInfo psi = new();
			psi.FileName = args[0];
			//if (args.Count > 0) psi.Arguments = args[1];
			Process process = new();
			process.StartInfo = psi;
			process.Start();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Unable to execute command: {ex.Message}");
			Console.WriteLine(string.Join(", ", args));
		}
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

	SHIFT_FOCUSED_WINDOW_RIGHT,
	SHIFT_FOCUSED_WINDOW_LEFT,

	SHIFT_WINDOW_NEXT_WORKSPACE,
	SHIFT_WINDOW_PREVIOUS_WORKSPACE,

	TOGGLE_FLOATING_WINDOW,

	EXEC,
}
