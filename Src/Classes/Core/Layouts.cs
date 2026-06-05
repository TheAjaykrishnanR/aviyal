using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public class Dwindle : ILayout
{
    // rects with margin
    RECT[] rects = null;

    // rects with no margins
    RECT[] fillRects = null;

    public RECT[] GetRects(
        int count /* no of windows */
    )
    {
        rects = new RECT[count];
        fillRects = new RECT[count];

        (int width, int height) = Utils.GetScreenSize();
        FillDirection fillDirection = FillDirection.HORIZONTAL;
        // where the nth window will go
        RECT fillRect = new()
        {
            Left = 0,
            Top = 0,
            Right = width,
            Bottom = height,
        };
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
            fillDirection =
                fillDirection == FillDirection.HORIZONTAL
                    ? FillDirection.VERTICAL
                    : FillDirection.HORIZONTAL;
        }
        //fillRects.Index().ToList().ForEach(irect => Console.WriteLine($"{irect.Item1}. L:{irect.Item2.Left} R:{irect.Item2.Right} T:{irect.Item2.Top} B:{irect.Item2.Bottom}"));
        //return ApplyInner(ApplyOuter(fillRects.ToArray()));
        return fillRects;
    }

    public int left { get; set; }
    public int top { get; set; }
    public int right { get; set; }
    public int bottom { get; set; }
    public int inner { get; set; }

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
    public RECT[] ApplyOuter(RECT[] fillRects)
    {
        (int width, int height) = Utils.GetScreenSize();
        for (int i = 0; i < fillRects.Length; i++)
        {
            if (fillRects[i].Left == 0)
                fillRects[i].Left += left;
            if (fillRects[i].Top == 0)
                fillRects[i].Top += top;
            if (fillRects[i].Right == width)
                fillRects[i].Right -= right;
            if (fillRects[i].Bottom == height)
                fillRects[i].Bottom -= bottom;
        }
        return fillRects;
    }

    // applies inner margins (apply only after outer)
    public RECT[] ApplyInner(RECT[] fillRects)
    {
        (int width, int height) = Utils.GetScreenSize();
        for (int i = 0; i < fillRects.Length; i++)
        {
            if (fillRects[i].Left != left)
                fillRects[i].Left += (int)(inner / 2);
            if (fillRects[i].Top != top)
                fillRects[i].Top += (int)(inner / 2);
            if (fillRects[i].Right != width - right)
                fillRects[i].Right -= (int)(inner / 2);
            if (fillRects[i].Bottom != height - bottom)
                fillRects[i].Bottom -= (int)(inner / 2);
        }
        return fillRects;
    }

    EDGE[] GetEdges(RECT rect, int screenWidth, int screenHeight)
    {
        List<EDGE> edges = new();
        //Console.WriteLine($"GetEdges: {rect.Left}");
        if (rect.Left == 0)
            edges.Add(EDGE.LEFT);
        if (rect.Top == 0)
            edges.Add(EDGE.TOP);
        if (rect.Right == screenWidth)
            edges.Add(EDGE.RIGHT);
        if (rect.Bottom == screenHeight)
            edges.Add(EDGE.BOTTOM);
        return edges.ToArray();
    }

    public int? GetAdjacent(int index, EDGE direction)
    {
        // 1. figure out if the window is on an edge
        // 2. if not just add +1 to index if direction is RIGHT, -1 if direction is LEFT
        // 3. if at edge return index
        if (index > fillRects.Length - 1)
            return null;
        (int width, int height) = Utils.GetScreenSize();
        EDGE[] edges = GetEdges(fillRects[index], width, height);
        //Console.WriteLine("edgesCount: " + edges.Length);
        edges.ToList().ForEach(edge => Console.Write($"{edge}, "));

        if (edges.Contains(direction))
            return index;
        else
        {
            if (direction == EDGE.LEFT || direction == EDGE.TOP)
                return index - 1;
            else
                return index + 1;
        }
    }

    public Dwindle(Config config)
    {
        this.left = config.left;
        this.right = config.right;
        this.top = config.top;
        this.bottom = config.bottom;
        this.inner = config.inner;
    }
}

public class Stack : ILayout
{
    public int left { get; set; }
    public int top { get; set; }
    public int right { get; set; }
    public int bottom { get; set; }
    public int inner { get; set; }

    public RECT[] GetRects(int count)
    {
        (int width, int height) = Utils.GetScreenSize();
        RECT[] fillRects = new RECT[count];
        for (int i = 0; i < count; i++)
        {
            fillRects[i].Left = 0;
            fillRects[i].Top = 0;
            fillRects[i].Right = width;
            fillRects[i].Bottom = height;
        }
        return fillRects;
    }

