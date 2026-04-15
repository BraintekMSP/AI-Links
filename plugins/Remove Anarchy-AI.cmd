REM Purpose: top-level human-friendly Windows front door for safe Anarchy-AI cleanup from the repo plugins folder.
REM Expected input: a source or installed Anarchy-AI bundle containing remove-anarchy-ai-human.ps1.
REM Expected output: launches the human cleanup helper, keeps the console open, and returns the helper exit code.
REM Critical dependencies: repo plugins directory layout, bundled human cleanup script discovery, powershell.exe, and exact path resolution before launch.
@echo off
setlocal

set "PLUGINS_ROOT=%~dp0"
set "HUMAN_HELPER="

if exist "%PLUGINS_ROOT%anarchy-ai\scripts\remove-anarchy-ai-human.ps1" (
  set "HUMAN_HELPER=%PLUGINS_ROOT%anarchy-ai\scripts\remove-anarchy-ai-human.ps1"
)

if not defined HUMAN_HELPER (
  for %%P in ("anarchy-ai-local-*" "anarchy-ai-herringms-*" "anarchy-local-*") do (
    for /d %%I in ("%PLUGINS_ROOT%%%~P") do (
      if not defined HUMAN_HELPER if exist "%%~fI\scripts\remove-anarchy-ai-human.ps1" (
        set "HUMAN_HELPER=%%~fI\scripts\remove-anarchy-ai-human.ps1"
      )
    )
  )
)

if not defined HUMAN_HELPER (
  echo No Anarchy-AI human cleanup helper was found under "%PLUGINS_ROOT%". 1>&2
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
