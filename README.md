# Aviyal

![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/TheAjaykrishnanR/aviyal/total?color=green)

> Window manager for windows written purely in C# thats simple, lightweight and portable.

This project was **NOT** vibecoded.

![showcase_1](https://github.com/TheAjaykrishnanR/aviyal/blob/master/Imgs/showcase.png)

## Features

1. Workspaces
2. Workspace animations (Horizontal and vertical)
3. Dynamic Tiling : `Dwindle`, `Stack`, `Master`
4. Toggle floating
5. Close focused window
6. Shift focus 
7. Configuration using json
8. Hot reloading
9. Qerry state using websocket and execute commands
10. Launch apps using hotkeys
11. Move windows without the titlebar
12. Protect windows from screencapture *(optional)*

> Protecting windows from screencapture is a feature that allows you to hide windows from any screen recording software such as obs, the builtin snipping tool, etc. However to achieve this it requires us to call a win32 function `SetWindowDisplayAffinity()` from the context of the remote process of the target window to hide. This requires us to inject a dll into the window's host process, call our special function and then unload. So if you need this functionality please ensure you have the accompanying `swda.dll` alongside aviyal.

## TODO
- [ ] scrolling workspaces
- [ ] custom window sizes in layouts

## Usage

Download the latest release from [releases](https://github.com/TheAjaykrishnanR/aviyal/releases) and run it.

```
Aviyal is a window manager that dynamically tiles your windows, organizes them inside workspaces, allows navigation through keybindings, and more :)

ver: 0.2.8

aviyal: https://github.com/TheAjaykrishnanR/aviyal
dflat: https://github.com/TheAjaykrishnanR/dflat

USAGE: aviyal <options> <arguments>

available options:

--help, -h      :   prints this help text.
--debug, -d     :   flag for running the program in debug mode. Only special windows are tiled.
--query, -q     :   execute a query string
--version, -v   :   prints the version.
--restore, -r   :   restores windows from a previous state. Useful when crashed and windows are hidden.
--changelog     :   prints the changes in the current version.
```

## Configuration

Configuration file `aviyal.json` will be created at first run. You can modify the default settings there,
including adding new keybindings etc. Look at the example config file [here](https://github.com/TheAjaykrishnanR/aviyal/blob/master/Src/aviyal.json)

A quick summary of the values available for each key in the config is [here](https://github.com/TheAjaykrishnanR/aviyal/blob/master/Docs/Config.md)
## Default keybindings

- `FOCUS NEXT WORKSPACE`: `LCONTROL, LSHIFT, L`
- `FOCUS PREVIOUS WORKSPACE`: `LCONTROL, LSHIFT, H`
- `FOCUS WORKSPACE NUMX`: `LMENU (ALT), NUMX`
- `FOCUS WINDOW RIGHT`: `LCONTROL, L`
- `FOCUS WINDOW LEFT`: `LCONTROL, H`
- `FOCUS WINDOW TOP`: `LCONTROL, K`
- `FOCUS WINDOW BOTTOM`: `LCONTROL, J`
- `SHIFT WINDOW NEXT WORKSPACE`: `LMENU (ALT), LSHIFT, L`
- `SHIFT WINDOW PREVIOUS WORKSPACE`: `LMENU (ALT), LSHIFT, H`
- `SHIFT WINDOW WORKSPACE NUMX`: `LMENU (ALT), LSHIFT, NUMX`
- `TOGGLE WINDOW FLOATING`: `LCONTROL, LSHIFT, Z`
- `TOGGLE WINDOW STACKED`: `LCONTROL, LSHIFT, S`
- `TOGGLE WINDOW PROTECTION`: `LCONTROL, LMENU, P` *(optional)*
- `SWAP WINDOW RIGHT`: `LMENU (ALT), L`
- `SWAP WINDOW LEFT`: `LMENU (ALT), H`
- `DRAG WINDOW WITH MOUSE`: `LCONTROL, SPACE`
- `RESTART APPLICATION`: `LCONTROL, LSHIFT, R` *(hot reload for config)*
- `REFRESH TILING`: `LCONTROL, LSHIFT, U`

By default `9` workspaces are initialized.

## Building

Aviyal is built using a custom C# Aot compiler called as [dflat](https://github.com/TheAjaykrishnanR/dflat)
If you have `dflat` in path, building is as simple as:

```
git clone https://github.com/TheAjaykrishnanR/aviyal
cd aviyal/Src
./Build.ps1
```

You will find the aot compiled executable at `bin\aviyal.exe`

For development ease, such as LSP a dotnet `csproj` file is also provided which allows language
support in neovim by roslyn. This allows you to build aviyal just like any other dotnet application.

If thats what you prefer, build it as:
```
git clone https://github.com/TheAjaykrishnanR/aviyal
cd aviyal/Src
dotnet build
```

You can find the executable at `bin\Debug\net*\win-x64`

### swda.dll

To build `swda.d` you need [dmd](https://dlang.org/download.html), which is the compiler for the D programming language.

The build script will try to build it by default but if you need to do it manually, you could do:
```
dmd -shared swda.d
```
> The reason for using dlang was twofold: 1) D is **awesome** and is almost like the little (*or big ?*) brother of C# and 2) C# doesn't support native dll [unloading](https://github.com/dotnet/corert/pull/7011). As a result the injected dll will just sit there in the remote process's memory with its relatively bulkier runtime for no real reasons.

## Contributing

PRs welcome !
