using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class Window : IWindow
{
	public nint hWnd { get; }
	public string title
	{
		get
		{
			return Utils.GetWindowTitleFromHWND(this.hWnd);
		}
	}

	public string className
	{
		get
		{
			return Utils.GetClassNameFromHWND(this.hWnd);
		}
	}
	public string exe
	{
		get
		{
			return Utils.GetExePathFromHWND(this.hWnd);
		}
	}

	public RECT rect // absolute position
	{
		get
		{
			User32.GetWindowRect(this.hWnd, out RECT _rect);
			return _rect;
		}
	}

	public RECT relRect { get; set; } // position of window relative to workspace (without margins)

	public SHOWWINDOW state
	{
		get
		{
			WINDOWPLACEMENT wndPlmnt = new();
			User32.GetWindowPlacement(this.hWnd, ref wndPlmnt);
			var state = (SHOWWINDOW)wndPlmnt.showCmd;
			Console.WriteLine($"state: {state}");
			return state;
		}
	}
	public bool floating { get; set; } = false;

	public bool elevated
	{
		get
		{
			string _exe = new FileInfo(exe).Name;
			Process _p = Process.GetProcessesByName(_exe).First();
			try
			{
				_ = _p.Handle;
				return false;
			}
			catch
			{
				return true;
			}
		}
	}

	public override bool Equals(object? obj)
	{
		//if (base.Equals(obj)) return true;
		if (obj is null) return false;
		if (((Window)obj).hWnd == this.hWnd) return true;
		return false;
	}

	public static bool operator ==(Window? left, Window? right)
	{
		if (left is null) return right is null;
		return left.Equals(right);
	}

	public static bool operator !=(Window? left, Window? right)
	{
		if (left is null) return right is not null;
		return !left.Equals(right);
	}

	public void ToggleAnimation(bool flag)
	{
		int attr = 0;
		if (!flag) attr = 1;
		int res = Dwmapi.DwmSetWindowAttribute(this.hWnd, DWMWINDOWATTRIBUTE.DWMWA_TRANSITIONS_FORCEDISABLED, ref attr, sizeof(int));
		//Console.WriteLine($"ToggleAnimation(): {res}");
	}

	public void Hide()
	{
		ToggleAnimation(false);
		User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_HIDE);
		ToggleAnimation(true);
	}
	public void Show()
	{
		ToggleAnimation(false);
		User32.ShowWindow(this.hWnd, SHOWWINDOW.SW_SHOWNA);
		ToggleAnimation(true);
	}

	public void Focus()
	{
		User32.keybd_event(0, 0, 0, Globals.FOREGROUND_FAKE_KEY);
		User32.SetForegroundWindow(this.hWnd);
	}

	public void Move(RECT pos)
	{
		// remove frame bounds
		RECT margin = GetFrameMargin();
		pos.Left -= margin.Left;
		pos.Top -= margin.Top;
		pos.Right -= margin.Right;
		pos.Bottom -= margin.Bottom;

		nint zorder = floating switch
		{
			true => (nint)SWPZORDER.HWND_TOP,
			false => (nint)SWPZORDER.HWND_BOTTOM
		};

		User32.SetWindowPos(this.hWnd, zorder, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, SETWINDOWPOS.SWP_NOACTIVATE);
	}

	bool RectEqual(RECT a, RECT b)
	{
		return a.Left == b.Left &&
			a.Top == b.Top &&
			a.Right == b.Right &&
			a.Bottom == b.Bottom;
	}

	public void Move(int? x, int? y)
	{
		User32.SetWindowPos(this.hWnd, 0, x ?? rect.Left, y ?? rect.Top, 0, 0, SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public void SetBottom()
	{
		User32.SetWindowPos(this.hWnd, (nint)SWPZORDER.HWND_BOTTOM, 0, 0, 0, 0, SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public RECT GetFrameMargin()
	{
		User32.GetWindowRect(this.hWnd, out RECT rect);
		Console.WriteLine($"GWR [L: {rect.Left} R: {rect.Right} T: {rect.Top} B:{rect.Bottom}]");
		int size = Marshal.SizeOf<RECT>();
		nint rectPtr = Marshal.AllocHGlobal(size);
		Dwmapi.DwmGetWindowAttribute(this.hWnd, (uint)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, rectPtr, (uint)size);
		RECT rect2 = Marshal.PtrToStructure<RECT>(rectPtr);
		Marshal.FreeHGlobal(rectPtr);
		Console.WriteLine($"DWM [L: {rect2.Left} R: {rect2.Right} T: {rect2.Top} B:{rect2.Bottom}]");
		// scale dwm rect2 to take into account display scaling
		double scale = Utils.GetDisplayScaling();

		return new RECT()
		{
			Left = rect2.Left - rect.Left,
			Top = rect2.Top - rect.Top,
			Right = rect2.Right - rect.Right,
			Bottom = rect2.Bottom - rect.Bottom,
		};
	}

	RECT ScaleRect(RECT rect, double scale)
	{
		rect.Left = (int)(rect.Left * scale);
		rect.Top = (int)(rect.Top * scale);
		rect.Right = (int)(rect.Right * scale);
		rect.Bottom = (int)(rect.Bottom * scale);
		return rect;
	}

	public void Close()
	{
		User32.SendMessage(this.hWnd, (uint)WINDOWMESSAGE.WM_CLOSE, 0, 0);
	}

	public Window(nint hWnd)
	{
		this.hWnd = hWnd;
	}
}

public class Workspace : IWorkspace
{
	public Guid id { get; } = Guid.NewGuid();
	public List<Window?> windows { get; private set; } = new();
	public Window? focusedWindow
	{
		get
		{
			return windows.FirstOrDefault(_wnd => _wnd == new Window(User32.GetForegroundWindow()));
		}
		private set;
	}
	public int? focusedWindowIndex
	{
		get
		{
			int? index = null;
			if (focusedWindow == null) return null;
			for (int i = 0; i < windows.Count; i++)
			{
				if (windows[i] == focusedWindow)
				{
					index = i;
					break;
				}
			}
			return index;
		}
	}
	public ILayout layout { get; set; }

	public override bool Equals(object? obj)
	{
		if (obj is null) return this is null;
		if (((Workspace)obj).id == this.id) return true;
		return false;
	}

	public static bool operator ==(Workspace left, Workspace right)
	{
		if (left is null) return right is null;
		return left.Equals(right);
	}

	public static bool operator !=(Workspace left, Workspace right)
	{
		if (left is null) return right is not null;
		return !left.Equals(right);
	}

	public void Add(Window wnd)
	{
		windows.Add(wnd);
		Update();
	}
	public void Remove(Window wnd)
	{
		windows.Remove(wnd);
		Update();
	}

	public void Update()
	{
		windows.ForEach(wnd => Console.WriteLine($"WND IS NULL: {wnd == null}"));

		List<Window?> nonFloating = windows
		.Where(wnd => wnd?.floating == false)
		.Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMAXIMIZED)
		.Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMINIMIZED)
		.ToList();

		if (nonFloating.Count == 0) return;

		RECT[] relRects = layout.GetRects(nonFloating.Count);
		RECT[] rects = layout.ApplyInner(layout.ApplyOuter(relRects.ToArray()));
		for (int i = 0; i < nonFloating.Count; i++)
		{
			nonFloating[i]?.Move(rects[i]);
			nonFloating[i].relRect = relRects[i];
		}
	}

	public void Focus()
	{
		Update();
		windows?.ForEach(wnd => wnd?.Show());
		windows?.FirstOrDefault()?.Focus();
		Console.WriteLine($"WORKSPACEFOCUSED, WINDOW: {focusedWindowIndex}");
	}

	public void Hide()
	{
		windows?.ForEach(wnd => wnd?.Hide());
	}

	public void CloseFocusedWindow()
	{
		int? index = focusedWindowIndex;
		if (index == null) return;
		index = index > 0 ? index - 1 : 0;
		focusedWindow?.Close();
		windows.ElementAtOrDefault((int)index)?.Focus();
	}

	public void FocusAdjacentWindow(EDGE direction)
	{
		Console.WriteLine($"focusing window on: {direction}, focusedWindowIndex: {focusedWindowIndex}");
		if (focusedWindowIndex == null) return;
		int? index = layout.GetAdjacent((int)focusedWindowIndex, direction);
		if (index != null) windows?[(int)index]?.Focus();
	}

	public void ShiftFocusedWindow(int shiftBy)
	{
		Window? _fwnd = focusedWindow;
		int? index = focusedWindowIndex;
		if (index == null) return;
		index += shiftBy;
		Console.WriteLine($"SHIFTING");
		if (index < 0 || index > windows.Count - 1) return;
		windows.Remove(_fwnd);
		windows.Insert((int)index, _fwnd);
		Focus();
	}

	public void ToggleFloating()
	{
		var wnd = focusedWindow;
		if (wnd == null) return;
		wnd.floating = !wnd.floating;
		Console.WriteLine($"[ TOGGLE FLOATING ] : {wnd.floating}, [ {config.floatingWindowSize} ]");
		wnd.Move(GetCenterRect(floatingWindowSize.Item1, floatingWindowSize.Item2));
		Update();
	}

	RECT GetCenterRect(int w, int h)
	{
		(int sw, int sh) = Utils.GetScreenSize();
		return new()
		{
			Left = (int)((sw - w) / 2),
			Right = (int)((sw + w) / 2),
			Top = (int)((sh - h) / 2),
			Bottom = (int)((sh + h) / 2),
		};
	}

	public void Move(int? x, int? y)
	{
		windows.ForEach(wnd =>
		{
			wnd?.Move(wnd.relRect.Left + x, wnd.relRect.Top + y);
		});
	}

	public void SwapWindows(Window wnd1, Window wnd2)
	{
		if (!windows.Contains(wnd1) || !windows.Contains(wnd2)) return;
		int wnd1_index = windows.Index().First(iwnd => iwnd.Item == wnd1).Index;
		int wnd2_index = windows.Index().First(iwnd => iwnd.Item == wnd2).Index;
		windows[wnd1_index] = wnd2;
		windows[wnd2_index] = wnd1;
		Update();
	}

	public Window? GetWindowFromPoint(POINT pt)
	{
		return windows.FirstOrDefault(wnd =>
		{
			return wnd?.relRect.Left < pt.X && pt.X < wnd?.relRect.Right &&
				   wnd?.relRect.Top < pt.Y && pt.Y < wnd?.relRect.Bottom;
		});
	}

	Config config;
	(int, int) floatingWindowSize;
	public Workspace(Config config)
	{
		this.config = config;
		var sizeStrs = config.floatingWindowSize.Split("x");
		floatingWindowSize.Item1 = Convert.ToInt32(sizeStrs[0]);
		floatingWindowSize.Item2 = Convert.ToInt32(sizeStrs[1]);
	}
}

public class WindowManager : IWindowManager
{
	public List<Window> initWindows { get; } = new();
	public List<Workspace> workspaces { get; } = new();
	public Workspace focusedWorkspace { get; private set; }

	public int focusedWorkspaceIndex
	{
		get
		{
			int index = 0;
			for (int i = 0; i < workspaces.Count; i++)
			{
				if (workspaces[i] == focusedWorkspace)
				{
					index = i;
					break;
				}
			}
			return index;
		}
	}
	public int WORKSPACES = 9;

	Server server = new();
	Config config;
	public static bool DEBUG = false;
	public WindowManager(Config config)
	{
		this.config = config;

		initWindows = GetVisibleWindows()!;

		// when running in debug mode, only window containing the title "windowgen" will 
		// be managed by the program. This is so that your ide or terminal is left free
		// while testing
		if (DEBUG)
		{
			initWindows = initWindows.Where(wnd => wnd.title.Contains("windowgen")).ToList();
		}
		initWindows.ForEach(wnd => Console.WriteLine($"Title: {wnd.title}, hWnd: {wnd.hWnd}"));

		for (int i = 0; i < WORKSPACES; i++)
		{
			Workspace wksp = new(config);
			wksp.layout = new Dwindle(config);
			workspaces.Add(wksp);
		}
		// add all windows to 1st workspace
		initWindows.ForEach(wnd => workspaces.First().windows.Add(wnd));
		FocusWorkspace(workspaces.First());

		server.REQUEST_RECEIVED += RequestReceived;
	}

	public List<Window?> GetVisibleWindows()
	{
		List<Window?> windows = new();
		List<nint>? hWnds = Utils.GetAllTaskbarWindows();
		hWnds?.ForEach(hWnd =>
		{
			windows.Add(new(hWnd));
		});
		return windows;
	}

	public void FocusWorkspace(Workspace wksp)
	{
		workspaces.ForEach(wksp => wksp.Hide());
		wksp.Focus();
		focusedWorkspace = wksp;
	}

	int GetX(int start, int end, int frames, int frame)
	{
		double progress = (double)frame / frames;
		progress = EaseOutQuint(progress);
		return start + (int)((end - start) * progress);
	}

	public double EaseOutQuint(double x)
	{
		return 1 - Math.Pow(1 - x, 3);
	}

	public async Task WorkspaceAnimate(Workspace wksp, int startX, int endX, int duration)
	{
		int fps = 60;
		int dt = (int)(1000 / fps); // milliseconds
		int frames = (int)(((float)duration / 1000) * fps);

		Stopwatch sw = new();
		sw.Start();
		for (int i = 0; i < frames; i++)
		{
			wksp.Move(GetX(startX, endX, frames, i), null);
			int wait = (int)(i * dt - sw.ElapsedMilliseconds);
			wait = wait < 0 ? 0 : wait;
			Console.WriteLine($"{i}. wait: {wait}");
			await Task.Delay(wait);
		}
		sw.Stop();
		Console.WriteLine($"total: {sw.ElapsedMilliseconds} ms");
	}

	public void FocusNextWorkspace()
	{
		SuppressEvents(() =>
		{
			int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
			int prev = focusedWorkspaceIndex > 0 ? focusedWorkspaceIndex - 1 : workspaces.Count - 1;

			if (config.workspaceAnimations)
			{
				// move left
				(int w, int h) = Utils.GetScreenSize();

				workspaces[next].Move(w, null);
				workspaces[next].Focus();

				int duration = 500;
				List<Task> _ts = new();
				_ts.Add(Task.Run(() => WorkspaceAnimate(focusedWorkspace, 0, -w, duration)));
				_ts.Add(Task.Run(() => WorkspaceAnimate(workspaces[next], w, 0, duration)));
				Task.WhenAll(_ts).Wait();
				focusedWorkspace.Hide();
				focusedWorkspace = workspaces[next];
				focusedWorkspace.Update(); // when animation finishes, margins dont match
			}

			else
			{
				FocusWorkspace(workspaces[next]);
				Console.WriteLine($"NEXT, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
			}
		});

		SaveState();
	}

	public void FocusPreviousWorkspace()
	{
		SuppressEvents(() =>
		{
			int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
			int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;

			if (config.workspaceAnimations)
			{
				// move right
				(int w, int h) = Utils.GetScreenSize();

				workspaces[prev].Move(-w, null);
				workspaces[prev].Focus();

				int duration = 500;
				List<Task> _ts = new();
				_ts.Add(Task.Run(() => WorkspaceAnimate(focusedWorkspace, 0, w, duration)));
				_ts.Add(Task.Run(() => WorkspaceAnimate(workspaces[prev], -w, 0, duration)));
				Task.WhenAll(_ts).Wait();
				focusedWorkspace.Hide();
				focusedWorkspace = workspaces[prev];
				focusedWorkspace.Update();
			}
			else
			{
				FocusWorkspace(workspaces[prev]);
				Console.WriteLine($"PREVIOUS, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
			}
		});

		SaveState();
	}

	public void ShiftFocusedWindowToWorkspace(int index)
	{
		if (index < 0 || index > workspaces.Count - 1) return;
		Window? wnd = focusedWorkspace.focusedWindow;
		if (wnd == null) return;
		focusedWorkspace.Remove(wnd);
		workspaces[index].Add(wnd);
		FocusWorkspace(workspaces[index]);
		focusedWorkspace = workspaces[index];
		wnd.Focus();
	}

	bool suppressEvents = false;
	void SuppressEvents(Action func)
	{
		Task.Run(async () =>
		{
			suppressEvents = true;
			func();
			await Task.Delay(50);
			suppressEvents = false;
		});
	}

	public void ShiftFocusedWindowToNextWorkspace()
	{
		int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
		SuppressEvents(() => ShiftFocusedWindowToWorkspace(next));

		SaveState();
	}

	public void ShiftFocusedWindowToPreviousWorkspace()
	{
		int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;
		SuppressEvents(() => ShiftFocusedWindowToWorkspace(prev));

		SaveState();
	}

	bool ShouldWindowBeIgnored(Window wnd)
	{
		if (wnd.className.Contains("#32770")) return true;
		List<string> styles = Utils.GetStylesFromHwnd(wnd.hWnd);
		if (styles.Contains("WS_POPUP") ||
			styles.Contains("WS_EX_TOOLWINDOW") ||
			styles.Contains("WS_DLGFRAME")
		) return true;
		if (!styles.Contains("WS_THICKFRAME")) return true;
		if (wnd.elevated) return true;
		return false;
	}

	public void WindowAdded(Window wnd)
	{
		if (suppressEvents) return;
		if (ShouldWindowBeIgnored(wnd)) return;

		Console.WriteLine($"WindowAdded, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		foreach (var wksp in workspaces)
			if (wksp.windows.Contains(wnd))
				return;

		focusedWorkspace.Add(wnd);
		focusedWorkspace.Update();

		CleanGhostWindows();
		SaveState();
	}

	public void WindowRemoved(Window wnd)
	{
		if (suppressEvents) return;

		Console.WriteLine($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		if (focusedWorkspace.windows.Contains(wnd))
		{
			focusedWorkspace.Remove(wnd);
			focusedWorkspace.Update();
		}

		CleanGhostWindows();
		SaveState();
	}

	public void CleanGhostWindows()
	{
		var visibleWindows = GetVisibleWindows();
		// visible windows will give all alt-tab programs, even tool windows
		// which we dont need and for whom winevents would typically not fire.
		// That is why whe have an '>' instead of an '!='
		// The reason we are doing all this is that for some windows such as
		// the file explorer, win events wont fire an OBJECT_SHOW when closing
		if (focusedWorkspace.windows.Count > visibleWindows.Count)
		{
			var ghostWindows = focusedWorkspace.windows.Where(wnd => !visibleWindows.Contains(wnd)).ToList();
			ghostWindows.ForEach(wnd =>
			{
				Console.WriteLine($"REMOVING GHOST: {wnd.title}, {wnd.hWnd}, {wnd.className}");
				focusedWorkspace.Remove(wnd);
			});
			focusedWorkspace.Update();
		}
	}

	public void WindowMoved(Window wnd)
	{
		if (suppressEvents) return;

		Console.WriteLine($"WindowMoved, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		var _wnd = focusedWorkspace.windows.FirstOrDefault(_wnd => _wnd == wnd);
		if (_wnd == null) return;
		// wnd -> window being moved
		// cursorPos
		// wndEnclosingCursor -> window enclosing cursor
		if (!_wnd.floating)
		{
			User32.GetCursorPos(out POINT pt);
			Window? wndUnderCursor = focusedWorkspace.GetWindowFromPoint(pt);
			if (wndUnderCursor == null) return;
			focusedWorkspace.SwapWindows(wnd, wndUnderCursor);
		}

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState();
	}

	public void WindowMaximized(Window wnd)
	{
		if (suppressEvents) return;

		Console.WriteLine($"WindowMazimized, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState();
	}

	public void WindowMinimized(Window wnd)
	{
		if (suppressEvents) return;

		Console.WriteLine($"WindowMinimized, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");
		// render only after state has updated (winevent and GetWindowPlacement() is not synchronous)
		TaskEx.WaitUntil(() => wnd.state == SHOWWINDOW.SW_SHOWMINIMIZED).Wait();

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState();
	}
	public void WindowRestored(Window wnd)
	{
		if (suppressEvents) return;

		Console.WriteLine($"WindowRestored, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState();
	}

	public void WindowFocused(Window wnd)
	{
		if (suppressEvents) return;

		Console.WriteLine($"WindowFocused, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		CleanGhostWindows();
		SaveState();
	}

	public WindowManagerState GetState()
	{
		WindowManagerState state = new();
		workspaces.ForEach(wksp => wksp.windows.ForEach(wnd => state.windows.Add(wnd!)));
		state.focusedWorkspaceIndex = focusedWorkspaceIndex;
		state.workspaceCount = workspaces.Count;
		return state;
	}

	readonly Lock @lock = new();
	public void SaveState()
	{
		lock (@lock)
		{
			var state = GetState();
			Console.WriteLine("WRITING STATE");
			server.Broadcast(state.ToJson());
			File.WriteAllText(Paths.stateFile, state.ToJson());
			Console.WriteLine(state.ToJson());
		}
	}

	public string RequestReceived(string request)
	{
		string[] args = request.Split(" ");
		args[args.Length - 1] = args.Last().Replace("\n", "");
		Console.WriteLine($"arg0: {args.FirstOrDefault()}, arg1: {args.ElementAtOrDefault(1)}");
		string? verb = args.FirstOrDefault();
		Console.WriteLine($"verb: {verb}");
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
						if (index >= 0 && index <= workspaces.Count - 1) FocusWorkspace(workspaces[index]);
						break;
				}
				break;
			default:
				break;
		}
		return response;
	}
}

enum FillDirection
{
	HORIZONTAL,
	VERTICAL
}
