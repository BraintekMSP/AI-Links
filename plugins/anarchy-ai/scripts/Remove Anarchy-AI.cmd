REM Purpose: human-friendly Windows front door for safe Anarchy-AI cleanup from an installed or source plugin bundle.
REM Expected input: the sibling remove-anarchy-ai-human.ps1 script and a reachable PowerShell host.
REM Expected output: launches safe quarantine-first cleanup, keeps the console open, and returns the cleanup exit code.
REM Critical dependencies: sibling human cleanup script, powershell.exe, and correct script-directory resolution.
@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "HUMAN_HELPER=%SCRIPT_DIR%remove-anarchy-ai-human.ps1"

if not exist "%HUMAN_HELPER%" (
  echo Anarchy-AI cleanup helper not found: "%HUMAN_HELPER%" 1>&2
  echo.
  echo Press any key to close.
  pause >nul
  exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%HUMAN_HELPER%"
set "EXIT_CODE=%ERRORLEVEL%"
echo.
if "%EXIT_CODE%"=="0" (
  echo Press any key to close.
) else (
  echo Cleanup ended with warnings or manual review. Press any key to close.
)
pause >nul
exit /b %EXIT_CODE%
