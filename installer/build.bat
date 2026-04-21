@echo off
REM ASLT Installer Build Script
REM Runs: dotnet publish (self-contained x64) -> Inno Setup ISCC compile
REM Output: installer\Output\ASLT-Setup-v1.0.0.exe
REM Prereqs: .NET 8 SDK, Inno Setup 6.x (ISCC.exe), installer\ffmpeg\ffmpeg.exe placed manually

setlocal
set REPO_ROOT=%~dp0..
set PUBLISH_DIR=%REPO_ROOT%\bin\Release\net8.0-windows\win-x64\publish
set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

echo [1/5] Verifying prerequisites...
if not exist "%~dp0ffmpeg\ffmpeg.exe" (
  echo ERROR: installer\ffmpeg\ffmpeg.exe not found. Download from https://www.gyan.dev/ffmpeg/builds/ ^(essentials, win-x64^) and place ffmpeg.exe in installer\ffmpeg\.
  exit /b 1
)
if not exist %ISCC% (
  echo ERROR: Inno Setup 6 not found at %ISCC%. Install from https://jrsoftware.org/isinfo.php
  exit /b 1
)

echo [2/5] Cleaning stale publish output...
if exist "%PUBLISH_DIR%" rd /s /q "%PUBLISH_DIR%"
if exist "%~dp0Output" rd /s /q "%~dp0Output"

echo [3/5] Running dotnet publish ^(self-contained x64^)...
pushd "%REPO_ROOT%"
dotnet publish ASLTv1.0.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false
if errorlevel 1 (
  echo ERROR: dotnet publish failed.
  popd
  exit /b 1
)
popd

echo [4/5] Verifying publish output integrity...
if not exist "%PUBLISH_DIR%\ASLTv1.exe" (
  echo ERROR: ASLTv1.exe missing from publish output.
  exit /b 1
)
if not exist "%PUBLISH_DIR%\coreclr.dll" (
  echo ERROR: coreclr.dll missing - self-contained publish is broken.
  exit /b 1
)
if not exist "%PUBLISH_DIR%\OpenCvSharpExtern.dll" (
  echo ERROR: OpenCvSharpExtern.dll missing - OpenCV native runtime absent.
  exit /b 1
)

echo [5/5] Compiling Inno Setup script...
%ISCC% "%~dp0ASLT-Setup.iss"
if errorlevel 1 (
  echo ERROR: ISCC compile failed.
  exit /b 1
)

REM Installer size sanity check (expected 90-150 MB with LZMA2 solid compression)
for %%F in ("%~dp0Output\ASLT-Setup-v1.0.0.exe") do set INSTALLER_SIZE=%%~zF
set /a INSTALLER_MB=%INSTALLER_SIZE% / 1048576
echo Installer size: %INSTALLER_MB% MB
if %INSTALLER_MB% LSS 80 (
  echo WARNING: Installer is only %INSTALLER_MB% MB, expected 90-150 MB.
  echo This may indicate missing payload. Verify publish content manually.
)

echo.
echo BUILD SUCCESS.
echo Installer: %~dp0Output\ASLT-Setup-v1.0.0.exe
endlocal
