@echo off
set CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC_PATH%" /target:winexe /out:LaunchScrcpy.exe /win32icon:AppIcon.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.FileSystem.dll LaunchScrcpy.cs
if %errorlevel% neq 0 (
    echo Compilation failed!
    pause
) else (
    echo Compilation successful! LaunchScrcpy.exe created.
)
