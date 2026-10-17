using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.UI.Shell;
using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshalling]

namespace Com;

public class ComFunctions {

    public static void IShellDispatch2_ShellExecute(string cmdLine, string? folder = null) {

        PInvoke.CoCreateInstance(typeof(ShellWindows).GUID, null, CLSCTX.CLSCTX_ALL, out IShellWindows shellWindows);

        IDispatch? desktopDispatch = shellWindows?.FindWindowSW(
                ComVariant.Null,
                ComVariant.Null,
                ShellWindowTypeConstants.SWC_DESKTOP,
                out int _,
                ShellWindowFindWindowOptions.SWFO_NEEDDISPATCH);

        var sp = (Windows.Win32.System.Com.IServiceProvider?)desktopDispatch;
        if(sp == null) return;
        sp.QueryService(PInvoke.SID_STopLevelBrowser, out IShellBrowser shellBrowser);
        shellBrowser.QueryActiveShellView(out IShellView shellView);
        shellView.GetItemObject(_SVGIO.SVGIO_BACKGROUND, out IDispatch dispatch);

        var shellFolderView = (IShellFolderViewDual)dispatch;
        var application = shellFolderView.get_Application();
        var shellDispatch = (IShellDispatch2)application;

        List<string> words = cmdLine.Split(" ").ToList();
        string file = words.First();
        words.RemoveAt(0);
        string? args = string.Join(" ", words);

        shellDispatch.ShellExecute(
            new BSTR(Marshal.StringToBSTR(file)),
            args == null ? ComVariant.Null : ComVariant.Create(args),
            folder == null ? ComVariant.Null : ComVariant.Create(folder),
            ComVariant.Null,
            ComVariant.Null
        );
    }
}
