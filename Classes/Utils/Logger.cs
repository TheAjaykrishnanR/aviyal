/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System.Diagnostics;
using System.Windows;
using System.IO;

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

	public static void Log(List<string> array)
	{
		foreach (var arr in array) Log(arr);
	}
}
