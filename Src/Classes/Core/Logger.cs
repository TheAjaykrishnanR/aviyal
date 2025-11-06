using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class Logger
{
    private static FileInfo? logFileInfo;

    public static void Log(
        string? text,
        Exception? ex = null,
        bool debug = true, bool console = true, bool file = true
    )
    {
        if (ex != null) text += $"\n{ex.Message}" + $"\n{ex.StackTrace}" + $"\n{ex?.InnerException?.StackTrace}";
        if (debug) Debug.WriteLine(text);
        if (console) Console.WriteLine(text);
        if (file)
        {
            if (logFileInfo == null)
            {
                if (!File.Exists(Paths.logFile)) File.WriteAllText(Paths.logFile, null);
                logFileInfo = new FileInfo(Paths.logFile);
            }

            if (logFileInfo?.Length > 1024 * 1024) File.WriteAllText(Paths.logFile, null);
            LogToFile(text);
        }
    }

    public static void LogToFile(string? text)
    {
        try
        {
            File.AppendAllText(Paths.logFile, $"{text}\n");
        }
        catch (Exception ex)
        {
            Log("unable to log to file", ex, file: false);
        }
    }

    public static void Log<T>(List<T> array, string? prefix = null, string? suffix = null)
    {
        var text = $"{prefix} [";
        array.ForEach(item => text += $"{item?.ToString()}, ");
        text += $"] {suffix}";
        Log(text);
    }

    public static void Error(Exception ex, string? customMessage = null)
    {
        var text = $"\n{ex.Message}\n{ex.StackTrace}";
        Console.WriteLine($"{customMessage}: {text}");
        User32.MessageBox(0, text, customMessage ?? "Error", 0);
    }
}