using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public class Animation<T> where T : IMoveable
{
    private readonly List<(T, (POINT2, POINT2))> animationObjectsAndBounds = new();
    private readonly int duration;
    private readonly string easeFunction;
    private readonly Dictionary<string, Func<double, double>> easeFunctions;

    public Animation(
        int duration,
        string easeFunction
    )
    {
        this.duration = duration;
        this.easeFunction = easeFunction;

        easeFunctions = new Dictionary<string, Func<double, double>>
        {
            { "easeOutQuint", EaseOutQuint }
        };
    }

    public void Add(
        T animationObject,
        POINT2 start,
        POINT2 end
    )
    {
        (T, (POINT2, POINT2)) animationObjectAndBound = new();
        animationObjectAndBound.Item1 = animationObject;
        animationObjectAndBound.Item2.Item1 = start;
        animationObjectAndBound.Item2.Item2 = end;
        animationObjectsAndBounds.Add(animationObjectAndBound);
    }

    public async Task Play()
    {
        var fps = 60;
        var dt = 1000 / fps; // milliseconds
        var frames = (int)((float)duration / 1000 * fps);

        Stopwatch sw = new();
        sw.Start();

        for (var i = 0; i < frames; i++)
        {
            for (var j = 0; j < animationObjectsAndBounds.Count; j++)
            {
                var obj = animationObjectsAndBounds[j].Item1;
                var start = animationObjectsAndBounds[j].Item2.Item1;
                var end = animationObjectsAndBounds[j].Item2.Item2;

                obj.Move(
                    GetCoord(start.X, end.X, frames, i),
                    GetCoord(start.Y, end.Y, frames, i),
                    false
                );
            }

            var wait = (int)(i * dt - sw.ElapsedMilliseconds);
            wait = wait < 0 ? 0 : wait;
            await Task.Delay(wait);
        }

        sw.Stop();
    }

    private int? GetCoord(int? start, int? end, int frames, int frame)
    {
        if (start == null || end == null) return null;
        var progress = (double)frame / frames;
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