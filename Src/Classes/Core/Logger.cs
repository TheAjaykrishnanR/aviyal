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
	}

	private static Lock @writeLock = new();
	public static void LogToFile(string text)
	{
		lock (@writeLock) File.AppendAllText(Paths.logFile, $"{text}\n");
	}

	public static void Log(List<string> array)
	{
		foreach (var arr in array) Log(arr);
	}

	public static void Error(Exception ex, string? customMessage = null)
	{
		string text = $"\n{ex.Message}\n{ex.StackTrace}";
		Console.WriteLine($"{customMessage}: {text}");
		User32.MessageBox(0, text, customMessage ?? "Error", 0);
	}
}