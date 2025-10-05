Remove-Item -Recurse -Confirm:$false bin\*

dflat Main.cs `
	  Interfaces\Core.cs `
	  Classes\Core.cs `
	  Classes\Layouts.cs `
	  Classes\Win32\Functions.cs `
	  Classes\Win32\Structs.cs `
	  Classes\Win32\Delegates.cs `
	  Classes\Win32\Enums.cs `
	  Classes\Utils\Core.cs `
	  Classes\Utils\Extensions.cs `
	  Classes\Events\Keys.cs `
	  Classes\Events\Windows.cs `
	  /langversion:preview `
	  /out Main.exe

mv Main.exe bin\Main.exe
