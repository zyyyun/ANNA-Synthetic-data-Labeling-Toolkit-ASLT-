# ASLT 인스톨러 빌드 자동화 스크립트
# Usage: .\installer\build-installer.ps1
# Or:    powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
#
# 7단계: stop-process → version-check → clean → publish → publish-verify → ISCC → installer-verify
# Korean OK. Re-runnable / idempotent.

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$CsprojPath  = Join-Path $ProjectRoot "ASLTv1.0.csproj"
$IssPath     = Join-Path $PSScriptRoot "ASLT-Setup.iss"
$PublishDir  = Join-Path $ProjectRoot "bin\Release\net8.0-windows\win-x64\publish"
$OutputDir   = Join-Path $PSScriptRoot "Output"
$IsccExe     = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "=== ASLT Installer Build ===" -ForegroundColor Cyan
$BuildStart = Get-Date

# 1. 실행 중인 ASLTv1.exe 종료 (file lock 회피)
Write-Host "[1/7] 실행 중인 ASLTv1.exe 종료..." -ForegroundColor Yellow
Get-Process -Name "ASLTv1" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  - PID $($_.Id) 종료" -ForegroundColor Gray
    $_ | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

# 2. csproj 에서 버전 추출 (검증용)
Write-Host "[2/7] csproj 버전 확인..." -ForegroundColor Yellow
$csprojXml = [xml](Get-Content $CsprojPath)
$Version = $csprojXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
Write-Host "  - csproj Version: $Version" -ForegroundColor Gray

# 3. clean — bin/Release + installer/Output (단, ffmpeg/ 는 절대 건드리지 않음)
Write-Host "[3/7] 빌드 산출물 정리..." -ForegroundColor Yellow
$ReleaseDir = Join-Path $ProjectRoot "bin\Release"
if (Test-Path $ReleaseDir) {
    Remove-Item -Path $ReleaseDir -Recurse -Force
    Write-Host "  - bin/Release 삭제 완료" -ForegroundColor Gray
}
if (Test-Path $OutputDir) {
    Get-ChildItem -Path $OutputDir -Filter "*.exe" | Remove-Item -Force
    Write-Host "  - installer/Output 정리 완료" -ForegroundColor Gray
}

# 4. dotnet publish (self-contained win-x64)
Write-Host "[4/7] dotnet publish 실행 중..." -ForegroundColor Yellow
$PublishStart = Get-Date
Push-Location $ProjectRoot
try {
    & dotnet publish $CsprojPath -c Release -r win-x64 --self-contained true -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 실패 (exit code $LASTEXITCODE)" }
} finally {
    Pop-Location
}
$PublishElapsed = (Get-Date) - $PublishStart
Write-Host "  - publish 완료 (소요: $([math]::Round($PublishElapsed.TotalSeconds, 1))s)" -ForegroundColor Gray

# 5. publish 산출물 검증
Write-Host "[5/7] publish 산출물 검증..." -ForegroundColor Yellow
$ExePath = Join-Path $PublishDir "ASLTv1.exe"
if (-not (Test-Path $ExePath)) { throw "ASLTv1.exe 생성 실패: $ExePath 없음" }
$NativeDll = Join-Path $PublishDir "OpenCvSharpExtern.dll"
if (-not (Test-Path $NativeDll)) { throw "OpenCvSharpExtern.dll 누락: $NativeDll 없음" }
$ExeSize = (Get-Item $ExePath).Length / 1MB
Write-Host "  - ASLTv1.exe ($([math]::Round($ExeSize, 2)) MB) OK" -ForegroundColor Green

# 6. ISCC 컴파일
Write-Host "[6/7] ISCC.exe 컴파일 중..." -ForegroundColor Yellow
$IsccStart = Get-Date
if (-not (Test-Path $IsccExe)) { throw "ISCC.exe 없음: $IsccExe — Inno Setup 6 설치 확인" }
& $IsccExe $IssPath
if ($LASTEXITCODE -ne 0) { throw "ISCC 컴파일 실패 (exit code $LASTEXITCODE)" }
$IsccElapsed = (Get-Date) - $IsccStart
Write-Host "  - ISCC 완료 (소요: $([math]::Round($IsccElapsed.TotalSeconds, 1))s)" -ForegroundColor Gray

# 7. 인스톨러 산출물 검증 + 리포트
Write-Host "[7/7] 인스톨러 검증..." -ForegroundColor Yellow
$InstallerPath = Join-Path $OutputDir "ASLT-Setup-v$Version.exe"
if (-not (Test-Path $InstallerPath)) { throw "인스톨러 생성 실패: $InstallerPath 없음" }
$InstallerInfo = Get-Item $InstallerPath
$SizeMB = [math]::Round($InstallerInfo.Length / 1MB, 2)
$TotalElapsed = (Get-Date) - $BuildStart

Write-Host ""
Write-Host "=== Build Successful ===" -ForegroundColor Green
Write-Host "  Path:      $InstallerPath" -ForegroundColor White
Write-Host "  Size:      $SizeMB MB" -ForegroundColor White
Write-Host "  Modified:  $($InstallerInfo.LastWriteTime)" -ForegroundColor White
Write-Host "  Version:   $Version" -ForegroundColor White
Write-Host "  Total:     $([math]::Round($TotalElapsed.TotalSeconds, 1))s" -ForegroundColor White
