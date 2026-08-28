@echo off
setlocal
echo ===================================================
echo   MarkRead - Windows Explorer & Taskbar Setup
echo ===================================================
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

echo Registering MarkRead in Windows Explorer...
"%EXE_PATH%" --register

echo.
echo ===================================================
echo   Registration Complete!
echo ===================================================
echo.
echo * File Context Menu: Right-click any .md file to see "Open with MarkRead"
echo * Directory Context Menu: Right-click any folder or empty space to see "Open with MarkRead"
echo * A "MarkRead" shortcut has been created on your Desktop
echo * To pin to Taskbar: Right-click the Desktop shortcut and click "Pin to taskbar"
echo.
pause
