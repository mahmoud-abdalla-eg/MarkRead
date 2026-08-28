@echo off
setlocal
echo ===================================================
echo   MarkRead - Remove Windows Explorer Integration
echo ===================================================
echo.

reg delete "HKCU\Software\Classes\Applications\MarkRead.exe" /f >nul 2>&1
reg delete "HKCU\Software\Classes\SystemFileAssociations\.md\shell\Open with MarkRead" /f >nul 2>&1
reg delete "HKCU\Software\Classes\SystemFileAssociations\.markdown\shell\Open with MarkRead" /f >nul 2>&1
reg delete "HKCU\Software\Classes\MarkRead.Document" /f >nul 2>&1

reg delete "HKCU\Software\Classes\.md\OpenWithProgids" /v "MarkRead.Document" /f >nul 2>&1
reg delete "HKCU\Software\Classes\.markdown\OpenWithProgids" /v "MarkRead.Document" /f >nul 2>&1

echo Removed MarkRead context menu and file associations.
echo.
pause
