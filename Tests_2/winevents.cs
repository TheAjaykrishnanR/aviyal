using System;
using System.Runtime.InteropServices;

class _
{
	delegate void WINEVENTPROC(
		nint hWinEventHook,
		WINEVENT msg,
		nint hWnd,
		int idObject,
		int idChild,
		uint idEventThread,
		uint dwmsEventTime
	);

	[DllImport("user32.dll", SetLastError = true)]
	static extern nint SetWinEventHook(
		uint eventMin,
		uint eventMax,
		nint hMod,
		WINEVENTPROC proc,
		uint idProcess,
		uint idThread,
		uint dwFlags
	);

	static void winEventProc(
		nint hWinEventHook,
		WINEVENT msg,
		nint hWnd,
		int idObject,
		int idChild,
		uint idEventThread,
		uint dwmsEventTime)
	{
		Console.WriteLine($"hookMsg: {msg}");
	}
	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetMessage(out uint msg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool TranslateMessage(ref uint msg);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DispatchMessage(ref uint msg);

	static void Main()
	{
		uint WINEVENT_OUTOFCONTEXT = 0;
		Console.WriteLine("SetWinEventHook...");
		nint ret = SetWinEventHook(0x00000001, 0x7FFFFFFF, 0, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT | 0x0001 | 0x0002);
		Console.WriteLine($"hook: {ret}");
		// message loop
		while (true)
		{
			int _ = GetMessage(out uint msg, 0, 0, 0);
			TranslateMessage(ref msg);
			DispatchMessage(ref msg);
		}
	}
}

enum WINEVENT : uint
{
	OBJECT_CREATE = 0x8001,
	OBJECT_DESTROY = 0x8002,
	OBJECT_SHOW = 0x8003,
	OBJECT_HIDE = 0x8004,
	OBJECT_REORDER = 0x8005,
}
