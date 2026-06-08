using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Aviyal : IDisposable
{
    static string version = "0.2.5";
    static string changelog =
        @"
- hotkeys improvement: accidental keypresses still manage to trigger hotkeys
- logging made async
- version bump
";
    static Aviyal? aviyal;

    public static bool DEBUG = false;

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

        kbdListener = new(config);

        actions = new()
        {
            { COMMAND.FOCUS_NEXT_WORKSPACE, () => wm.FocusNextWorkspace() },
            { COMMAND.FOCUS_PREVIOUS_WORKSPACE, () => wm.FocusPreviousWorkspace() },
            { COMMAND.CLOSE_FOCUSED_WINDOW, () => wm.CloseFocusedWindow() },
            { COMMAND.FOCUS_LEFT_WINDOW, () => wm.FocusAdjacentWindow(EDGE.LEFT) },
            { COMMAND.FOCUS_TOP_WINDOW, () => wm.FocusAdjacentWindow(EDGE.TOP) },
            { COMMAND.FOCUS_RIGHT_WINDOW, () => wm.FocusAdjacentWindow(EDGE.RIGHT) },
            { COMMAND.FOCUS_BOTTOM_WINDOW, () => wm.FocusAdjacentWindow(EDGE.BOTTOM) },
            { COMMAND.SHIFT_FOCUSED_WINDOW_RIGHT, () => wm.ShiftFocusedWindowBy(+1) },
            { COMMAND.SHIFT_FOCUSED_WINDOW_LEFT, () => wm.ShiftFocusedWindowBy(-1) },
            { COMMAND.SHIFT_WINDOW_NEXT_WORKSPACE, () => wm.ShiftFocusedWindowToNextWorkspace() },
            {
                COMMAND.SHIFT_WINDOW_PREVIOUS_WORKSPACE,
                () => wm.ShiftFocusedWindowToPreviousWorkspace()
            },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_1, () => wm.ShiftFocusedWindowToNumWorkspace(0) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_2, () => wm.ShiftFocusedWindowToNumWorkspace(1) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_3, () => wm.ShiftFocusedWindowToNumWorkspace(2) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_4, () => wm.ShiftFocusedWindowToNumWorkspace(3) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_5, () => wm.ShiftFocusedWindowToNumWorkspace(4) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_6, () => wm.ShiftFocusedWindowToNumWorkspace(5) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_7, () => wm.ShiftFocusedWindowToNumWorkspace(6) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_8, () => wm.ShiftFocusedWindowToNumWorkspace(7) },
            { COMMAND.SHIFT_WINDOW_WORKSPACE_9, () => wm.ShiftFocusedWindowToNumWorkspace(8) },
            { COMMAND.TOGGLE_FLOATING_WINDOW, () => wm.ToggleFloating() },
            { COMMAND.TOGGLE_STACKED_WINDOW, () => wm.ToggleStacked() },
            {
                COMMAND.TOGGLE_FOCUSED_WINDOW_MAXIMIZATION,
                () => wm.ToggleFocusedWindowMaximization()
            },
            { COMMAND.MINIMIZE_FOCUSED_WINDOW, () => wm.MinimizeFocusedWindow() },
            { COMMAND.FOCUS_WORKSPACE_1, () => wm.FocusWorkspace(0) },
            { COMMAND.FOCUS_WORKSPACE_2, () => wm.FocusWorkspace(1) },
            { COMMAND.FOCUS_WORKSPACE_3, () => wm.FocusWorkspace(2) },
            { COMMAND.FOCUS_WORKSPACE_4, () => wm.FocusWorkspace(3) },
            { COMMAND.FOCUS_WORKSPACE_5, () => wm.FocusWorkspace(4) },
            { COMMAND.FOCUS_WORKSPACE_6, () => wm.FocusWorkspace(5) },
            { COMMAND.FOCUS_WORKSPACE_7, () => wm.FocusWorkspace(6) },
            { COMMAND.FOCUS_WORKSPACE_8, () => wm.FocusWorkspace(7) },
            { COMMAND.FOCUS_WORKSPACE_9, () => wm.FocusWorkspace(8) },
            { COMMAND.UPDATE, () => wm.Update() },
            { COMMAND.RESTART, () => Restart() },
        };

        // just make all windows reappear if crashes
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.UnhandledException += (s, e) =>
        {
            int i = 0;
            wm.workspaces.ForEach(wksp =>
                wksp?.windows.ForEach(wnd =>
                {
                    wnd?.Show();
                    i++;
                })
            );
            Logger.Log($"Restored {i} windows...", LogType.INFO);

            Exception ex = (Exception)e.ExceptionObject;
            Logger.Log("AppDomain: Unhandled exception", ex: ex, logType: LogType.ERROR);
            errored = true;
        };
    }

    public void AttachEventHandlers()
    {
        wm.WM_EVENT += WmEventHandler;
        server.REQUEST_RECEIVED += RequestReceived;

        wndListener.WINDOW_SHOWN += wm.WindowShown;
        wndListener.WINDOW_HIDDEN += wm.WindowHidden;
        wndListener.WINDOW_DESTROYED += wm.WindowDestroyed;
        wndListener.WINDOW_MOVED += wm.WindowMoved;
        wndListener.WINDOW_MAXIMIZED += wm.WindowMaximized;
        wndListener.WINDOW_MINIMIZED += wm.WindowMinimized;
        wndListener.WINDOW_RESTORED += wm.WindowRestored;
        wndListener.WINDOW_FOCUSED += wm.WindowFocused;

        kbdListener.HOTKEY_PRESSED += HotkeyPressed;

        mouseListener.MOUSE_DOWN += MouseDown;
        mouseListener.MOUSE_UP += MouseUp;
    }

    public void Dispose()
    {
        // instances wont be disposed if event handlers are still attached
        // found out the hard way when couldnt figure out why previous instance
        // configuration persisted onto the next. Turns out it was one of these
        // old event handlers still setting window attributes
        wm.WM_EVENT -= WmEventHandler;
        server.REQUEST_RECEIVED -= RequestReceived;
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

    public void WmEventHandler(string message) => SaveState(message);

    public void HotkeyPressed(Keymap keymap)
    {
        if (DEBUG)
            Logger.Log(
                $"Hotekey Pressed: {keymap.command}, time: {DateTimeOffset.Now.ToUnixTimeMilliseconds()}",
                logType: LogType.EVENT
            );
        if (keymap.command == COMMAND.EXEC)
            Exec(keymap.arguments);
        else
            actions[keymap.command]?.Invoke();
    }

    public void MouseDown() => wm.mouseDown = true;

    public void MouseUp() => wm.mouseDown = false;

    // server request received
    public string RequestReceived(string request)
    {
        string[] args = request.Split(" ");
        args[args.Length - 1] = args.Last().Replace("\n", "");
        string? verb = args.FirstOrDefault();
        string response = "";
        switch (verb)
        {
            case null or "":
                break;
            case "get":
                switch (args.ElementAtOrDefault(1))
                {
                    case null or "":
                        break;
                    case "state":
                        response = GetState().ToJson();
                        break;
                }
                break;
            case "set":
                switch (args.ElementAtOrDefault(1))
                {
                    case null or "":
                        break;
                    case "focusedWorkspaceIndex":
                        int index = Convert.ToInt32(args.ElementAtOrDefault(2));
                        wm.FocusWorkspace(index);
                        break;
                }
                break;
            default:
                break;
        }
        return response;
    }

    public ProgramState GetState()
    {
        ProgramState state = new();
        wm.windows.ForEach(wnd => state.windows.Add(wnd!));
        state.focusedWorkspaceIndex = wm.focusedWorkspaceIndex;
        state.workspaceCount = wm.workspaces.Count;
        state.keysHookThreadState = kbdListener.thread.ThreadState.ToString();
        state.mouseHookThreadState = mouseListener.thread.ThreadState.ToString();
        state.wndHookThreadState = wndListener.thread.ThreadState.ToString();
        return state;
    }

    public void SaveState(string? lastAction = null)
    {
        var state = GetState();
        server.Broadcast(state.ToJson());
        try
        {
            File.WriteAllText(Paths.stateFile, state.ToJson());
        }
        catch (Exception ex)
        {
            Logger.Log("Can't writing to state file", ex: ex, logType: LogType.ERROR);
        }
        Logger.Log(
            $"lastAction: {lastAction}, time: {DateTimeOffset.Now.ToUnixTimeMilliseconds()}, focusedWorkspace: {state.focusedWorkspaceIndex}",
            file: false,
            logType: LogType.EVENT
        );
        if (DEBUG)
            Logger.Log(state.ToJson());
    }

    public void Exec(List<string> args, bool elevated = false)
    {
        if (args.Count == 0)
            return;

        ProcessStartInfo psi = new();
        psi.FileName = args[0];
        //if (args.Count > 0) psi.Arguments = args[1];
        Process process = new();
        process.StartInfo = psi;

        if (Environment.IsPrivilegedProcess == elevated)
        {
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to execute command", ex: ex, logType: LogType.ERROR);
            }
        }
        else if (elevated)
        {
            psi.Verb = "runas";
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to execute command", ex: ex, logType: LogType.ERROR);
            }
        }
        else
        {
            string cmdLine = string.Join(" ", args);
            Utils.ExecuteUnelevated(cmdLine);
        }
    }

    /*
     * Creates an instance of the program
     * */

    static void Run()
    {
        Logger.Init();

        if (reloadCount == 0)
        {
            File.Delete(Paths.logFile);
            Logger.Log($"Starting aviyal, time: {DateTimeOffset.Now.ToUnixTimeSeconds()}");
        }

        var psWithSameName = Process
            .GetProcessesByName(Process.GetCurrentProcess().ProcessName)
            .ToList();
        if (psWithSameName.Count > 1)
        {
            Logger.Log("an instance is already running, exiting...");
            var opsWithSameName = psWithSameName
                .Where(p => p.Id != Process.GetCurrentProcess().Id)
                .ToList();
            string currentUser = Utils.GetProcessUserName((uint)Process.GetCurrentProcess().Id);
            // only exit if another process is running on the same user
            foreach (var p in opsWithSameName)
            {
                if (Utils.GetProcessUserName((uint)p.Id) == currentUser)
                    return;
            }
        }

        Logger.Log($"Running aviyal instance, reload count: {reloadCount}");

        Paths.CreateIfAbsent();

        Config? config = null;
        if (File.Exists(Paths.configFile))
        {
            string jsonString = File.ReadAllText(Paths.configFile);
            Logger.Log(jsonString, file: false);
            try
            {
                config = Config.FromJson(jsonString);
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to parse json config file", ex: ex);
                config = new();
            }
        }
        else
        {
            config = new();
            Logger.Log("Default config: ", file: false);
            File.AppendAllText(Paths.configFile, config.ToJson());
        }

        Shcore.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);

        // collect windows to restore when reloaded (when reloaded all windows will be put to workspace 0)
        var windows = aviyal?.wm.windows;
        aviyal?.Dispose();
        aviyal = new(config);
        aviyal.wm.initWindows = windows!;
        aviyal.wm.Start();
        // do NOT attach the event handlers before wm has started. Window events before initialization
        // can case race conditions and collection modifications in wm.Start()
        aviyal.AttachEventHandlers();
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
        } while (!errored);
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
            Logger.Log($"State file: {restoreFile} not found!", logType: LogType.ERROR);
            return;
        }
        ProgramState state = ProgramState.FromJson(File.ReadAllText(restoreFile));
        Logger.Log($"Found {state.windows.Count} windows in {restoreFile}");
        state.windows.ForEach(wnd =>
        {
            Logger.Log($"Restoring {wnd.title}, hWnd: {wnd.hWnd}");
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
                string message =
                    @"
Running as a non elevated process. Elevated windows will be 
unmanaged. Focused elevated windows will steal input. For 
managing all windows including elevated ones run the process 
as an administrator or from an elevated prompt.
";
                if (!Environment.IsPrivilegedProcess)
                    User32.MessageBox(0, message, "Message", 0);
                Loop();
                break;
            case "--debug":
                DEBUG = true;
                WindowManager.DEBUG_WND_NAME = args.ToList().ElementAtOrDefault(1);
                WithConsole(Loop);
                break;
            case "--version":
                WithConsole(() => Console.WriteLine($"Aviyal version: {version}"));
                break;
            case "--changelog":
                WithConsole(() => Console.WriteLine($"CHANGELOG [{version}]:\n {changelog}"));
                break;
            case "--help":
                WithConsole(() =>
                {
                    Console.WriteLine(
                        @$"
,_______________________________,
|  Aviyal Dynamic Tiling  |__|__|
|______Window Manager_____|__|__|
|Author:  Ajaykrishnan.R  |\/ \/|
|/\/\/\/\/\/\/\/\/\/\/\/\/|/\_/\|
|________C# .NET 10_______|++++++
|////////////////////////////////
$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$

Aviyal is a window manager that dynamically tiles your windows, organizes them inside workspaces, allows navigation through keybindings, and more :)

ver: {version}

aviyal: https://github.com/TheAjaykrishnanR/aviyal
dflat: https://github.com/TheAjaykrishnanR/dflat

USAGE: aviyal <options> <arguments>

available options:

--help:     	prints this help text.
--debug:    	flag for running the program in debug mode. Only special windows are tiled.
--version:  	prints the version.
--changelog:	prints the changes in the current version.
--restore:  	restores windows from a previous state. Useful when crashed and windows are hidden.
"
                    );
                });
                break;
            case "--restore":
                WithConsole(() => Restore(args.ToList().ElementAtOrDefault(1)));
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

    SHIFT_FOCUSED_WINDOW_RIGHT, // same workspace
    SHIFT_FOCUSED_WINDOW_LEFT,

    SHIFT_WINDOW_NEXT_WORKSPACE,
    SHIFT_WINDOW_PREVIOUS_WORKSPACE,
    SHIFT_WINDOW_WORKSPACE_1,
    SHIFT_WINDOW_WORKSPACE_2,
    SHIFT_WINDOW_WORKSPACE_3,
    SHIFT_WINDOW_WORKSPACE_4,
    SHIFT_WINDOW_WORKSPACE_5,
    SHIFT_WINDOW_WORKSPACE_6,
    SHIFT_WINDOW_WORKSPACE_7,
    SHIFT_WINDOW_WORKSPACE_8,
    SHIFT_WINDOW_WORKSPACE_9,

    TOGGLE_FLOATING_WINDOW,
    TOGGLE_STACKED_WINDOW,

    TOGGLE_FOCUSED_WINDOW_MAXIMIZATION,
    MINIMIZE_FOCUSED_WINDOW,

    FOCUS_WORKSPACE_1,
    FOCUS_WORKSPACE_2,
    FOCUS_WORKSPACE_3,
    FOCUS_WORKSPACE_4,
    FOCUS_WORKSPACE_5,
    FOCUS_WORKSPACE_6,
    FOCUS_WORKSPACE_7,
    FOCUS_WORKSPACE_8,
    FOCUS_WORKSPACE_9,

    EXEC,
    RESTART,
    UPDATE,
    DEBUG_CONSOLE_CLEAR,
}
