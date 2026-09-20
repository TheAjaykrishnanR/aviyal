using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static System.Console;

#nullable enable

public interface IArg
{
    public nint Ptr { get; }
    public int Length { get; }
    public void Free();
}

public class StringArg(string text) : IArg
{
    readonly nint _ptr = Marshal.StringToHGlobalAnsi(text);
    public nint Ptr
    {
        get => _ptr;
    }
    public int Length
    {
        get => text.Length;
    }

    public void Free() => Marshal.FreeHGlobal(_ptr);
}

public struct REMOTE_THREAD_RESULT
{
    public bool success;
    public uint exitCode;
    public int exitReason;
    public uint threadId;
}

public class RemoteProcess : IDisposable
{
    nint hProcess;

    public RemoteProcess(int pid)
    {
        const uint PROCESS_ALL_ACCESS = 0x1FFFFF;
        hProcess = Kernel32.OpenProcess(PROCESS_ALL_ACCESS, false, pid);
    }

    public void Dispose()
    {
        Kernel32.CloseHandle(hProcess);
    }

    /// <summary>
    /// Gets the Relative Virtual Adress (relative to base) of a procedure inside a module
    /// A side effect is that module gets temporarirly loaded into the host process
    /// </summary>
    public nint GetRVA(string fnName, string modulePath)
    {
        // we cannot avoid loading the module since GetProcAddress requires an address to module base
        // that is inside our process and not the target
        nint moduleBase = Kernel32.LoadLibraryA(modulePath);
        nint rva = Kernel32.GetProcAddress(moduleBase, fnName) - moduleBase;
        Kernel32.FreeLibrary(moduleBase);
        return rva;
    }

    /// <summary>
    /// Enumerates all modules inside a process and pair them with their names and base addresses
    /// </summary>
    public unsafe Dictionary<string, nint>? GetModules()
    {
        int initArrayLength = 1024;
        nint* modules = stackalloc nint[initArrayLength];
        int cb = initArrayLength * sizeof(nint);
        uint sizeNeeded = 0;
        uint LIST_MODULES_ALL = 0x03;
        int hresult = Psapi.EnumProcessModulesEx(
            hProcess,
            (nint)modules,
            cb,
            (nint)(&sizeNeeded),
            LIST_MODULES_ALL
        );
        if (hresult > 0)
        {
            Dictionary<string, nint> dlls = new();
            for (int i = 0; i < sizeNeeded / sizeof(nint); i++)
            {
                StringBuilder str = new(1024);
                if (Psapi.GetModuleFileNameEx(hProcess, modules[i], str, (uint)str.Capacity) > 0)
                    dlls[new FileInfo(str.ToString()).Name.ToLower()] = modules[i];
            }
            return dlls;
        }
        return null;
    }

    const uint MEM_RESERVE = 0x00002000;
    const uint MEM_COMMIT = 0x00001000;
    const uint PAGE_READWRITE = 0x04;

    public nint? Write(nint dataPtr, nint dataLength)
    {
        // destination in the target process where the data is to be written
        nint destination = Kernel32.VirtualAllocEx(
            hProcess,
            0,
            (nuint)dataLength,
            MEM_COMMIT | MEM_RESERVE,
            PAGE_READWRITE
        );
        if (
            Kernel32.WriteProcessMemory(
                hProcess,
                destination,
                dataPtr,
                (nuint)dataLength,
                out int written
            ) == 0
        )
            return null;

        if (written != dataLength)
            return null;

        return destination;
    }

    /// <summary>
    /// <param name="fnPtr">pointer to the function that lives in the process</param>
    /// <param name="args">arguments to this function wrapped as a struct</param>
    /// </summary>
    public REMOTE_THREAD_RESULT Call(nint fnPtr, nint? argsPtr = null)
    {
        nint remoteThread;
        if (
            (
                remoteThread = Kernel32.CreateRemoteThread(
                    hProcess,
                    0,
                    0,
                    fnPtr,
                    argsPtr ?? 0,
                    0,
                    out uint threadId
                )
            ) == 0
        )
            return new REMOTE_THREAD_RESULT { success = false };
        const int WAIT_DURATION = 5000;
        int exitReason = Kernel32.WaitForSingleObject(remoteThread, WAIT_DURATION);
        Kernel32.GetExitCodeThread(remoteThread, out uint exitCode);
        return new REMOTE_THREAD_RESULT
        {
            success = true,
            threadId = threadId,
            exitCode = exitCode,
            exitReason = exitReason,
        };
    }
}

class Injector : IDisposable
{
    string moduleName;
    string modulePath;
    nint moduleBase;
    RemoteProcess rp;

    /// <summary>
    /// A class for injecting dlls into other process.
    /// <param name="moduleName">the name of the module (dll) to be injected to a process</param>
    /// <param name="pid">the pid of the target process into which the module is to be injected</param>
    /// </summary>
    private Injector(string modulePath, int pid)
    {
        this.modulePath = modulePath;
        moduleName = new FileInfo(modulePath).Name;
        Logger.Log($"Initializing injector with moduleName={moduleName}");
        rp = new(pid);
    }

    public void Dispose()
    {
        rp.Dispose();
    }

    const string MODULE_EXPORTED_INIT = "__init";
    const string MODULE_EXPORTED_SET_ARG = "__set_arg";
    const string MODULE_EXPORTED_CLEAR_ARGS = "__clear_args";
    const string MODULE_EXPORTED_MAIN = "__main";
    nint MODULE_EXPORTED_INIT_RVA; // RVA to __init()
    nint MODULE_EXPORTED_SET_ARG_RVA; // RVA to __set_arg()
    nint MODULE_EXPORTED_CLEAR_ARGS_RVA; // RVA to __clear_args()
    nint MODULE_EXPORTED_MAIN_RVA; // RVA to __main()

