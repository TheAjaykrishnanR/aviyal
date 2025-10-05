using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class Dwindle : ILayout
{
	public RECT[] GetRect(int count)
	{
		// rects with margin
		RECT[] rects = new RECT[count];
		// rects with no margins
		RECT[] fillRects = new RECT[count];
		(int width, int height) = Utils.GetScreenSize();
		FillDirection fillDirection = FillDirection.HORIZONTAL;
		// where the nth window will go
		RECT fillRect = new() { Left = 0, Top = 0, Right = width, Bottom = height };
		for (int i = 0; i < count; i++)
		{
			fillRects[i] = fillRect;

			// modify the fillRect
			switch (fillDirection)
			{
				case FillDirection.HORIZONTAL:
					if (i - 1 >= 0)
					{
						fillRects[i - 1] = TopHalf(fillRects[i - 1]);
					}
					fillRect.Left += (int)((fillRect.Right - fillRect.Left) / 2);
					break;
				case FillDirection.VERTICAL:
					if (i - 1 >= 0)
					{
						fillRects[i - 1] = LeftHalf(fillRects[i - 1]);
					}
					fillRect.Top += (int)((fillRect.Bottom - fillRect.Top) / 2);
					break;
			}
			fillDirection = fillDirection == FillDirection.HORIZONTAL ? FillDirection.VERTICAL : FillDirection.HORIZONTAL;

		}
		fillRects.Index().ToList().ForEach(irect => Console.WriteLine($"{irect.Item1}. L:{irect.Item2.Left} R:{irect.Item2.Right} T:{irect.Item2.Top} B:{irect.Item2.Bottom}"));
		return ApplyInner(ApplyOuter(fillRects));
	}
	public int outer { get; set; } = 5;
	public int inner { get; set; } = 5;

	RECT LeftHalf(RECT rect)
	{
		rect.Right -= (int)((rect.Right - rect.Left) / 2);
		return rect;
	}
	RECT TopHalf(RECT rect)
	{
		rect.Bottom -= (int)((rect.Bottom - rect.Top) / 2);
		return rect;
	}

	// applies outer margins
	RECT[] ApplyOuter(RECT[] fillRects)
	{
		(int width, int height) = Utils.GetScreenSize();
		for (int i = 0; i < fillRects.Length; i++)
		{
			if (fillRects[i].Left == 0) fillRects[i].Left += outer;
			if (fillRects[i].Top == 0) fillRects[i].Top += outer;
			if (fillRects[i].Right == width) fillRects[i].Right -= outer;
			if (fillRects[i].Bottom == height) fillRects[i].Bottom -= outer;
		}
		return fillRects;
	}

	// applies inner margins (apply only after outer)
	RECT[] ApplyInner(RECT[] fillRects)
	{
		(int width, int height) = Utils.GetScreenSize();
		for (int i = 0; i < fillRects.Length; i++)
		{
			if (fillRects[i].Left != outer) fillRects[i].Left += (int)(inner / 2);
			if (fillRects[i].Top != outer) fillRects[i].Top += (int)(inner / 2);
			if (fillRects[i].Right != width - outer) fillRects[i].Right -= (int)(inner / 2);
			if (fillRects[i].Bottom != height - outer) fillRects[i].Bottom -= (int)(inner / 2);
		}
		return fillRects;
	}
}
