/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

class Aviyal : IDisposable
{
	static string ver = "0.1.0";
	static Aviyal? aviyal;

	public WindowManager wm;
	public Server server;

	public WindowEventsListener wndListener = new();
	public KeyEventsListener kbdListener;
	public MouseEventsListener mouseListener = new();

	Dictionary<COMMAND, Action> actions { get; }

	public Aviyal(Config config)
	{
		wm = new(config);
		server = new(config);

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
			{ COMMAND.SHIFT_WINDOW_NEXT_WORKSPACE, () => wm.ShiftFocusedWindowToNextWorkspace() },
			{ COMMAND.SHIFT_WINDOW_PREVIOUS_WORKSPACE, () => wm.ShiftFocusedWindowToPreviousWorkspace() },
			{ COMMAND.TOGGLE_FLOATING_WINDOW, () => wm.focusedWorkspace.ToggleFloating() },

			{ COMMAND.FOCUS_WORKSPACE_1, () => wm.FocusWorkspace(wm.workspaces[0]) },
			{ COMMAND.FOCUS_WORKSPACE_2, () => wm.FocusWorkspace(wm.workspaces[1]) },
			{ COMMAND.FOCUS_WORKSPACE_3, () => wm.FocusWorkspace(wm.workspaces[2]) },
			{ COMMAND.FOCUS_WORKSPACE_4, () => wm.FocusWorkspace(wm.workspaces[3]) },
			{ COMMAND.FOCUS_WORKSPACE_5, () => wm.FocusWorkspace(wm.workspaces[4]) },
			{ COMMAND.FOCUS_WORKSPACE_6, () => wm.FocusWorkspace(wm.workspaces[5]) },
			{ COMMAND.FOCUS_WORKSPACE_7, () => wm.FocusWorkspace(wm.workspaces[6]) },
			{ COMMAND.FOCUS_WORKSPACE_8, () => wm.FocusWorkspace(wm.workspaces[7]) },
			{ COMMAND.FOCUS_WORKSPACE_9, () => wm.FocusWorkspace(wm.workspaces[8]) },

			{ COMMAND.RESTART, () => Restart() },
			{ COMMAND.UPDATE, () => wm.focusedWorkspace.Update() },
		};

		server.REQUEST_RECEIVED += wm.RequestReceived;
		wm.WINDOW_MANAGER_MESSAGE_SENT += (message) => server.Broadcast(message);
		// in order to recieve window events for windows that
		// already exists while the application is run
		//wm.initWindows.ForEach(wnd => wndListener.shown.Add(wnd.hWnd));
		wndListener.WINDOW_SHOWN += wm.WindowShown;
		wndListener.WINDOW_HIDDEN += wm.WindowHidden;
		wndListener.WINDOW_DESTROYED += wm.WindowDestroyed;
		wndListener.WINDOW_MOVED += wm.WindowMoved;
		wndListener.WINDOW_MAXIMIZED += wm.WindowMaximized;
		wndListener.WINDOW_MINIMIZED += wm.WindowMinimized;
		wndListener.WINDOW_RESTORED += wm.WindowRestored;
		wndListener.WINDOW_FOCUSED += wm.WindowFocused;

		kbdListener = new(config);
		kbdListener.HOTKEY_PRESSED += HotkeyPressed;

		mouseListener.MOUSE_DOWN += MouseDown;
		mouseListener.MOUSE_UP += MouseUp;

