# Remove-Item -Recurse -Confirm:$false bin\*
rm bin\Main.exe

dflat Main.cs `
	  Interfaces\Core.cs `
	  Interfaces\Json.cs `
	  Classes\Core.cs `
	  Classes\Layouts.cs `
	  Classes\Win32\Functions.cs `
	  Classes\Win32\Structs.cs `
	  Classes\Win32\Delegates.cs `
	  Classes\Win32\Enums.cs `
	  Classes\Utils\Core.cs `
	  Classes\Utils\Extensions.cs `
	  Classes\Utils\Logger.cs `
	  Classes\Events\Keys.cs `
	  Classes\Events\Windows.cs `
	  Classes\Config\Config.cs `
	  Classes\Config\Paths.cs `
	  /r:Libs `
	  /langversion:preview `
	  /out Main.exe `

mv Main.exe bin\Main.exe
