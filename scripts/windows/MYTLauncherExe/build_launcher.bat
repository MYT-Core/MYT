@echo off
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] dotnet SDK not found.
  echo Install .NET 8 SDK from:
  echo https://dotnet.microsoft.com/en-us/download/dotnet/8.0
  exit /b 1
)

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%MYTLauncherExe.csproj"
set "OUTDIR=%SCRIPT_DIR%publish"
set "CFG=Release"
set "RID=win-x64"

if exist "%OUTDIR%" (
  rmdir /s /q "%OUTDIR%"
)

dotnet publish "%PROJECT%" ^
  -c %CFG% ^
  -r %RID% ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:SelfContained=true ^
  -p:PublishTrimmed=false ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%OUTDIR%"
if errorlevel 1 exit /b 1

echo.
echo Build done:
echo %OUTDIR%\MYTLauncher.exe
echo.
echo Copy MYTLauncher.exe into your MYT bin folder (same folder as mytd.exe).
echo.
dir "%OUTDIR%"

endlocal
