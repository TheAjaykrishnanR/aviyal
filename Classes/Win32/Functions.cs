/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
using System.Runtime.InteropServices;
using System.Text;

public class User32
{
	[DllImport("user32.dll", SetLastError = true)]
	public static extern int MessageBox(nint hWnd, string title, string message, uint type);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern uint GetWindowLong(nint hWnd, GETWINDOWLONG nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, SETWINDOWPOS uFlags);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint FindWindow(string className, string windowName);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int ShowWindow(nint hWnd, SHOWWINDOW nCmdShow);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint FindWindowEx(nint hWndParent, nint hWndChildAfter, string className, string windowName);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int ShowWindowAsync(nint hWnd, SHOWWINDOW nCmdShow);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetCursorPos(out POINT cursorPos);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowRect(nint hWnd, out RECT windowRect);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetClientRect(nint hWnd, out RECT clientRect);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint GetForegroundWindow();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int AllowSetForegroundWindow(uint dwProcessId);

	[DllImport("user32.dll")]
	public static extern nint WindowFromPoint(POINT Point);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool EnumWindows(EnumWindowProc enumWindowProc, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool EnumChildWindows(nint hWndParent, EnumWindowProc enumWindowProc, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool GetWindowThreadProcessId(nint hWnd, out uint processId);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint CreateWindowEx(
		WINDOWSTYLE dwExStyle,
		string lpClassName,
		string lpWindowName,
		WINDOWSTYLE dwStyle,
		int X,
		int Y,
		int nWidth,
		int nHeight,
		nint hWndParent,
		nint hMenu,
		nint hInstance,
		nint lpParam
	);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern ushort RegisterClassEx(ref WNDCLASSEX wc);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool UnregisterClass(string className, nint hInstance);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern nint GetModuleHandle(string moduleName);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint DefWindowProc(nint hWnd, WINDOWMESSAGE uMsg, nint wParam, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int DestroyWindow(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetMessage(out MSG msg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool TranslateMessage(ref MSG msg);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DispatchMessage(ref MSG msg);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern void PostQuitMessage(int nExitCode);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern uint RegisterWindowMessage(string message);

	/// <summary>
	/// Use when msg is not in WINDOWMESSAGE enum, like in situations where a new boradcast
	/// message has to be sent
	/// </summary>
	/// <param name="hWnd"></param>
	/// <param name="msg"></param>
	/// <param name="wParam"></param>
	/// <param name="lParam"></param>
	/// <returns></returns>

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SendNotifyMessage(
		nint hWnd,
		uint msg,
		nint wParam,
		nint lParam
	);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SendMessage(
		nint hWnd,
		uint msg,
		nint wParam,
		nint lParam
	);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern void SetTimer(nint hWnd, nint nIdEvent, uint uElapse, TIMERPROC timerProc);

	[DllImport("user32.dll", SetLastError = true)]
	public extern static nint GetAncestor(
	  nint hwnd,
	  uint gaFlags
	);

	[DllImport("user32.dll", SetLastError = true)]
	public extern static nint GetLastActivePopup(
		nint hWnd
	);

	[DllImport("user32.dll", SetLastError = true)]
	public extern static bool IsWindowVisible(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	public extern static int AnimateWindow(nint hWnd, uint dwTime, ANIMATEWINDOW dwFlags);

	// return hMonitor or the monitor handle

	[DllImport("user32.dll", SetLastError = true)]
	public extern static nint MonitorFromPoint(POINT pt, uint dwFlags);

	[DllImport("user32.dll", SetLastError = true)]
	public extern static nint MonitorFromWindow(nint hWnd, uint dwFlags);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern nint SetParent(nint hWndChild, nint hWndNewParent);
}

public class Shell32
{
	[DllImport("shell32.dll", SetLastError = true)]
	public static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

	[DllImport("shell32.dll", SetLastError = true)]
	public static extern long Shell_NotifyIconGetRect(ref _NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

	[DllImport("shell32.dll", SetLastError = true)]
	public static extern uint ExtractIconEx(string exePath, int nIconIndex, out nint iconLarge, out nint iconSmall, uint nIcons);
}

public class Kernel32
{
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool AttachConsole(int processId);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern nint GetModuleHandle(string moduleName);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern nint OpenProcess(uint processAccess, bool bInheritHandle, int processId);

	[DllImport("kernel32.dll")]
	public static extern uint GetLogicalDriveStringsW(
	  uint nBufferLength,
	  StringBuilder lpBuffer
	);

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern uint QueryDosDevice(
	  string lpDeviceName,
	  StringBuilder lpTargetPath,
	  uint ucchMax
	);
}

public class Dwmapi
{
	[DllImport("dwmapi.dll", SetLastError = true)]
	public static extern int DwmSetWindowAttribute(nint hWnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);

	[DllImport("dwmapi.dll", SetLastError = true)]
	public static extern int DwmGetWindowAttribute(
		nint hWnd,
		uint dwAttribute,
		nint pvAttribute,
		uint cbAttribute
	);
}

public class Psapi
{
	[DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern uint GetModuleFileNameEx(nint hProcess, nint hModule, out StringBuilder moduleFileName, uint nSize);
}

/// <summary>
/// Query kernel objects
/// </summary>
public class Ntdll
{
	[DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern int NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS infoType, ref SYSTEM_PROCESS_ID_INFORMATION info, uint infoLength, out uint returnLength);

	[DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern int NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS infoType, ref SYSTEM_BASIC_INFORMATION info, uint infoLength, out uint returnLength);

	[DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern int NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS infoType, nint info, uint infoLength, out uint returnLength);

}

public class Shcore
{
	// retrieves monitor scaling info
	// MONITOR_DEFAULTTONULL
	// 0x00000000
	// Returns NULL.
	// MONITOR_DEFAULTTOPRIMARY
	// 0x00000001
	// Returns a handle to the primary display monitor.
	// MONITOR_DEFAULTTONEAREST
	// 0x00000002
	// Returns a handle to the display monitor that is nearest to the point.
	[DllImport("shcore.dll", SetLastError = true)]
	public static extern int GetScaleFactorForMonitor(nint hMon, out DEVICE_SCALE_FACTOR scaleFactor);

	[DllImport("shcore.dll", SetLastError = true)]
	public static extern int GetDpiForMonitor(nint hMon, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

}

public class Iphlpapi
{
	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getipnetworkconnectionbandwidthestimates
	/// </summary>
	/// <param name="interfaceIndex"></param>
	/// <param name="adressFamily"></param>
	/// <param name="info"></param>
	/// <returns></returns>
	[DllImport("iphlpapi.dll", SetLastError = true)]
	public static extern int GetIpNetworkConnectionBandwidthEstimates(int interfaceIndex, ADRESS_FAMILY adressFamily, out _MIB_IP_NETWORK_CONNECTION_BANDWIDTH_ESTIMATES info);
}

public class Winmm
{

}
