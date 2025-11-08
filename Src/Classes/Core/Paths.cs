using System;
using System.Diagnostics;
using System.IO;

public class Paths
{
    public static string rootDir = Path.GetDirectoryName(Environment.ProcessPath)!
        .Contains("Program Files")
        ? Path.Join(
            Path.GetDirectoryName(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            )!,
            "aviyal"
        )
        : Path.GetDirectoryName(Environment.ProcessPath)!;
    public static string configFile = Path.Join(rootDir, "aviyal.json");
    public static string stateFile = Path.Join(rootDir, "state.json");
    public static string logFile = Path.Join(rootDir, "aviyal.log");

    public static void CreateIfAbsent()
    {
        if (!Directory.Exists(rootDir))
            Directory.CreateDirectory(rootDir);
    }
}
