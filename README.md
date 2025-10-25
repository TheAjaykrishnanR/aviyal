# Aviyal

> Window manager for windows written purely in C# thats simple, lightweight and portable.

[<video src="https://github.com/TheAjaykrishnanR/aviyal/blob/master/Imgs/output.mp4"></video>](https://github.com/user-attachments/assets/82e910b9-878b-458e-9562-6700579b199f)

## Features

1. Workspaces
2. Workspace animations (Horizontal and vertical)
3. Dynamic Tiling : `Dwindle`
4. Toggle floating
5. Close focused window
6. Shift focus 
7. Configuration using json
8. Hot reloading
9. Qerry state using websocket and execute commands
10. Launch apps using hotkeys

## Usage

Download the latest release from [releases](https://github.com/TheAjaykrishnanR/aviyal/releases) and run it. 
For live debug output, run from a terminal (`cmd.exe` or `pwsh.exe`).

## Configuration

Configuration file `aviyal.json` will be created at first run. You can modify the default settings there,
including adding new keybindings etc. Look at the example config file [here](https://github.com/TheAjaykrishnanR/aviyal/blob/master/Src/aviyal.json)
## Default keybindings

- `FOCUS NEXT WORKSPACE`: `LCONTROL, LSHIFT, L`
- `FOCUS PREVIOUS WORKSPACE`: `LCONTROL, LSHIFT, H`
- `FOCUS WINDOW RIGHT`: `LCONTROL, L`
- `FOCUS WINDOW LEFT`: `LCONTROL, H`
- `FOCUS WINDOW TOP`: `LCONTROL, K`
- `FOCUS WINDOW BOTTOM`: `LCONTROL, J`
- `SHIFT WINDOW NEXT WORKSPACE`: `LMENU (ALT), LSHIFT, L`
- `SHIFT WINDOW PREVIOUS WORKSPACE`: `LMENU (ALT), LSHIFT, H`
- `SWAP WINDOW RIGHT`: `LMENU (ALT), L`
- `SWAP WINDOW LEFT`: `LMENU (ALT), H`
- `RESTART APPLICATION`: `LCONTROL, LSHIFT, R` (hot reload for config)
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

## Contributing

PRs welcome !
