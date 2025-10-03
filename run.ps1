Remove-Item -Recurse -Confirm:$false bin\*

dflat Main.cs `
	  Classes\Win32\Functions.cs `
	  Classes\Win32\Structs.cs `
	  Classes\Win32\Delegates.cs `
	  Classes\Win32\Enums.cs `
	  /out Main.exe

mv Main.exe bin\Main.exe
rm Main.exe

