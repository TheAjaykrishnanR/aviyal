using System;
using System.Runtime.InteropServices;

class _
{
	[DllImport("user32.dll")]
	static extern int CallNextHookEx(nint hhook, int code, nint wparam, nint lparam);

	[DllImport("kernel32.dll")]
	static extern void OutputDebugString(string text);

	[UnmanagedCallersOnly(EntryPoint = "CreateWindowHook")]
	public static nint CreateWindowHook(int code, nint wparam, nint lparam)
	{
		switch (code)
		{
			case 3:
				OutputDebugString("from hook.dll");
				break;
		}
		CallNextHookEx(0, code, wparam, lparam);
		return 0;
	}
}
