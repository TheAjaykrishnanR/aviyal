using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class Animation<T>
    where T : IMoveable
{
    List<(T, (POINT2, POINT2))> animationObjectsAndBounds = new();
    int duration;
    string easeFunction;
    Dictionary<string, Func<double, double>> easeFunctions;

    public Animation(int duration, string easeFunction)
    {
        this.duration = duration;
        this.easeFunction = easeFunction;

        this.easeFunctions = new() { { "easeOutQuint", EaseOutQuint } };
    }

    public void Add(T animationObject, POINT2 start, POINT2 end)
    {
        (T, (POINT2, POINT2)) animationObjectAndBound = new();
        animationObjectAndBound.Item1 = animationObject;
        animationObjectAndBound.Item2.Item1 = start;
        animationObjectAndBound.Item2.Item2 = end;
        this.animationObjectsAndBounds.Add(animationObjectAndBound);
    }

    public void Play()
    {
        int fps = 60;
        int dt = (int)(1000 / fps); // milliseconds
        int frames = (int)(((float)duration / 1000) * fps);

        Stopwatch sw = new();
        sw.Start();

        for (int i = 0; i < frames; i++)
        {
            for (int j = 0; j < this.animationObjectsAndBounds.Count; j++)
            {
                T obj = animationObjectsAndBounds[j].Item1;
                POINT2 start = animationObjectsAndBounds[j].Item2.Item1;
                POINT2 end = animationObjectsAndBounds[j].Item2.Item2;

                int? x = GetCoord(start.X, end.X, frames, i);
                int? y = GetCoord(start.Y, end.Y, frames, i);

                if (Aviyal.DEBUG)
                    Logger.Log(
                        $"moving object, x: {x}, y: {y}, time: {Utils.FastTime_milli()}",
                        file: false
                    );

                obj.Move(x, y, verify: false, redraw: false);
            }

            int wait = (int)(i * dt - sw.ElapsedMilliseconds);
            wait = wait < 0 ? 0 : wait;
            Thread.Sleep(wait);
        }
        sw.Stop();
    }

    int? GetCoord(int? start, int? end, int frames, int frame)
    {
        if (start == null || end == null)
            return null;
        double progress = (double)frame / frames;
        progress = easeFunctions[easeFunction].Invoke(progress);
        return start + (int)((end - start) * progress);
    }

    public double EaseOutQuint(double x)
    {
        return 1 - Math.Pow(1 - x, 3);
    }
}

public struct POINT2
{
    public int? X;
    public int? Y;
}
