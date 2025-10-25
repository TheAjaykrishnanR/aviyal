clear
rm bin\aviyal.exe

$target = "exe"
if($args[0] -eq "winexe") {
	$target = "winexe" 
}

dflat Main.cs `
	  Classes\Core\Interfaces\IAviyal.cs `
	  Classes\Core\Interfaces\IJson.cs `
	  Classes\Core\Aviyal.cs `
	  Classes\Core\Config.cs `
	  Classes\Core\Globals.cs `
	  Classes\Core\Layouts.cs `
	  Classes\Core\Logger.cs `
	  Classes\Core\Paths.cs `
	  Classes\Core\Server.cs `
	  Classes\Core\State.cs `
	  Classes\Core\Utils.cs `
	  Classes\Hooks\Keys.cs `
	  Classes\Hooks\Mouse.cs `
	  Classes\Hooks\Windows.cs `
	  Classes\Win32\Delegates.cs `
	  Classes\Win32\Enums.cs `
	  Classes\Win32\Functions.cs `
	  Classes\Win32\Structs.cs `
	  /langversion:preview `
	  /target:$target `
	  /out aviyal.exe `

mv aviyal.exe bin\aviyal.exe