    public static Injector? New(string moduleName, int pid)
    {
        Injector injector = new(moduleName, pid);

        // check if MODULE_EXPORTED_INIT exists in module
        if (
            (
                injector.MODULE_EXPORTED_INIT_RVA = injector.rp.GetRVA(
                    MODULE_EXPORTED_INIT,
                    injector.modulePath
                )
            ) <= 0
        )
        {
            Logger.Log($"Export func: {MODULE_EXPORTED_INIT}() not present in {moduleName}");
            return null;
        }

        // check if MODULE_EXPORTED_SET_ARG exists in module
        if (
            (
                injector.MODULE_EXPORTED_SET_ARG_RVA = injector.rp.GetRVA(
                    MODULE_EXPORTED_SET_ARG,
                    injector.modulePath
                )
            ) <= 0
        )
        {
            Logger.Log($"Export func: {MODULE_EXPORTED_SET_ARG}() not present in {moduleName}");
            return null;
        }

        // check if MODULE_EXPORTED_CLEAR_ARGS exists in module
        if (
            (
                injector.MODULE_EXPORTED_CLEAR_ARGS_RVA = injector.rp.GetRVA(
                    MODULE_EXPORTED_CLEAR_ARGS,
                    injector.modulePath
                )
            ) <= 0
        )
        {
            Logger.Log($"Export func: {MODULE_EXPORTED_CLEAR_ARGS}() not present in {moduleName}");
            return null;
        }

        // check if MODULE_EXPORTED_MAIN exists in module
        if (
            (
                injector.MODULE_EXPORTED_MAIN_RVA = injector.rp.GetRVA(
                    MODULE_EXPORTED_MAIN,
                    injector.modulePath
                )
            ) <= 0
        )
        {
            Logger.Log($"Export func: {MODULE_EXPORTED_MAIN}() not present in {moduleName}");
            return null;
        }

        return injector;
    }

    public nint kernel32base
    {
        get
        {
            if (field == 0)
                rp.GetModules()?.TryGetValue("kernel32.dll", out field);
            return field;
        }
    }

    /// <summary>
    /// Injects a module into a process
    /// </summary>
    public unsafe bool Inject()
    {
        // call kernel32!LoadLibrary()
        nint fnRVA = rp.GetRVA("LoadLibraryA", "kernel32.dll");

        nint fnPtr = kernel32base + fnRVA;
        StringArg arg = new(modulePath);
        nint? argsPtr = rp.Write(arg.Ptr, arg.Length);
        arg.Free();

        if (argsPtr == null || argsPtr == 0)
        {
            Logger.Log("argsPtr = null");
            return false;
        }

        var result = rp.Call(fnPtr, argsPtr);

        Logger.Log($"Inject(), fnPtr: {fnPtr}, argsPtr: {argsPtr}");
        Logger.Log(
            $"Inject(), success: {result.success}, exitCode: {result.exitCode}, exitReason: {result.exitReason}"
        );

        // set moduleBase
        if (!rp.GetModules()?.TryGetValue(moduleName, out moduleBase) ?? false)
            return false;
        return true;
    }

    /// <summary>
    /// Unloads the modul from the process
    /// </summary>
    public bool Unload()
    {
        // call kernel32!FreeLibrary
        nint fnRVA = rp.GetRVA("FreeLibrary", "kernel32.dll");

        nint fnPtr = kernel32base + fnRVA;
        var result = rp.Call(fnPtr, moduleBase);

        Logger.Log($"Unload(), fnPtr: {fnPtr}, argsPtr: {moduleBase}");
        Logger.Log(
            $"Unload(), success: {result.success}, exitCode: {result.exitCode}, exitReason: {result.exitReason}"
        );

        if (rp.GetModules()?.TryGetValue(moduleName, out moduleBase) ?? false)
            return false;
        return true;
    }

    public void CallInit()
    {
        if (moduleBase == 0)
            return;
        nint fnPtr = moduleBase + MODULE_EXPORTED_INIT_RVA;
        rp.Call(fnPtr);
    }

    public void CallSetArg(nint num)
    {
        if (moduleBase == 0)
            return;
        nint fnPtr = moduleBase + MODULE_EXPORTED_SET_ARG_RVA;
        rp.Call(fnPtr, num);
    }

    public void CallSetArg(IArg arg)
    {
        if (moduleBase == 0)
            return;
        nint fnPtr = moduleBase + MODULE_EXPORTED_SET_ARG_RVA;
        nint? argPtr = rp.Write(arg.Ptr, arg.Length);
        arg.Free();
        rp.Call(fnPtr, argPtr);
    }

    public void CallClearArgs()
    {
        if (moduleBase == 0)
            return;
        nint fnPtr = moduleBase + MODULE_EXPORTED_CLEAR_ARGS_RVA;
        rp.Call(fnPtr);
    }

    public void CallMain()
    {
        if (moduleBase == 0)
            return;
        nint fnPtr = moduleBase + MODULE_EXPORTED_MAIN_RVA;
        rp.Call(fnPtr);
    }

    // public static void Main(string[] args) {
    //     int pid = Convert.ToInt32(args[0]);
    //     Injector? injector = New("mydll.dll", pid);
    //     if(!injector?.Inject() ?? false) {
    //         Logger.Log("Injector failed");
    //         return;
    //     }

    //     // call injected dll's exported functions
    //     injector?.CallInit();
    //     injector?.CallSetArg(new StringArg("hello"));
    //     injector?.CallSetArg(new StringArg("world"));
    //     injector?.CallSetArg(69);
    //     injector?.CallMain();

    //     // unload
    //     Logger.Log("Unloading...");
    //     injector?.Unload();
    // }
}
