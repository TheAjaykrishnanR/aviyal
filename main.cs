using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

class _
{
	[DllImport("user32.dll")]
	static extern int SetWindowPos(nint hWnd, nint z, int x, int y, int cx, int cy, uint flags);
	[DllImport("user32.dll")]
	static extern int GetWindowRect(nint hWnd, out RECT rect);

	const int SWP_NOACTIVATE = 0x0010;

	static int duration = 1000; // milliseconds
	static int fps = 60;
	static int dt = (int)(1000 / fps); // milliseconds
	static int frames = (int)(((float)duration / 1000) * fps);

	static int zoom = 100;
	static async Task Main()
	{
		Console.Write("hWnd: ");
		nint hWnd = (nint)Convert.ToInt64(Console.ReadLine(), 16);

		Console.WriteLine($"dt: {dt} ms, frames: {frames}");

		RECT start, end;
		GetWindowRect(hWnd, out start);
		end = new()
		{
			left = start.left - zoom,
			top = start.top - zoom,
			right = start.right + zoom,
			bottom = start.bottom + zoom
		};
		Stopwatch sw = new();
		sw.Start();
		for (int i = 0; i < frames; i++)
		{
			RECT frameRect = GetRect(start, end, i);
			int cx = frameRect.right - frameRect.left;
			int cy = frameRect.bottom - frameRect.top;
			SetWindowPos(hWnd, 0, frameRect.left, frameRect.top, cx, cy, SWP_NOACTIVATE);
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
		double progress = (double)frame / frames;
		// easeInSine
		progress = EaseInOutQuint(progress);
		//
		RECT x = new RECT()
		{
			left = start.left + (int)((end.left - start.left) * progress),
			top = start.top + (int)((end.top - start.top) * progress),
			right = start.right + (int)((end.right - start.right) * progress),
			bottom = start.bottom + (int)((end.bottom - start.bottom) * progress)
		};
		//Console.WriteLine($"x -> {x.right}, y -> {x.bottom}");
		return x;
	}

	public static double EaseInOutQuint(double x)
	{
		return x < 0.5
			? 16 * x * x * x * x * x
			: 1 - Math.Pow(-2 * x + 2, 5) / 2;
	}
}

struct RECT
{
	public int left;
	public int top;
	public int right;
	public int bottom;
}
