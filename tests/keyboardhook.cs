using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

class _Main()
{
	delegate int KeyboardProc(int code, nint wparam, nint lparam);

	[DllImport("user32.dll", SetLastError = true)]
	static extern nint SetWindowsHookExA(int idHook, KeyboardProc lpfn, nint hmod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	static extern int CallNextHookEx(nint hhk, int nCode, nint wparam, nint lparam);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetMessage(out uint msg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool TranslateMessage(ref uint msg);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool DispatchMessage(ref uint msg);
	static void Main()
	{
		KeyboardProc proc = (int code, nint wparam, nint lparam) =>
		{
			Console.WriteLine($"code: {code}, wparam: {wparam}, lparam: {lparam}");
			return CallNextHookEx(0, code, wparam, lparam);
		};
		const int WH_KEYBOARD_LL = 13;
		// hmod = 0, hook function is in code
		// dwThreadId = 0, hook all threads
		nint hhook = SetWindowsHookExA(WH_KEYBOARD_LL, proc, Process.GetCurrentProcess().MainModule.BaseAddress, 0);
		// always use a message pump, instead of: while(Console.ReadLine() != ":q") { }
		while (true)
		{
			int _ = GetMessage(out uint msg, 0, 0, 0);
			TranslateMessage(ref msg);
			DispatchMessage(ref msg);
		}
	}
}
