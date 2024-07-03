# SunOS porting (illumos and maybe solaris)

Building CLR, libs, packs is working:
```
export ROOTFS_DIR=/crossrootfs/x64
./build.sh clr+libs+packs -c Debug -gcc --keepnativesymbols true \
 -cross -os illumos "$@"
```

Starting on libraries.  Will need:
```
src/libraries/System.Diagnostics.Process/src/System.Diagnostics.Process.csproj
src/libraries/System.IO.FileSystem.Watcher/src/System.IO.FileSystem.Watcher.csproj
src/libraries/System.Net.Security/src/System.Net.Security.csproj
```

First up System.Diagnostics.Process

What I've done is copy things ... and start porting.

Copied to ... from: ../FreeBSD
src/libraries/Common/src/Interop/SunOS/
  Interop.Libraries.cs
  Interop.Process.GetProcInfo.cs
  Interop.Process.cs

Copied to ... from: ../BSD/System.Native/Interop.Sysctl.cs
src/libraries/Common/src/Interop/SunOS/
  System.Native/Interop.Kstat.cs

This was already here:
src/libraries/Common/src/Interop/SunOS/procfs/Interop.ProcFsStat.TryReadProcessStatusInfo.cs

Copied to ... from: *FreeBSD.cs
src/libraries/System.Diagnostics.Process/src/System/Diagnostics/*SunOS.cs

And add that all to the *.csproj file
src/libraries/System.Diagnostics.Process/src/System.Diagnostics.Process.csproj
