# Aviyal

> Window manager for windows written purely in C# thats simple, lightweight and portable.

## Usage

Download the latest release from [releases](https://github.com/TheAjaykrishnanR/aviyal/releases) and run it. 
For live debug output, run from a terminal (`cmd.exe` or `pwsh.exe`).

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

By default `9` workspaces are initialized.

## Configuration

Configuration file `aviyal.json` will be created at first run. You can modify the default settings there,
including adding new keybindings etc

## Building

Aviyal is built using a custom C# Aot compiler called as [dflat](https://github.com/TheAjaykrishnanR/dflat)
If you have `dflat` in path, building is as simple as:

```
git clone https://github.com/TheAjaykrishnanR/aviyal
cd aviyal
./Build.ps1
```

You will find the aot compiled executable at `bin\aviyal.exe`

For development ease, such as LSP a dotnet `csproj` file is also provided which allows language
support in neovim by roslyn. This allows you to build aviyal just like any other dotnet application.

If thats what you prefer, building it is as simple as

```
git clone https://github.com/TheAjaykrishnanR/aviyal
cd aviyal
dotnet build
```

You can find the executable at `bin\Debug\net*\win-x64`

## Contributing

PRs welcome !

## License

This project is free to use, modify and distribute according to the MIT License.
