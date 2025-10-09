using System;
using System.IO;
using System.Diagnostics;

public class Paths
{
	public static string configFile = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "aviyal.json");
}
