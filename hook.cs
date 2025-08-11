using System;
using System.Runtime.InteropServices;

class _
{
	[DllImport("kernel32.dll", SetLastError = true)]
	static extern uint GetCurrentThreadId();

	delegate int HookProc(int code, nint wparam, nint lparam);

	const int WH_CBT = 5;

	[DllImport("user32.dll", SetLastError = true)]
	static extern nint SetWindowsHookExA(int id, HookProc proc, nint hmod, uint threadId);

	static void Main()
	{
		HookProc hookProc = (int code, nint wparam, nint lparam) =>
		{
			Console.WriteLine("from hook");
			return 0;
		};
		HandleError(SetWindowsHookExA(WH_CBT, hookProc, 0, GetCurrentThreadId()));
		Console.ReadLine();
	}

	static void HandleError(nint code)
	{
		if (code == 0)
		{
			throw new Exception($"win32: {Marshal.GetLastWin32Error()}");
		}
	}
}
