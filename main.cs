using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

class _
{
	[DllImport("user32.dll")]
	static extern int MoveWindow(nint hWnd, int x, int y, int cx, int cy, bool redraw);
	[DllImport("user32.dll")]
	static extern int SetWindowPos(nint hWnd, nint z, int x, int y, int cx, int cy, uint flags);
	[DllImport("user32.dll")]
	static extern int InvalidateRect(nint hWnd, nint rect, bool erase);
	[DllImport("user32.dll")]
	static extern int GetWindowRect(nint hWnd, out RECT rect);

	const int SWP_NOREDRAW = 0x0008;

	static int duration = 600; // milliseconds
	static int fps = 60;
	static int dt = (int)(1000 / fps); // milliseconds
	static int frames = (int)(((float)duration / 1000) * fps);

	static int extend = 100;
	static async Task Main()
	{
		Console.Write("hWnd: ");
		nint hWnd = (nint)Convert.ToInt64(Console.ReadLine(), 16);

		Console.WriteLine($"dt: {dt} ms, frames: {frames}");

		RECT start, end;
		GetWindowRect(hWnd, out start);
		end = new()
		{
			left = start.left,
			top = start.top,
			right = start.right + extend,
			bottom = start.bottom + extend
		};
		Stopwatch sw = new();
		sw.Start();
		for (int i = 0; i < frames; i++)
		{
			RECT frameRect = GetRect(start, end, i);
			int cx = frameRect.right - frameRect.left;
			int cy = frameRect.bottom - frameRect.top;
			SetWindowPos(hWnd, 0, frameRect.left, frameRect.top, cx, cy, SWP_NOREDRAW);
			int wait = (int)(i * dt - sw.ElapsedMilliseconds);
			wait = wait < 0 ? 0 : wait;
			Console.WriteLine($"{i}. wait: {wait}");
			await Task.Delay(wait);
		}
		sw.Stop();
		Console.WriteLine($"total: {sw.ElapsedMilliseconds} ms");
	}

	static RECT GetRect(RECT start, RECT end, int frame)
	{
		float progress = (float)frame / frames;
		RECT x = new RECT()
		{
			left = start.left,
			top = start.top,
			right = start.right + (int)((end.right - start.right) * progress),
			bottom = start.bottom + (int)((end.bottom - start.bottom) * progress)
		};
		//Console.WriteLine($"x -> {x.right}, y -> {x.bottom}");
		return x;
	}
}

struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;
}
