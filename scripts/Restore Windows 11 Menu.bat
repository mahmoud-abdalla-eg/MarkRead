@echo off
setlocal
echo ================================================================
echo   Restore Modern Windows 11 Right-Click Menu
echo ================================================================
echo.
reg delete "HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}" /f
echo.
echo Restarting Windows Explorer...
taskkill /f /im explorer.exe
start explorer.exe
echo.
echo Modern Windows 11 menu restored.
pause
