using System;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Text.Json.Nodes;
using System.Collections.Generic;

public class Logger
{
	public static bool DEBUG = true;
	public static bool CONSOLE = true;
	public static bool FILE = true;

	public static void Log(string? text, Exception? ex = null, bool debug = true, bool console = true, bool file = true)
	{
		if (ex != null) text += $"\n{ex.Message}" + $"\n{ex.StackTrace}" + $"\n{ex?.InnerException?.StackTrace}";
		if (DEBUG && debug) Debug.WriteLine(text);
		if (CONSOLE && console) Console.WriteLine(text);
		if (FILE && file) LogToFile(text);
	}

	private static Lock @writeLock = new();
	public static void LogToFile(string? text)
	{
		lock (@writeLock)
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
