/*
	MIT License
    Copyright (c) 2025 Ajaykrishnan R	
*/

using System;
using System.IO;
using System.Diagnostics;

public class Paths
{
	public static string rootDir = Path.GetDirectoryName(Environment.ProcessPath)!;
	public static string configFile = Path.Join(rootDir, "aviyal.json");
	public static string stateFile = Path.Join(rootDir, "state.json");
	public static string errorFile = Path.Join(rootDir, "error.log");
}