		// just make all windows reappear if crashes
		AppDomain currentDomain = AppDomain.CurrentDomain;
		currentDomain.UnhandledException += (s, e) =>
		{
			int i = 0;
			wm.workspaces.ForEach(wksp => wksp.windows.ForEach(wnd => { wnd?.Show(); i++; }));
			Console.WriteLine($"Crash: Restored {i} windows...");

			Exception ex = (Exception)e.ExceptionObject;
			string text = ex.Message + "\n" + ex.StackTrace;
			Console.WriteLine(text);
			File.WriteAllText(Paths.errorFile, text);
			errored = true;
		};
	}

	public void Dispose()
	{
		// instances wont be disposed if event handlers are still attached 
		// found out the hard way when couldnt figure out why previous instance
		// configuration persisted onto the next. Turns out it was one of these
		// old event handlers still setting window attributes
		server.REQUEST_RECEIVED -= wm.RequestReceived;
		wm.WINDOW_MANAGER_MESSAGE_SENT -= (message) => server.Broadcast(message);
		wndListener.WINDOW_SHOWN -= wm.WindowShown;
		wndListener.WINDOW_DESTROYED -= wm.WindowDestroyed;
		wndListener.WINDOW_MOVED -= wm.WindowMoved;
		wndListener.WINDOW_MAXIMIZED -= wm.WindowMaximized;
		wndListener.WINDOW_MINIMIZED -= wm.WindowMinimized;
		wndListener.WINDOW_RESTORED -= wm.WindowRestored;
		wndListener.WINDOW_FOCUSED -= wm.WindowFocused;
		kbdListener.HOTKEY_PRESSED -= HotkeyPressed;
		mouseListener.MOUSE_DOWN -= MouseDown;
		mouseListener.MOUSE_UP -= MouseUp;

		server.Dispose(); // release the previous socket
		wndListener.Dispose();
		kbdListener.Dispose();
		mouseListener.Dispose();
	}

	public void HotkeyPressed(Keymap keymap)
	{
		Console.WriteLine($"Hotekey Pressed: {keymap.command}");
		if (keymap.command == COMMAND.EXEC) Exec(keymap.arguments);
		else actions[keymap.command]?.Invoke();
	}

	public void MouseDown() => wm.mouseDown = true;
	public void MouseUp() => wm.mouseDown = false;

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

	static void Run()
	{
		if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
		{
			Console.WriteLine("an instance is already running, exiting...");
			return;
		}

		Console.WriteLine($"Running aviyal instance, reload count: {reloadCount}");

		Config config = null;
		if (File.Exists(Paths.configFile))
		{
			string jsonString = File.ReadAllText(Paths.configFile);
			Console.WriteLine(jsonString);
			try
			{
				config = Config.FromJson(jsonString);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Unable to parse json config file");
				config = new();
			}
		}
		else
		{
			config = new();
			Console.WriteLine("Default config: ");
			File.AppendAllText(Paths.configFile, config.ToJson());
		}

		Shcore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);

		// collect windows to restore when reloaded (when reloaded all windows will be put to workspace 0)
		var windows = aviyal?.wm.GetAllWindows();
		aviyal?.Dispose();
		aviyal = new(config);
		aviyal.wm.initWindows = windows!;
		aviyal.wm.Start();
	}

	static bool errored = false;
	static bool running = false;
	static int reloadCount = 0;
	static void Loop()
	{
		do
		{
			if (!running)
			{
				Run();
				running = true;
				reloadCount++;
			}
			Thread.Sleep(1);
		}
		while (!errored);
	}

	static void Restart() => running = false;

	static void Restore(string? file = null)
	{
		string restoreFile;
		if (file != null)
			restoreFile = new FileInfo(file).FullName;
		else
			restoreFile = Paths.stateFile;
		if (!File.Exists(restoreFile))
		{
			Console.WriteLine($"State file: {restoreFile} not found!");
			return;
		}
		WindowManagerState state = WindowManagerState.FromJson(File.ReadAllText(restoreFile));
		Console.WriteLine($"Found {state.windows.Count} windows in {restoreFile}");
		state.windows.ForEach(wnd =>
		{
			Console.WriteLine($"Restoring {wnd.title}, hWnd: {wnd.hWnd}");
			wnd.Move(0, 0);
			wnd.Show();
		});
	}

	static void WithConsole(Action func)
	{
		Kernel32.AttachConsole(-1);
		Console.Clear();
		Console.Write("\n");
		func();
		Console.WriteLine("Press enter to return...");
		Kernel32.FreeConsole();
	}

	static void Main(string[] args)
	{
		switch (args.ToList().ElementAtOrDefault(0))
		{
			case null:
				Loop();
				break;
			case "--debug":
				WindowManager.DEBUG = true;
				WithConsole(() => Loop());
				break;
			case "--version":
				WithConsole(() =>
				{
					Console.WriteLine($"Aviyal version: {ver}");
				});
				break;
			case "--help":
				WithConsole(() =>
				{
					Console.WriteLine(
@"
,_______________________________,
|   Aviyal Window Manager |__|__|
|___ver_0.1.0-alpha_______|__|__|
|Author:  Ajaykrishnan.R  |\/ \/|
|/\/\/\/\/\/\/\/\/\/\/\/\/|/\_/\|
|________C# .NET 10_______|++++++
|////////////////////////////////
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$

Aviyal is a window manager that dynamically tiles your windows, organizes them inside workspaces, allows navigation through keybindings, and more :)

aviyal: https://github.com/TheAjaykrishnanR/aviyal
dflat: https://github.com/TheAjaykrishnanR/dflat

USAGE: aviyal <options> <arguments>

available options:

--help:     prints this help text
--debug:    flag for running the program in debug mode. Only special windows are tiled.
--version:  prints the version
--restore:  restores windows from a previous state. Useful when crashed and windows are hidden.
"
					);
				});
				break;
			case "--restore":
				WithConsole(() =>
				{
					Restore(args.ToList().ElementAtOrDefault(1));
				});
				break;
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

	FOCUS_WORKSPACE_1,
	FOCUS_WORKSPACE_2,
	FOCUS_WORKSPACE_3,
	FOCUS_WORKSPACE_4,
	FOCUS_WORKSPACE_5,
	FOCUS_WORKSPACE_6,
	FOCUS_WORKSPACE_7,
	FOCUS_WORKSPACE_8,
	FOCUS_WORKSPACE_9,

	RESTART,
	UPDATE,
}
