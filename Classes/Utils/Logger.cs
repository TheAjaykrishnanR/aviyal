/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
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

	public static void Log(List<string> array)
	{
		foreach (var arr in array) Log(arr);
	}
}

public class WindowManagerState : IJson<WindowManagerState>
{
	public List<Window> windows = new();

	public string ToJson()
	{
		JsonObject j = new()
		{
			["windows"] = new JsonArray(
				windows.Select(wnd =>
				{
					return new JsonObject()
					{
						["hWnd"] = wnd.hWnd.ToString(),
						["title"] = wnd.title,
						["exe"] = wnd.exe,
					};
				}).ToArray()
			),
		};
		return j.ToString();
	}

	public WindowManagerState FromJson(string json)
	{
		JsonNode? node = JsonNode.Parse(json);
		WindowManagerState state = new();
		JsonArray? _arr = node?["windows"]?.AsArray();
		_arr?.ToList().ForEach(
			_wnd =>
			{
				nint hWnd = (nint)Convert.ToInt32(_wnd?["hWnd"]?.ToString());
				state.windows.Add(new Window(hWnd));
			}
		);
		return state;
	}
}
