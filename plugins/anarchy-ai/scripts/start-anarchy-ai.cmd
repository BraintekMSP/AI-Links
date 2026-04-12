@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "PLUGIN_ROOT=%%~fI"
for %%I in ("%PLUGIN_ROOT%\..\..") do set "REPO_ROOT=%%~fI"
set "BUNDLED_ENTRY=%PLUGIN_ROOT%\runtime\win-x64\AnarchyAi.Mcp.Server.exe"
set "SERVER_DIR=%REPO_ROOT%\harness\server\dotnet"
set "NET8_ENTRY=%SERVER_DIR%\bin\Release\net8.0\win-x64\publish\AnarchyAi.Mcp.Server.exe"
set "NET48_ENTRY=%SERVER_DIR%\bin\Release\net48\AnarchyAi.Mcp.Server.exe"
set "DOTNET_CMD="

if exist "%BUNDLED_ENTRY%" (
  pushd "%PLUGIN_ROOT%"
  "%BUNDLED_ENTRY%"
  popd
  exit /b %ERRORLEVEL%
)

if exist "%NET8_ENTRY%" (
  pushd "%REPO_ROOT%"
  "%NET8_ENTRY%"
  popd
  exit /b %ERRORLEVEL%
)

if exist "%NET48_ENTRY%" (
  pushd "%REPO_ROOT%"
  "%NET48_ENTRY%"
  popd
  exit /b %ERRORLEVEL%
)

where dotnet >nul 2>nul
if not errorlevel 1 set "DOTNET_CMD=dotnet"
if not defined DOTNET_CMD if exist "%USERPROFILE%\.dotnet\dotnet.exe" set "DOTNET_CMD=%USERPROFILE%\.dotnet\dotnet.exe"
if not defined DOTNET_CMD (
  echo Anarchy AI could not find a bundled runtime or a repo-local harness build, and dotnet is not installed. 1>&2
  echo Preferred path: plugins\\anarchy-ai\\runtime\\win-x64\\AnarchyAi.Mcp.Server.exe 1>&2
  exit /b 1
)

for /f %%I in ('"%DOTNET_CMD%" --list-sdks ^| find /c /v ""') do set "SDK_COUNT=%%I"
if "%SDK_COUNT%"=="0" (
  echo Anarchy AI found the dotnet runtime but no .NET SDK. 1>&2
  echo A published executable is required unless the .NET SDK is installed for local build/publish. 1>&2
  exit /b 1
)

pushd "%SERVER_DIR%"
"%DOTNET_CMD%" publish -c Release -f net8.0 -r win-x64
if errorlevel 1 (
  popd
  exit /b 1
)
popd

if exist "%NET8_ENTRY%" (
  pushd "%REPO_ROOT%"
  "%NET8_ENTRY%"
  popd
  exit /b %ERRORLEVEL%
)

echo Anarchy AI publish completed but no launchable harness executable was found. 1>&2
exit /b 1
