using System;
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

	public string className { get; }
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
	public async void Focus()
	{
		// simulate an ALT key press inorder to focus and not just flash in
		// the taskbar
		// https://stackoverflow.com/a/13881647
		const uint EXTENDEDKEY = 0x1;
		const uint KEYUP = 0x2;
		User32.keybd_event((byte)VK.LMENU, 0x3C, EXTENDEDKEY, 0);
		User32.keybd_event((byte)VK.LMENU, 0x3C, EXTENDEDKEY | KEYUP, 0);

		User32.SetForegroundWindow(this.hWnd);

		// dont leave this function until focusWindow gets stable
		await TaskEx.WaitUntil(() => this.hWnd == User32.GetForegroundWindow());
	}

	public void Move(RECT pos)
	{
		// remove frame bounds
		RECT margin = GetFrameMargin();
		pos.Left -= margin.Left;
		pos.Top -= margin.Top;
		pos.Right -= margin.Right;
		pos.Bottom -= margin.Bottom;

		User32.SetWindowPos(this.hWnd, (nint)SWPZORDER.HWND_BOTTOM, pos.Left, pos.Top, pos.Right - pos.Left, pos.Bottom - pos.Top, SETWINDOWPOS.SWP_NOACTIVATE);
	}

	public void Move(int? x, int? y)
	{
		User32.SetWindowPos(this.hWnd, 0, x ?? rect.Left, y ?? rect.Top, 0, 0, SETWINDOWPOS.SWP_NOSIZE | SETWINDOWPOS.SWP_NOACTIVATE);
		TaskEx.WaitUntil(() => (rect.Left == (x == null ? rect.Left : x)) && rect.Top == (y == null ? rect.Top : y)).Wait();
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
	public ILayout layout { get; set; } = new Dwindle();

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
		if (focusedWindowIndex == null) return;
		int? index = layout.GetAdjacent((int)focusedWindowIndex, direction);
		if (index != null) windows[(int)index].Focus();
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
		wnd.floating = !wnd.floating;
		Console.WriteLine($"[ TOGGLE FLOATING ] : {wnd.floating}, [ {config.floatingWindowSize} ]");
		wnd.Move(GetCenterRect(floatingWindowSize.Item1, floatingWindowSize.Item2));
		Focus();
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

	Config config;
	public WindowManager(Config config)
	{
		this.config = config;

		List<nint>? hWnds = Utils.GetAllTaskbarWindows();
		hWnds?.ForEach(hWnd =>
		{
			initWindows.Add(new(hWnd));
		});
		initWindows = initWindows.Where(wnd => wnd.title.Contains("windowgen")).ToList();
		initWindows.ForEach(wnd => Console.WriteLine($"Title: {wnd.title}, hWnd: {wnd.hWnd}"));

		for (int i = 0; i < WORKSPACES; i++)
		{
			Workspace wksp = new(config);
			workspaces.Add(wksp);
		}
		// add all windows to 1st workspace
		initWindows.ForEach(wnd => workspaces.First().windows.Add(wnd));
		FocusWorkspace(workspaces.First());
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

		//System.Timers.Timer timer = new(dt);
		//int frame = 0;
		//timer.Elapsed += (s, e) =>
		//{
		//	if (frame == frames) timer.Stop();
		//	int x = GetX(startX, endX, frames, frame++);
		//	wksp.Move(x, null);
		//	Console.WriteLine($"frame: {frame}, dt: {dt}, x: {x}");
		//};
		//timer.Start();
	}

	public void FocusNextWorkspace()
	{
		int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
		int prev = focusedWorkspaceIndex > 0 ? focusedWorkspaceIndex - 1 : workspaces.Count - 1;

		if (config.workspaceAnimations)
		{
			// move left
			(int w, int h) = Utils.GetScreenSize();

			workspaces[next].Hide(); // if not, a slight flicker will appear because the windows apparently rememeber its last location even after Move for a short while

			workspaces[next].Move(w, null);
			workspaces[next].Focus();

			int duration = 500;
			List<Task> _ts = new();
			_ts.Add(Task.Run(() => WorkspaceAnimate(focusedWorkspace, 0, -w, duration)));
			_ts.Add(Task.Run(() => WorkspaceAnimate(workspaces[next], w, 0, duration)));
			Task.WhenAll(_ts).Wait();
			focusedWorkspace.Hide();
			focusedWorkspace = workspaces[next];
			focusedWorkspace.Update();
		}

		else
		{
			FocusWorkspace(workspaces[next]);
			Console.WriteLine($"NEXT, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
		}
	}

	public void FocusPreviousWorkspace()
	{
		int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
		int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;

		if (!config.workspaceAnimations)
		{
			// move right
		}
		else
		{
			FocusWorkspace(workspaces[prev]);
			Console.WriteLine($"PREVIOUS, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
		}
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
	}

	public void ShiftFocusedWindowToNextWorkspace()
	{
		int next = focusedWorkspaceIndex >= workspaces.Count - 1 ? 0 : focusedWorkspaceIndex + 1;
		ShiftFocusedWindowToWorkspace(next);
	}

	public void ShiftFocusedWindowToPreviousWorkspace()
	{
		int prev = focusedWorkspaceIndex <= 0 ? workspaces.Count - 1 : focusedWorkspaceIndex - 1;
		ShiftFocusedWindowToWorkspace(prev);
	}

	public void WindowAdded(Window wnd)
	{
		Console.WriteLine($"WindowAdded, {wnd.title}, hWnd: {wnd.hWnd}, focusedWorkspaceIndex: {focusedWorkspaceIndex}");
		focusedWorkspace.Add(wnd);
		focusedWorkspace.Focus();
		//wnd.SetBottom(); // if you wanna move the window back to even the terminal while debugging
	}
	public void WindowRemoved(Window wnd)
	{
		Console.WriteLine($"WindowRemoved, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Remove(wnd);
		focusedWorkspace.Focus();
	}
	public void WindowMoved(Window wnd)
	{
		Console.WriteLine($"WindowMoved, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Focus();
	}

	public void WindowMaximized(Window wnd)
	{
		Console.WriteLine($"WindowMazimized, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Focus();
	}

	public async void WindowMinimized(Window wnd)
	{
		Console.WriteLine($"WindowMinimized, {wnd.title}, hWnd: {wnd.hWnd}");
		// render only after state has updated (winevent and GetWindowPlacement() is not synchronous)
		await TaskEx.WaitUntil(() => wnd.state == SHOWWINDOW.SW_SHOWMINIMIZED);
		focusedWorkspace.Focus();
	}
	public void WindowRestored(Window wnd)
	{
		Console.WriteLine($"WindowRestored, {wnd.title}, hWnd: {wnd.hWnd}");
		focusedWorkspace.Focus();
	}
}

enum FillDirection
{
	HORIZONTAL,
	VERTICAL
}
