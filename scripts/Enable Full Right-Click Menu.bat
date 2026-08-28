@echo off
setlocal
echo ================================================================
echo   Enable Direct Right-Click Menu (Classic Windows 10/11 Menu)
echo ================================================================
echo.
echo This restores the full right-click menu in Windows 11, so that
echo "Open with MarkRead" appears DIRECTLY on the first right-click
echo without having to click "Show more options" or press Shift.
echo.
pause

reg add "HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32" /f /ve
echo.
echo Restarting Windows Explorer to apply changes...
taskkill /f /im explorer.exe
start explorer.exe
echo.
echo Done! Now right-click any .md file and "Open with MarkRead" is right there!
pause
