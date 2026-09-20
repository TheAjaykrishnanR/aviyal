module swda;

import std.stdio;
import core.sys.windows.dll;
import core.sys.windows.windef;
import core.sys.windows.winuser;
import core.sys.windows.windows;

pragma(lib, "user32.lib");

mixin SimpleDllMain;

extern(Windows) {
    int SetWindowDisplayAffinity(void* hWnd, uint dwAffinity);
}

export extern(C) void __init() {
    stdout.setvbuf(0, _IONBF);
}

__gshared void*[] args;
export extern(C) void __set_arg(void* arg) {
    args ~= arg;
}

export extern(C) void __clear_args() {
    args = [];
}

export extern(C) void __main() {
    if(args.length < 2) return;
    SetWindowDisplayAffinity(args[0], cast(uint)args[1]);
}
