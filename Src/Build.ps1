clear
rm bin\aviyal.exe, bin\swda.dll

mkdir bin

$target = "exe"
if($args[0] -eq "winexe") {
	$target = "winexe" 
}

echo "Building aviyal..."
dflat Main.cs `
	  Classes\Core\Interfaces\IAviyal.cs `
	  Classes\Core\Interfaces\IJson.cs `
	  Classes\Core\Interfaces\IAnimation.cs `
	  Classes\Core\Aviyal.cs `
	  Classes\Core\Config.cs `
	  Classes\Core\Globals.cs `
	  Classes\Core\Injector.cs `
	  Classes\Core\Layouts.cs `
	  Classes\Core\Logger.cs `
	  Classes\Core\Paths.cs `
	  Classes\Core\Server.cs `
	  Classes\Core\State.cs `
	  Classes\Core\Utils.cs `
	  Classes\Core\Animation.cs `
	  Classes\Hooks\Keys.cs `
	  Classes\Hooks\Mouse.cs `
	  Classes\Hooks\Windows.cs `
	  Classes\Win32\Delegates.cs `
	  Classes\Win32\Enums.cs `
	  Classes\Win32\Functions.cs `
	  Classes\Win32\Structs.cs `
	  /target:$target `
	  /out:bin\aviyal.exe `

echo "Building swda.dll"
dmd -shared swda.d
rm swda.exp, swda.lib, swda.obj
mv swda.dll bin\swda.dll
