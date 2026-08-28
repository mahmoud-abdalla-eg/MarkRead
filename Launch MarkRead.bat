@echo off
setlocal
set "EXE=%~dp0MarkRead\bin\Release\net8.0-windows\win-x64\publish\MarkRead.exe"

if not exist "%EXE%" (
    set "EXE=%~dp0MarkRead\bin\Release\net8.0-windows\MarkRead.exe"
)
if not exist "%EXE%" (
    set "EXE=%~dp0MarkRead\bin\Debug\net8.0-windows\MarkRead.exe"
)
if not exist "%EXE%" (
    echo [INFO] MarkRead executable not found. Compiling with build.bat...
    call "%~dp0build.bat"
    set "EXE=%~dp0MarkRead\bin\Release\net8.0-windows\win-x64\publish\MarkRead.exe"
)

if exist "%EXE%" (
    start "" "%EXE%" %*
) else (
    echo [ERROR] Could not find or build MarkRead.exe.
    echo Please make sure .NET 8 SDK is installed.
    pause
)