    public RECT[] ApplyInner(RECT[] fillRects)
    {
        return fillRects;
    }

    public RECT[] ApplyOuter(RECT[] fillRects)
    {
        (int width, int height) = Utils.GetScreenSize();
        for (int i = 0; i < fillRects.Length; i++)
        {
            if (fillRects[i].Left == 0)
                fillRects[i].Left += left;
            if (fillRects[i].Top == 0)
                fillRects[i].Top += top;
            if (fillRects[i].Right == width)
                fillRects[i].Right -= right;
            if (fillRects[i].Bottom == height)
                fillRects[i].Bottom -= bottom;
        }
        return fillRects;
    }

    public int? GetAdjacent(int index, EDGE direction)
    {
        return null;
    }

    public Stack(Config config)
    {
        this.left = config.left;
        this.right = config.right;
        this.top = config.top;
        this.bottom = config.bottom;
        this.inner = config.inner;
    }
}

/* One main window on the left and the rest are vertically stacked on the right
 * */
public class Master : ILayout
{
    public int left { get; set; }
    public int top { get; set; }
    public int right { get; set; }
    public int bottom { get; set; }
    public int inner { get; set; }

    RECT[] rects = null;
    RECT[] fillRects = null;

    public RECT[] GetRects(int count)
    {
        rects = new RECT[count];
        fillRects = new RECT[count];

        if (count == 0)
            return fillRects;

        (int width, int height) = Utils.GetScreenSize();

        fillRects[0] = new()
        {
            Left = 0,
            Top = 0,
            Right = width,
            Bottom = height,
        };
        if (count == 1)
            return fillRects;

        fillRects[0] = new()
        {
            Left = 0,
            Top = 0,
            Right = width / 2,
            Bottom = height,
        };
        int dy = height / (count - 1);
        for (int i = 1; i < count; i++)
        {
            fillRects[i] = new()
            {
                Left = width / 2,
                Top = (i - 1) * dy,
                Right = width,
                Bottom = i * dy,
            };
        }

        return fillRects;
    }

    public RECT[] ApplyOuter(RECT[] fillRects)
    {
        (int width, int height) = Utils.GetScreenSize();
        for (int i = 0; i < fillRects.Length; i++)
        {
            if (fillRects[i].Left == 0)
                fillRects[i].Left += left;
            if (fillRects[i].Top == 0)
                fillRects[i].Top += top;
            if (fillRects[i].Right == width)
                fillRects[i].Right -= right;
            if (fillRects[i].Bottom == height)
                fillRects[i].Bottom -= bottom;
        }
        return fillRects;
    }

    // applies inner margins (apply only after outer)
    public RECT[] ApplyInner(RECT[] fillRects)
    {
        (int width, int height) = Utils.GetScreenSize();
        for (int i = 0; i < fillRects.Length; i++)
        {
            if (fillRects[i].Left != left)
                fillRects[i].Left += (int)(inner / 2);
            if (fillRects[i].Top != top)
                fillRects[i].Top += (int)(inner / 2);
            if (fillRects[i].Right != width - right)
                fillRects[i].Right -= (int)(inner / 2);
            if (fillRects[i].Bottom != height - bottom)
                fillRects[i].Bottom -= (int)(inner / 2);
        }
        return fillRects;
    }

    public int? GetAdjacent(int index, EDGE direction)
    {
        if (fillRects.Length == 0 || fillRects.Length == 1)
            return null;

        if (index == 0)
        {
            if (direction == EDGE.RIGHT)
                return 1;
            return null; // LEFT, TOP, BOTTOM
        }
        if (index == 1)
        {
            if (direction == EDGE.TOP || direction == EDGE.RIGHT)
                return null;
            if (direction == EDGE.BOTTOM)
                return 2;
            return 0; // LEFT
        }
        if (index == fillRects.Length - 1)
        {
            if (direction == EDGE.BOTTOM || direction == EDGE.RIGHT)
                return null;
            if (direction == EDGE.TOP)
                return index - 1;
            return 0; // LEFT
        }

        // windows in the middle of the right vertical stack
        if (direction == EDGE.RIGHT)
            return null;
        if (direction == EDGE.LEFT)
            return 0;
        if (direction == EDGE.TOP)
            return index - 1;
        if (direction == EDGE.BOTTOM)
            return index + 1;

        return null;
    }

    public Master(Config config)
    {
        this.left = config.left;
        this.right = config.right;
        this.top = config.top;
        this.bottom = config.bottom;
        this.inner = config.inner;
    }
}

public enum EDGE
{
    LEFT,
    TOP,
    RIGHT,
    BOTTOM,
}
