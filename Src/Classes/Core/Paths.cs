using System;
using System.Diagnostics;
using System.IO;

#nullable enable

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

    // the swda.dll is an optional file if you need the optional feature of protecting windows from screen recorders
    // we need to do dll injection because SetWindowDisplayAffinity is only effective on window owned by calling thread
    // and not arbitrary windows of arbitrary processes
    public static string swdaDll = Path.Join(rootDir, "swda.dll");

    public static string home =
        Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
        ?? "";

    public static void CreateIfAbsent()
    {
        if (!Directory.Exists(rootDir))
            Directory.CreateDirectory(rootDir);
    }
}
