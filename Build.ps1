Remove-Item -Recurse -Confirm:$false bin\*

dflat Main.cs `
	  Classes\Win32\Functions.cs `
	  Classes\Win32\Structs.cs `
	  Classes\Win32\Delegates.cs `
	  Classes\Win32\Enums.cs `
	  Classes\Utils\Core.cs `
	  Classes\Utils\Extensions.cs `
	  Interfaces\Core.cs `
	  /langversion:preview `
	  /out Main.exe

mv Main.exe bin\Main.exe
