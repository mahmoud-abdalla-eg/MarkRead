@echo off
setlocal
echo ==========================================================
echo   MarkRead - Make Default Viewer for Markdown (.md)
echo ==========================================================
echo.

set "EXE_PATH=%~dp0..\MarkRead\bin\Release\net8.0-windows\win-x64\publish\MarkRead.exe"

if not exist "%EXE_PATH%" (
    set "EXE_PATH=%~dp0..\MarkRead\bin\Release\net8.0-windows\MarkRead.exe"
)
if not exist "%EXE_PATH%" (
    set "EXE_PATH=%~dp0..\MarkRead\bin\Debug\net8.0-windows\MarkRead.exe"
)
if not exist "%EXE_PATH%" (
    echo [INFO] MarkRead.exe not found. Building project...
    call "%~dp0..\build.bat"
    set "EXE_PATH=%~dp0..\MarkRead\bin\Release\net8.0-windows\win-x64\publish\MarkRead.exe"
)

if not exist "%EXE_PATH%" (
    echo [ERROR] MarkRead.exe could not be found or built!
    echo Please make sure .NET 8 SDK is installed.
    pause
    exit /b 1
)

echo Launching Windows default app picker...
echo Select "MarkRead" and click "Always" to make it default.
echo.
"%EXE_PATH%" --default
