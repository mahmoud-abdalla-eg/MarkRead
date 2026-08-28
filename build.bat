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

echo [1/2] Building MarkRead Desktop (.NET 8 WPF x64)...
dotnet publish "%~dp0MarkRead\MarkRead.csproj" -c Release -r win-x64 --self-contained false -o "%~dp0MarkRead\bin\Release\net8.0-windows\win-x64\publish"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Desktop build failed!
    pause
    exit /b 1
)

echo.
echo [2/2] Checking MarkRead Web dependencies...
where npm >nul 2>nul
if %ERRORLEVEL% equ 0 (
    if exist "%~dp0markread-web" (
        if not exist "%~dp0markread-web\node_modules" (
            echo Installing web npm dependencies...
            pushd "%~dp0markread-web"
            call npm install
            popd
        )
    )
) else (
    echo [NOTE] Node.js/npm not found. Skipping web dependency installation.
)

echo.
echo ========================================================
echo   Build Completed Successfully!
echo ========================================================
echo.
echo You can now run:
echo   - "Launch MarkRead.bat" (Desktop Viewer)
echo   - "Launch MarkRead Web.bat" (Web Viewer)
echo.
pause
