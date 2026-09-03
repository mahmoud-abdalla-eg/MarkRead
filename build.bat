@echo off
setlocal
echo ========================================================
echo               MarkRead - Build Pipeline
echo ========================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK is not found on your system PATH!
    echo Please install the .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo Building MarkRead Desktop (.NET 8 WPF x64)...
dotnet publish "%~dp0MarkRead\MarkRead.csproj" -c Release -r win-x64 --self-contained false -o "%~dp0MarkRead\bin\Release\net8.0-windows\win-x64\publish"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Desktop build failed!
    pause
    exit /b 1
)

echo.
echo ========================================================
echo   Build Completed Successfully!
echo ========================================================
echo.
echo You can now run:
echo   - "Launch MarkRead.bat" (Desktop Viewer)
echo.
pause
