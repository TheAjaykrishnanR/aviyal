using System;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

class _
{
	[DllImport("user32.dll")]
	static extern int MoveWindow(nint hWnd, int x, int y, int cx, int cy, bool redraw);
	[DllImport("user32.dll")]
	static extern int InvalidateRect(nint hWnd, nint rect, bool erase);
	[DllImport("user32.dll")]
	static extern int GetWindowRect(nint hWnd, out RECT rect);

	static int duration = 1000; // milliseconds
	static int fps = 60;
	static int dt = (int)(1000 / fps); // milliseconds
	static int frames = (int)(duration / fps);

	static void Main()
	{
		Console.Write("hWnd: ");
		nint hWnd = (nint)Convert.ToInt64(Console.ReadLine(), 16);

		RECT start, end;
		GetWindowRect(hWnd, out start);
		end = new()
		{
			left = start.left,
			top = start.top,
			right = start.right + 100,
			bottom = start.bottom + 100
		};
		for (int i = 0; i < frames; i++)
		{
			RECT frameRect = GetRect(start, end, i);
			int cx = frameRect.right - frameRect.left;
			int cy = frameRect.bottom - frameRect.top;
			MoveWindow(hWnd, frameRect.left, frameRect.top, cx, cy, false);
			InvalidateRect(hWnd, 0, false);
			Thread.Sleep(dt);
		}
	}

	static RECT GetRect(RECT start, RECT end, int frame)
	{
		return new RECT()
		{
			left = start.left,
			top = start.top,
			right = start.right + (int)((end.right - start.right) / frames) * frame,
			bottom = start.bottom + (int)((end.bottom - start.bottom) / frames) * frame
		};
	}
}

struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;
}
