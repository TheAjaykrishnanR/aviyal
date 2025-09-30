#rm  _Main.cs
#rm Main.exe
#
#cat Main.cs >> _Main.cs
#cat Classes\Win32\Functions.cs >> _Main.cs
#cat Classes\Win32\Structs.cs >> _Main.cs
#cat Classes\Win32\Delegates.cs >> _Main.cs
#cat Classes\Win32\Enums.cs >> _Main.cs
#
#dflat _Main.cs /out Main.exe

dflat Main.cs 

