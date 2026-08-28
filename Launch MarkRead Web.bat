@echo off
setlocal
echo ===================================================
echo       Launching MarkRead Web Application...
echo ===================================================
echo.

cd /d "%~dp0markread-web"

if not exist "node_modules" (
    echo [INFO] Installing required web dependencies...
    call npm install
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Failed to install npm packages.
        pause
        exit /b 1
    )
)

echo Starting local development server...
start http://localhost:5173
npm run dev
