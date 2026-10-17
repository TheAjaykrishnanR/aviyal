# What this is...

Com is fricking hard since we cant even use the `[ComImport]` (built-in Com) so we cheat a little by utilizing ComWrappers Source generation provided by `Microsoft.Interop.ComInterfaceGenerator`. CsWin32 will generate the biolerplate ComWrapper implementation for all Com interfaces annotated with the `[GeneratedComInterface]` attribute, however we dont even want to write the interfaces ourselves either as CsWin32 can already does that anyway.

- `Microsoft.Windows.CsWin32` running as an msbuild task and not an analyzer (inside the compiler) emits interfaces and classes decorated with `[GeneratedComInterface]` and `[GeneratedComClass]`
- `Microsoft.Interop.ComInterfaceGenerator` implements the ComWrappers

Runtime Marshalling has to be disabled because otherwise the CLR's built in marshaller would convert com objects itself instead of using our com interface, and disabling runtime marshalling applies across the entire assembly and therefore this tiny little dll project.
