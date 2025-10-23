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
	public int workspace;
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
	public string? exe
	{
		get
		{
			// EnumWindowProcess() is hella expensive, and anyway exe wont change
			// for a process once its found
			if (field == null)
			{
				field = Utils.GetExePathFromHWND(this.hWnd) ??
						Utils.EnumWindowProcesses()
						.FirstOrDefault(wndProcess => wndProcess
										.windows.Select(wndp => wndp.hWnd)
						.Contains(this.hWnd))?
						.process.MainModule?.FileName;
			}
			return field;
		}
	}
	public string exeName
	{
		get
		{
			return @$"{exe}"?.Split(@"\").Last().Replace(".exe", "")!;
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
			//Console.WriteLine($"state: {state}");
			return state;
		}
	}
	public bool floating { get; set; } = false;
	public bool tileable { get; set; } = true;

	public int pid
	{
		get
		{
			Process? _p = Process.GetProcessesByName(exeName).FirstOrDefault();
			return _p == null ? 0 : _p.Id;
		}
	}

	public bool elevated
	{
		get
		{
			//Console.WriteLine($"checking elevation of {title}: {Utils.IsProcessElevated(pid)}");
			return Utils.IsProcessElevated(pid);
		}
	}

	public WINDOWSTYLE styles
	{
		get
		{
			return (WINDOWSTYLE)User32.GetWindowLong(this.hWnd, GETWINDOWLONG.GWL_STYLE);
		}
	}

	public WINDOWSTYLEEX exStyles
	{
		get
		{
			return (WINDOWSTYLEEX)User32.GetWindowLong(this.hWnd, GETWINDOWLONG.GWL_EXSTYLE);
		}
	}

	public int borderThickness
	{
		get
		{
			User32.GetWindowInfo(this.hWnd, out WINDOWINFO info);
			return info.cxWindowBorders;
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
		////Console.WriteLine($"ToggleAnimation(): {res}");
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

	const SETWINDOWPOS defaultMoveFlags =
		SETWINDOWPOS.SWP_NOSENDCHANGING |
		SETWINDOWPOS.SWP_NOCOPYBITS |
		SETWINDOWPOS.SWP_ASYNCWINDOWPOS |
		SETWINDOWPOS.SWP_NOACTIVATE |
		SETWINDOWPOS.SWP_NOZORDER;

	public void Move(RECT pos, bool redraw = true)
	{
		// remove frame bounds
		RECT margin = GetFrameMargin();
		pos.Left -= margin.Left;
		pos.Top -= margin.Top;
		pos.Right -= margin.Right;
		pos.Bottom -= margin.Bottom;

		SETWINDOWPOS moveFlags = redraw switch
		{
			true => defaultMoveFlags,
			false => defaultMoveFlags | SETWINDOWPOS.SWP_NOREDRAW
		};

		User32.SetWindowPos(this.hWnd, 0, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, moveFlags);
	}

	const SETWINDOWPOS slideFlag = defaultMoveFlags | SETWINDOWPOS.SWP_NOSIZE;
	public void Move(int? x, int? y, bool redraw = true)
	{
		SETWINDOWPOS moveFlag = redraw switch
		{
			true => slideFlag,
			false => slideFlag | SETWINDOWPOS.SWP_NOREDRAW
		};
		User32.SetWindowPos(this.hWnd, 0, x ?? rect.Left, y ?? rect.Top, 0, 0, moveFlag);
	}

	public void SetBottom()
	{
		User32.SetWindowPos(this.hWnd, (nint)SWPZORDER.HWND_BOTTOM, 0, 0, 0, 0, SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public void SetFront()
	{
		User32.SetWindowPos(this.hWnd, (nint)SWPZORDER.HWND_TOP, 0, 0, 0, 0, SETWINDOWPOS.SWP_NOMOVE | SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public RECT GetFrameMargin()
	{
		User32.GetWindowRect(this.hWnd, out RECT rect);
		//Console.WriteLine($"GWR [L: {rect.Left} R: {rect.Right} T: {rect.Top} B:{rect.Bottom}]");
		int size = Marshal.SizeOf<RECT>();
		nint rectPtr = Marshal.AllocHGlobal(size);
		Dwmapi.DwmGetWindowAttribute(this.hWnd, (uint)DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, rectPtr, (uint)size);
		RECT rect2 = Marshal.PtrToStructure<RECT>(rectPtr);
		Marshal.FreeHGlobal(rectPtr);
		//Console.WriteLine($"DWM [L: {rect2.Left} R: {rect2.Right} T: {rect2.Top} B:{rect2.Bottom}]");
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

	public void Close()
	{
		User32.SendMessage(this.hWnd, (uint)WINDOWMESSAGE.WM_CLOSE, 0, 0);
	}

	// force the window to redraw itself
	public void Redraw()
	{
		User32.RedrawWindow(this.hWnd, 0, 0,
			REDRAWWINDOW.INVALIDATE |
			REDRAWWINDOW.ALLCHILDREN |
			REDRAWWINDOW.UPDATENOW
		);
	}

	RECT ScaleRect(RECT rect, double scale)
	{
		rect.Left = (int)(rect.Left * scale);
		rect.Top = (int)(rect.Top * scale);
		rect.Right = (int)(rect.Right * scale);
		rect.Bottom = (int)(rect.Bottom * scale);
		return rect;
	}

	bool RectEqual(RECT a, RECT b)
	{
		return a.Left == b.Left &&
			a.Top == b.Top &&
			a.Right == b.Right &&
			a.Bottom == b.Bottom;
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

	// applies updated relRects (provided by the layout) to the windows in the workspace
	public void Update()
	{
		//windows.ForEach(wnd => //Console.WriteLine($"WND IS NULL: {wnd == null}"));

		List<Window?> nonFloating = windows
		.Where(wnd => wnd?.floating == false)
		.Where(wnd => wnd?.tileable == true)
		.Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMAXIMIZED)
		.Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMINIMIZED)
		.ToList();

		// non floating
		RECT[] relRects = layout.GetRects(nonFloating.Count);
		RECT[] rects = layout.ApplyInner(layout.ApplyOuter(relRects.ToArray()));
		for (int i = 0; i < nonFloating.Count; i++)
		{
			nonFloating[i]?.Move(rects[i]);
			nonFloating[i].relRect = relRects[i];
		}

		// floating
		List<Window?> floating = windows
		.Where(wnd => wnd?.floating == true)
		.Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMAXIMIZED)
		.Where(wnd => wnd?.state != SHOWWINDOW.SW_SHOWMINIMIZED)
		.ToList();

		for (int i = 0; i < floating.Count; i++)
		{
			floating[i].relRect = floating[i].rect;
		}
	}

	public void Show()
	{
		windows?.ForEach(wnd => wnd?.Show());
	}

	Window? lastFocusedWindow = null;
	public void SetFocusedWindow()
	{
		if (lastFocusedWindow == null)
		{
			var wnd = windows?.FirstOrDefault();
			lastFocusedWindow = wnd;
			wnd?.Focus();
		}
		else lastFocusedWindow.Focus();
	}

	public void Focus()
	{
		Update();
		Show();
		SetFocusedWindow();
	}

	public void Redraw()
	{
		windows?.ForEach(wnd => wnd?.Redraw());
	}

	public void Hide()
	{
		windows?.ForEach(wnd =>
		{
			//var (sx, sy) = Utils.GetScreenSize();
			//wnd?.Move(sx, sy);
			wnd?.Hide();
		});
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
		//Console.WriteLine($"focusing window on: {direction}, focusedWindowIndex: {focusedWindowIndex}");
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
		//Console.WriteLine($"SHIFTING");
		if (index < 0 || index > windows.Count - 1) return;
		windows.Remove(_fwnd);
		windows.Insert((int)index, _fwnd);
		Focus();
	}

	public void ApplyFloatingSize(Window wnd)
	{
		wnd.Move(GetCenterRect(floatingWindowSize.Item1, floatingWindowSize.Item2));
	}

	public void ToggleFloating(Window? wnd = null)
	{
		if (wnd == null) wnd = focusedWindow;
		if (wnd == null) return;
		wnd.floating = !wnd.floating;
		//Console.WriteLine($"[ TOGGLE FLOATING ] : {wnd.floating}, [ {config.floatingWindowSize} ]");
		if (wnd.floating) ApplyFloatingSize(wnd);
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

	public void Move(int? x, int? y, bool redraw = true)
	{
		windows.ForEach(wnd =>
		{
			wnd?.Move(wnd.relRect.Left + x, wnd.relRect.Top + y, redraw);
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
	public List<Window>? initWindows { get; set; } // initWindows := initial set of windows to start the WM with
	public List<Workspace?> workspaces { get; } = new();
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

	//Server server = new();
	Config config;
	public static bool DEBUG = false;
	public WindowManager(Config config)
	{
		this.config = config;
	}

	public void Start()
	{
		if (initWindows == null)
		{
			this.initWindows = GetVisibleWindows()!;
			this.initWindows = this.initWindows
						  .Where(wnd => !ShouldWindowBeIgnored(wnd))
						  .ToList();
			this.initWindows.ForEach(wnd => ApplyConfigsToWindow(wnd));
		}

		// when running in debug mode, only window containing the title "windowgen" will 
		// be managed by the program. This is so that your ide or terminal is left free
		// while testing
		if (DEBUG)
		{
			this.initWindows = this.initWindows.Where(wnd => wnd.title.Contains("windowgen")).ToList();
		}
		//initWindows.ForEach(wnd => //Console.WriteLine($"Title: {wnd.title}, hWnd: {wnd.hWnd}"));

		for (int i = 0; i < WORKSPACES; i++)
		{
			Workspace wksp = new(config);
			wksp.layout = new Dwindle(config);
			workspaces.Add(wksp);
		}
		// add all windows to 1st workspace
		this.initWindows.ForEach(wnd =>
		{
			wnd.workspace = 0;
			workspaces.FirstOrDefault()?.windows.Add(wnd);
		});
		FocusWorkspace(workspaces?.FirstOrDefault()!);
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
			wksp.Move(GetX(startX, endX, frames, i), null, redraw: false); // not drawn, so must be manually redrawn once finished
			int wait = (int)(i * dt - sw.ElapsedMilliseconds);
			wait = wait < 0 ? 0 : wait;
			await Task.Delay(wait);
		}
		sw.Stop();
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

				// we call Show() here instead of Focus() because Focus() has a call to Update()
				// if we Update() our Workspace then all the windows will be set to their
				// appropriate relRect effectively reversing Move(w, null). Hence as a result
				// you will see a flash of the next/prev workspace before it appears sliding.
				// So whats exactly going on ? Move(w, null) moves your workspace out of screen,
				// Focus() brings it back using Update() and Shows it until WorkspaceAnimate()
				// takes it out of screen as part of the animation start position which is also
				// beyond the screen.
				//Thread.Sleep(100);
				workspaces[next].Show();

				int duration = 500;
				List<Task> _ts = new();
				_ts.Add(Task.Run(() => WorkspaceAnimate(focusedWorkspace, 0, -w, duration)));
				_ts.Add(Task.Run(() => WorkspaceAnimate(workspaces[next], w, 0, duration)));
				Task.WhenAll(_ts).Wait();
				focusedWorkspace.Hide();
				focusedWorkspace = workspaces[next];
				focusedWorkspace.Update(); // when animation finishes, margins dont match
				focusedWorkspace.Redraw(); // manually redraw
				focusedWorkspace.SetFocusedWindow();
			}

			else
			{
				FocusWorkspace(workspaces[next]);
				//Console.WriteLine($"FOCUSING NEXT WORKSPACE, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
			}
		});

		SaveState("FocusNextWorkspace");
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
				workspaces[prev].Show();

				int duration = 500;
				List<Task> _ts = new();
				_ts.Add(Task.Run(() => WorkspaceAnimate(focusedWorkspace, 0, w, duration)));
				_ts.Add(Task.Run(() => WorkspaceAnimate(workspaces[prev], -w, 0, duration)));
				Task.WhenAll(_ts).Wait();
				focusedWorkspace.Hide();
				focusedWorkspace = workspaces[prev];
				focusedWorkspace.Update();
				focusedWorkspace.Redraw();
				focusedWorkspace.SetFocusedWindow();
			}
			else
			{
				FocusWorkspace(workspaces[prev]);
				//Console.WriteLine($"FOCUSING PREVIOUS WORKSPACE, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
			}
		});

		SaveState("FocusPreviousWorkspace");
	}

	public void ShiftFocusedWindowToWorkspace(int index)
	{
		if (index < 0 || index > workspaces.Count - 1) return;
		Window? wnd = focusedWorkspace.focusedWindow;
		if (wnd == null) return;
		focusedWorkspace.Remove(wnd);
		wnd.workspace = index;
		workspaces[index].Add(wnd);
		FocusWorkspace(workspaces[index]);
		focusedWorkspace = workspaces[index];
		wnd.Focus();
	}

	bool suppressEvents = false;
	void SuppressEvents(Action func)
	{
		lock (@addLock)
		{
			suppressEvents = true;
			func();
			Thread.Sleep(100);
			suppressEvents = false;
		}
	}

	public void ShiftFocusedWindowToNextWorkspace()
	{
		int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
		SuppressEvents(() => ShiftFocusedWindowToWorkspace(next));

		SaveState("ShiftWindowToNextWorkspace");
	}

	public void ShiftFocusedWindowToPreviousWorkspace()
	{
		int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;
		SuppressEvents(() => ShiftFocusedWindowToWorkspace(prev));

		SaveState("ShiftWindowToPreviousWorkspace");
	}

	bool IsWindowInConfigRules(Window wnd, string ruleType)
	{
		var rules = config.rules.Where(rule => rule.type == ruleType).ToList();

		foreach (var rule in rules)
		{
			Func<string, string, bool> condition = rule.method switch
			{
				"equals" => (wndAttribute, identifier) => wndAttribute == identifier,
				"contains" => (wndAttribute, identifier) => wndAttribute.Contains(identifier),
				_ => (x, y) => false
			};

			string wndAttribute = rule.identifierType switch
			{
				"windowProcess" => wnd.exeName,
				"windowTitle" => wnd.title,
				"windowClass" => wnd.className,
				_ => ""
			};
			if (condition(wndAttribute, rule.identifier)) return true;
		}
		return false;
	}

	// filter out windows that should never be interacted with
	bool ShouldWindowBeIgnored(Window wnd)
	{
		// not required actually because WINDOW_ADDED only fires on OBJECT_SHOW
		// however adding for completeness
		if (!wnd.styles.HasFlag(WINDOWSTYLE.WS_VISIBLE)) return true;
		if (wnd.styles.HasFlag(WINDOWSTYLE.WS_CHILD)) return true;

		// all normal top level windows must have either "WS_OVERLAPPED" - OR - "WS_POPUP"
		// so kick out windows that dont have neither
		// WS_OVERLAPPED is the default style with which you get a normal window
		// since WS_OVERLAPPED = 0x00000000L it must be checked by the absence of both
		// WS_POPUP and WS_CHILD
		bool isOverlapped = ((uint)wnd.styles & ((uint)WINDOWSTYLE.WS_POPUP | (uint)WINDOWSTYLE.WS_CHILD)) == 0;
		if (!isOverlapped &&
		   !wnd.styles.HasFlag(WINDOWSTYLE.WS_POPUP)
		) return true;

		if (wnd.exStyles.HasFlag(WINDOWSTYLEEX.WS_EX_TOOLWINDOW)) return true;
		if (wnd.exStyles.HasFlag(WINDOWSTYLEEX.WS_EX_TOPMOST)) return true;

		if (wnd.className == null || wnd.className == "") return true;

		if (wnd.className.Contains("#32770") &&
			!wnd.styles.HasFlag(WINDOWSTYLE.WS_SYSMENU) &&
			(wnd.rect.Bottom - wnd.rect.Top < 50 ||
			 wnd.rect.Right - wnd.rect.Left < 50)
			) return true; // dialogs

		// tooltips
		// https://learn.microsoft.com/en-us/windows/win32/controls/common-control-window-classes
		if (wnd.className.Contains("MicrosoftWindowsTooltip") ||
			wnd.className.Contains("tooltips_class32")
			) return true;

		// menus
		// https://learn.microsoft.com/en-us/windows/win32/winmsg/about-window-classes
		if (wnd.className.Contains("#32768") ||
			wnd.className.Contains("#32772")
			) return true;

		// filter out windows without the normal/default border thickness
		const int SM_CXSIZEFRAME = 32;
		if (wnd.borderThickness < User32.GetSystemMetrics(SM_CXSIZEFRAME))
			return true;

		if (!Environment.IsPrivilegedProcess && wnd.elevated) return true;

		if (IsWindowInConfigRules(wnd, "ignore"))
		{
			//Console.WriteLine($"ignoring {wnd.title} due to config rules");
			return true;
		}

		return false;
	}

	bool ShouldWindowBeFloating(Window wnd)
	{
		// despite window rules dont apply floating to dialog windows
		if (wnd.className.Contains("#32770")) return false;

		if (IsWindowInConfigRules(wnd, "floating")) return true;
		return false;
	}

	bool ShouldWindowBeTileable(Window wnd)
	{
		if (!wnd.styles.HasFlag(WINDOWSTYLE.WS_THICKFRAME)) return false; // non resizeable window
		if (wnd.className.Contains("OperationStatusWindow") || // copy, paste status windows
			wnd.className.Contains("DS_MODALFRAME")
			) return false;
		return true;
	}

	public void CleanGhostWindows()
	{
		lock (@addLock)
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
				ghostWindows.ForEach(wnd => focusedWorkspace.Remove(wnd!));
				focusedWorkspace.Update();
			}

			// windows that have been added but has gone bad and should be removed
			var rottenWindows = focusedWorkspace.windows.Where(wnd => ShouldWindowBeIgnored(wnd!)).ToList();
			rottenWindows.ForEach(wnd => focusedWorkspace.Remove(wnd!));
		}
	}

	void ApplyConfigsToWindow(Window wnd)
	{
		if (ShouldWindowBeFloating(wnd)) wnd.floating = true; else wnd.floating = false;
		if (ShouldWindowBeTileable(wnd)) wnd.tileable = true; else wnd.tileable = false;
	}

	public List<Window?> GetAllWindows()
	{
		List<Window?> windows = new();
		foreach (var wksp in workspaces)
			foreach (var wnd in wksp!.windows)
				windows.Add(wnd);
		return windows;
	}

	readonly Lock @addLock = new();
	public void WindowAdded(Window wnd)
	{
		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		foreach (var wksp in workspaces)
			if (wksp!.windows.Contains(wnd))
			{
				// This is for cases where an already added window gets focused without direct interaction
				// for eg say you click a link on your terminal and your default browser is open
				// in another workspace. The reason why we are handling it here instead of
				// WindowFocused is because the event emmited is OBJECT_SHOW rather than
				// EVENT_FOREGROUND_CHANGED
				if (wksp != focusedWorkspace) FocusWorkspace(wksp);
				return;
			}

		// Add() and CleanGhostWindows() can cause windows to be re added if they
		// occur while the other hasnt completed, so lock them
		lock (@addLock)
		{
			ApplyConfigsToWindow(wnd);
			wnd.workspace = focusedWorkspaceIndex;
			focusedWorkspace.Add(wnd);
			if (wnd.floating) focusedWorkspace.ApplyFloatingSize(wnd);
			focusedWorkspace.Update();
			//Console.WriteLine($"WindowAdded, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}, floating: {wnd.floating}, exeName: {wnd.exeName}, count: {focusedWorkspace.windows.Count}");
		}

		CleanGhostWindows();
		SaveState($"WindowAdded, wnd: {wnd.title}, exe: {wnd.exe}, tileable: {wnd.tileable}");
	}

	// search for the window in our workspace and give a local reference that
	// has all the valid states set, the window instance emmitted by window event
	// listener gives a blank window that only matches the stateless properties
	// call this in all event handlers that deal with windows events of windows
	// that already exist in the workspace so basically every one except WindowAdded
	Window? GetAlreadyStoredWindow(Window wnd)
	{
		return focusedWorkspace?.windows?.FirstOrDefault(_wnd => _wnd == wnd);
	}

	public void WindowRemoved(Window wnd)
	{
		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;

		//Console.WriteLine($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		if (focusedWorkspace.windows.Contains(wnd))
		{
			focusedWorkspace.Remove(wnd);
			focusedWorkspace.Update();
		}

		CleanGhostWindows();
		SaveState("WindowRemoved");
	}

	// window handlers must always check window properties of the already stored windows
	public void WindowMoved(Window wnd)
	{
		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;

		//Console.WriteLine($"WindowMoved, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		//var _wnd = focusedWorkspace.windows.FirstOrDefault(_wnd => _wnd == wnd);
		//if (_wnd == null) return;
		// wnd -> window being moved
		// cursorPos
		// wndEnclosingCursor -> window enclosing cursor
		if (!wnd.floating && wnd.tileable)
		{
			User32.GetCursorPos(out POINT pt);
			Window? wndUnderCursor = focusedWorkspace.GetWindowFromPoint(pt);
			if (wndUnderCursor == null) return;
			focusedWorkspace.SwapWindows(wnd, wndUnderCursor);
		}

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState("WindowMoved");
	}

	public void WindowMaximized(Window wnd)
	{
		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;

		//Console.WriteLine($"WindowMazimized, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState("WindowMaximized");
	}

	public void WindowMinimized(Window wnd)
	{
		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;

		//Console.WriteLine($"WindowMinimized, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");
		// render only after state has updated (winevent and GetWindowPlacement() is not synchronous)
		TaskEx.WaitUntil(() => wnd.state == SHOWWINDOW.SW_SHOWMINIMIZED).Wait();

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState("WindowMinimized");
	}

	public bool mouseDown { get; set; } = false;
	public void WindowRestored(Window wnd)
	{

		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		if ((wnd = GetAlreadyStoredWindow(wnd)) == null) return;
		if (mouseDown) return;

		//Console.WriteLine($"WindowRestored, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState($"WindowRestored, wnd: {wnd.title}, hWnd: {wnd.hWnd}");
	}

	Workspace? GetWindowWorkspace(Window wnd)
	{
		return workspaces.FirstOrDefault(wksp => wksp!.windows.Contains(wnd));
	}

	public void WindowFocused(Window wnd)
	{
		if (ShouldWindowBeIgnored(wnd)) return;
		if (suppressEvents) return;
		if ((wnd = GetAlreadyStoredWindow(wnd)!) == null) return;

		//Console.WriteLine($"WindowFocused, {wnd.title}, hWnd: {wnd.hWnd}, class: {wnd.className}");

		focusedWorkspace.Update();
		CleanGhostWindows();
		SaveState($"WindowFocused, {wnd.title}, {wnd.tileable}");
	}

	public WindowManagerState GetState()
	{
		WindowManagerState state = new();
		GetAllWindows().ForEach(wnd => state.windows.Add(wnd!));
		state.focusedWorkspaceIndex = focusedWorkspaceIndex;
		state.workspaceCount = workspaces.Count;
		return state;
	}

	int stateCounter = 0;
	readonly Lock @lock = new();
	public void SaveState(string? lastAction = null)
	{
		lock (@lock)
		{
			var state = GetState();
			WINDOW_MANAGER_MESSAGE_SENT(state.ToJson());
			try
			{
				File.WriteAllText(Paths.stateFile, state.ToJson());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			Console.WriteLine($"{stateCounter++}. lastAction: {lastAction}\n{state.ToJson()}");
		}
	}

	public delegate void WindowManagerMessageHandler(string message);
	public event WindowManagerMessageHandler WINDOW_MANAGER_MESSAGE_SENT = (message) => { };

	public string RequestReceived(string request)
	{
		string[] args = request.Split(" ");
		args[args.Length - 1] = args.Last().Replace("\n", "");
		//Console.WriteLine($"arg0: {args.FirstOrDefault()}, arg1: {args.ElementAtOrDefault(1)}");
		string? verb = args.FirstOrDefault();
		//Console.WriteLine($"verb: {verb}");
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
