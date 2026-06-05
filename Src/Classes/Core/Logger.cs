using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows;

public class Logger
{
    public static string normal = "\x1b[39m";
    public static Func<int, int, int, string> f_col = (r, g, b) => $"\x1b[38;2;{r};{b};{g}m";
    public static Func<int, int, int, string> b_col = (r, g, b) => $"\x1b[48;2;{r};{b};{g}m";
    public static string alt_buffer = "\x1b[?1049h";
    public static string orig_buffer = "\x1b[?1049l";

    static string ColorText(string text, int r, int b, int g) => $"{f_col(r, b, g)}{text}{normal}";

    static FileInfo? logFileInfo;

    public static void Log(
        string text,
        LogType logType = LogType.INFO,
        Exception? ex = null,
        bool debug = true,
        bool console = true,
        bool file = true
    )
    {
        if (ex != null)
            logType = LogType.ERROR;

        string consoleText = logType switch
        {
            LogType.INFO =>
                $"[{ColorText(logType.ToString(), 255, 255, 255)}] {ColorText(text, 150, 150, 150)}",
            LogType.ERROR =>
                $"[{ColorText(logType.ToString(), 255, 0, 0)}] {ColorText(text, 255, 0, 0)}",
            LogType.MSG =>
                $"[{ColorText(logType.ToString(), 100, 100, 100)}] {ColorText(text, 150, 150, 150)}",
            LogType.EVENT =>
                $"[{ColorText(logType.ToString(), 20, 10, 150)}] {ColorText(text, 150, 150, 150)}",
            _ =>
                $"[{ColorText(logType.ToString(), 255, 255, 255)}] {ColorText(text, 150, 150, 150)}",
        };

        if (ex != null)
        {
            text += $"\n{ex.Message}\n{ex.StackTrace}";
            consoleText +=
                $"\n{ColorText(ex.Message, 255, 0, 0)}\n{ColorText(ex.StackTrace, 255, 50, 50)}";
        }
        if (debug)
            Debug.WriteLine(consoleText);
        if (console)
            Console.WriteLine(consoleText);
        if (file)
        {
            if (logFileInfo == null)
            {
                if (!File.Exists(Paths.logFile))
                    File.WriteAllText(Paths.logFile, null);
                logFileInfo = new(Paths.logFile);
            }
            if (logFileInfo?.Length > 1024 * 1024)
                File.WriteAllText(Paths.logFile, null);
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
            Log("unable to log to file", ex: ex, file: false);
        }
    }

    public static void Log<T>(List<T> array, string? prefix = null, string? suffix = null)
    {
        string text = $"{prefix} [";
        array.ForEach(item => text += $"{item?.ToString()}, ");
        text += $"] {suffix}";
        Log(text);
    }

    public static void Error(Exception ex, string? customMessage = null)
    {
        string text = $"\n{ex.Message}\n{ex.StackTrace}";
        Console.WriteLine($"{customMessage}: {text}");
        User32.MessageBox(0, text, customMessage ?? "Error", 0);
    }
}

public enum LogType
{
    INFO,
    ERROR,
    EVENT,
    MSG,
}
