using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class _
{
	[DllImport("kernel32.dll")]
	static extern nint LoadLibrary(string name);

	[DllImport("kernel32.dll")]
	static extern nint GetProcAddress(nint dllBase, string procName);

	[DllImport("user32.dll")]
	static extern nint SetWindowsHookExA(int id, nint proc, nint hMod, nint threadId);

	[DllImport("user32.dll")]
	static extern int UnhookWindowsHookEx(nint hhook);

	[DllImport("user32.dll")]
	static extern int SendNotifyMessageA(nint hWnd, uint msg, nint wparam, nint lparam);

	const int WH_CBT = 5;

	[DllImport("user32.dll")]
	static extern int CallNextHookEx(nint hhook, int code, nint wparam, nint lparam);

	static void Main()
	{
		nint hookBase = LoadLibrary("hook.dll");
		nint hookFn = GetProcAddress(hookBase, "CreateWindowHook");
		nint hhook = SetWindowsHookExA(WH_CBT, hookFn, hookBase, 0);
		Console.WriteLine($"hhook: {hhook}");
		while (Console.ReadLine() != ":q") { }
		;
		UnhookWindowsHookEx(hhook);
		SendNotifyMessageA(0xffff, 0, 0, 0);
	}
}
