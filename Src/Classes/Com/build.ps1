rm bin\com.dll

dflat Com.cs `
    <# Com #> `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.System.Com.IDispatch.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.System.Com.ISequentialStream.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.System.Com.IServiceProvider.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.System.Com.IStream.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.System.Ole.IOleWindow.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.Folder.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.FolderItem.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.FolderItems.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.FolderItemVerb.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.FolderItemVerbs.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.IShellBrowser.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.IShellDispatch.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.IShellDispatch2.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.IShellFolderViewDual.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.IShellView.cs `
    Generated\Microsoft.Interop.ComInterfaceGenerator\Windows.Win32.UI.Shell.IShellWindows.cs `
    <# LibraryImports #> `
    Generated\Microsoft.Interop.LibraryImportGenerator\LibraryImports.g.cs `
    <# CsWin32 #> `
    Generated\Microsoft.Windows.CsWin32\Windows.Win32.NativeMethods.g.cs `
    /target:lib `
    /out:"bin\com.dll"
