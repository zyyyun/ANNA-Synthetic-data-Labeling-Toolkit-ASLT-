---
phase: quick-260427-eyf
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - ASLTv1.0.csproj
  - installer/ASLT-Setup.iss
  - installer/build-installer.ps1
  - installer/Output/ASLT-Setup-v1.0.0.exe  # deleted
  - installer/Output/ASLT-Setup-v1.0.1.exe  # produced by build
autonomous: true
requirements:
  - QUICK-260427-eyf-A  # build automation script
  - QUICK-260427-eyf-B  # version bump 1.0.0 → 1.0.1
  - QUICK-260427-eyf-C  # produce real v1.0.1 installer artifact

must_haves:
  truths:
    - "installer/build-installer.ps1 단일 실행으로 publish + ISCC 컴파일 + 검증이 자동화된다"
    - "csproj Version/AssemblyVersion/FileVersion/InformationalVersion 모두 1.0.1 이다"
    - "ASLT-Setup.iss MyAppVersion 이 1.0.1 이고 그 외 라인(AppId 포함) 은 변경되지 않았다"
    - "installer/Output/ASLT-Setup-v1.0.0.exe 가 삭제되었다"
    - "installer/Output/ASLT-Setup-v1.0.1.exe 가 실제로 빌드되어 존재한다 (오늘 날짜 타임스탬프, 100MB+ 사이즈)"
  artifacts:
    - path: "installer/build-installer.ps1"
      provides: "End-to-end installer build automation"
      contains: "dotnet publish"
    - path: "ASLTv1.0.csproj"
      provides: "Version 1.0.1 across 4 version fields"
      contains: "<Version>1.0.1</Version>"
    - path: "installer/ASLT-Setup.iss"
      provides: "Inno Setup script with MyAppVersion 1.0.1"
      contains: "#define MyAppVersion \"1.0.1\""
    - path: "installer/Output/ASLT-Setup-v1.0.1.exe"
      provides: "Fresh installer artifact with 05.5/05.6 fixes"
  key_links:
    - from: "installer/build-installer.ps1"
      to: "ASLTv1.0.csproj"
      via: "dotnet publish -c Release -r win-x64 --self-contained true"
      pattern: "dotnet publish"
    - from: "installer/build-installer.ps1"
      to: "installer/ASLT-Setup.iss"
      via: "ISCC.exe at C:\\Program Files (x86)\\Inno Setup 6"
      pattern: "ISCC\\.exe"
    - from: "installer/ASLT-Setup.iss"
      to: "bin/Release/net8.0-windows/win-x64/publish/"
      via: "PublishDir define + [Files] section recursesubdirs"
      pattern: "PublishDir"
---

<objective>
ASLT 인스톨러 빌드 파이프라인을 자동화하고 1.0.1 버전 인스톨러를 생성한다.

**문제:**
1. `bin/Release/net8.0-windows/win-x64/publish/` 디렉토리가 없음 (publish 미실행 또는 삭제)
2. `installer/Output/ASLT-Setup-v1.0.0.exe` 는 05.5/05.6 결함수정 이전 빌드 — stale
3. csproj 와 iss 모두 Version 이 여전히 1.0.0 — 새 빌드해도 동일 파일명으로 덮어쓰기 위험

**해결:**
1. `installer/build-installer.ps1` — 단일 PowerShell 스크립트로 stop-process → clean → publish → verify → ISCC → verify → report 전 과정 자동화
2. csproj + iss 의 Version 1.0.0 → 1.0.1 동시 bump
3. stale v1.0.0 인스톨러 삭제 + 신규 v1.0.1 인스톨러 빌드 실행

Purpose: 향후 버전 업데이트 시 단 한 번의 `.\build-installer.ps1` 실행으로 일관된 빌드 산출물을 보장. GS인증 제출용 인스톨러 신뢰성 확보.
Output: build-installer.ps1, 버전 업데이트된 csproj/iss, 신규 ASLT-Setup-v1.0.1.exe 인스톨러
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@CLAUDE.md
@installer/ASLT-Setup.iss
@installer/build.bat
@ASLTv1.0.csproj

<interfaces>
<!-- 핵심 컨트랙트 - 실행자가 코드베이스 탐색 없이 바로 사용 -->

ISCC.exe 절대경로:
  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

dotnet publish 명령 (csproj 의 SelfContained=true + RuntimeIdentifier=win-x64 이미 정의됨):
  dotnet publish ASLTv1.0.csproj -c Release -r win-x64 --self-contained true -o bin\Release\net8.0-windows\win-x64\publish

publish 산출물 검증 경로 (iss 의 #define PublishDir 이 참조):
  bin\Release\net8.0-windows\win-x64\publish\ASLTv1.exe  ← 반드시 존재해야 함
  bin\Release\net8.0-windows\win-x64\publish\OpenCvSharpExtern.dll  ← 네이티브 DLL
  bin\Release\net8.0-windows\win-x64\publish\opencv_videoio_ffmpeg4110_64.dll

ISCC 컴파일 명령:
  & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer\ASLT-Setup.iss"

ISCC 산출물 (iss 의 OutputDir=Output, OutputBaseFilename=ASLT-Setup-v{#MyAppVersion}):
  installer\Output\ASLT-Setup-v1.0.1.exe  ← 검증 대상

csproj Version 필드 (4개, 라인 13-17):
  <Version>1.0.0</Version>                      → 1.0.1
  <AssemblyVersion>1.0.0.0</AssemblyVersion>    → 1.0.1.0
  <FileVersion>1.0.0.0</FileVersion>            → 1.0.1.0
  <InformationalVersion>1.0.0</InformationalVersion>  → 1.0.1

iss Version 필드 (라인 7, 단 1개 — AppId 와 분리):
  #define MyAppVersion "1.0.0"  → "1.0.1"

ffmpeg.exe (보존 필수, 건드리지 말 것):
  installer\ffmpeg\ffmpeg.exe  ← iss [Files] 가 직접 참조, clean 단계에서 절대 삭제 금지
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: 버전 1.0.0 → 1.0.1 bump (csproj + iss) 및 stale 인스톨러 삭제</name>
  <files>ASLTv1.0.csproj, installer/ASLT-Setup.iss, installer/Output/ASLT-Setup-v1.0.0.exe</files>
  <action>
**ASLTv1.0.csproj 수정 (4 라인):**
- 라인 13: `<Version>1.0.0</Version>` → `<Version>1.0.1</Version>`
- 라인 15: `<AssemblyVersion>1.0.0.0</AssemblyVersion>` → `<AssemblyVersion>1.0.1.0</AssemblyVersion>`
- 라인 16: `<FileVersion>1.0.0.0</FileVersion>` → `<FileVersion>1.0.1.0</FileVersion>`
- 라인 17: `<InformationalVersion>1.0.0</InformationalVersion>` → `<InformationalVersion>1.0.1</InformationalVersion>`

다른 라인 (특히 라인 23 SelfContained, 라인 24 RuntimeIdentifier) 절대 변경하지 말 것.

**installer/ASLT-Setup.iss 수정 (1 라인만):**
- 라인 7: `#define MyAppVersion "1.0.0"` → `#define MyAppVersion "1.0.1"`

**절대 변경 금지 라인:**
- 라인 14 `AppId={{B4A2C1F0-8E4D-4A6B-9F3A-ASLT10000001}}` — 같은 AppId 유지해야 1.0.x in-place upgrade 동작 (Phase 05 D-14)
- 라인 27 `OutputBaseFilename=ASLT-Setup-v{#MyAppVersion}` — 자동으로 v1.0.1 반영됨, 직접 수정 금지
- 그 외 모든 [Setup]/[Files]/[Icons]/[Run]/[UninstallDelete] 섹션

**stale 인스톨러 삭제:**
- `installer/Output/ASLT-Setup-v1.0.0.exe` 삭제 (Bash 또는 PowerShell `Remove-Item` 사용)

이 task 는 build-installer.ps1 실행 전 사전 작업 — Task 2 의 verify 단계에서 v1.0.1 파일 검증이 작동하려면 csproj/iss 가 먼저 1.0.1 이어야 함.
  </action>
  <verify>
    <automated>grep -c "1\.0\.1" ASLTv1.0.csproj | grep -E "^4$" &amp;&amp; grep -c "MyAppVersion \"1\.0\.1\"" installer/ASLT-Setup.iss | grep -E "^1$" &amp;&amp; ! test -f installer/Output/ASLT-Setup-v1.0.0.exe</automated>
  </verify>
  <done>
- csproj 에 "1.0.1" 패턴이 정확히 4번 등장 (Version, AssemblyVersion 의 1.0.1.0, FileVersion 의 1.0.1.0, InformationalVersion)
- iss 에 `#define MyAppVersion "1.0.1"` 1줄
- iss 의 AppId/OutputBaseFilename/[Files]/[UninstallDelete] 라인 모두 그대로 유지 (`git diff installer/ASLT-Setup.iss` 가 라인 7 한 줄만 보여줌)
- installer/Output/ASLT-Setup-v1.0.0.exe 파일 부재
  </done>
</task>

<task type="auto">
  <name>Task 2: build-installer.ps1 작성 + 실행하여 ASLT-Setup-v1.0.1.exe 생성</name>
  <files>installer/build-installer.ps1, bin/Release/net8.0-windows/win-x64/publish/* (생성), installer/Output/ASLT-Setup-v1.0.1.exe (생성)</files>
  <action>
**installer/build-installer.ps1 생성 — 다음 7단계를 순차 실행하는 PowerShell 스크립트:**

```powershell
# ASLT 인스톨러 빌드 자동화 스크립트
# Usage: .\installer\build-installer.ps1
# Or:    powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$CsprojPath  = Join-Path $ProjectRoot "ASLTv1.0.csproj"
$IssPath     = Join-Path $PSScriptRoot "ASLT-Setup.iss"
$PublishDir  = Join-Path $ProjectRoot "bin\Release\net8.0-windows\win-x64\publish"
$OutputDir   = Join-Path $PSScriptRoot "Output"
$IsccExe     = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "=== ASLT Installer Build ===" -ForegroundColor Cyan

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
Push-Location $ProjectRoot
try {
    & dotnet publish $CsprojPath -c Release -r win-x64 --self-contained true -o $PublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish 실패 (exit code $LASTEXITCODE)" }
} finally {
    Pop-Location
}

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
if (-not (Test-Path $IsccExe)) { throw "ISCC.exe 없음: $IsccExe — Inno Setup 6 설치 확인" }
& $IsccExe $IssPath
if ($LASTEXITCODE -ne 0) { throw "ISCC 컴파일 실패 (exit code $LASTEXITCODE)" }

# 7. 인스톨러 산출물 검증 + 리포트
Write-Host "[7/7] 인스톨러 검증..." -ForegroundColor Yellow
$InstallerPath = Join-Path $OutputDir "ASLT-Setup-v$Version.exe"
if (-not (Test-Path $InstallerPath)) { throw "인스톨러 생성 실패: $InstallerPath 없음" }
$InstallerInfo = Get-Item $InstallerPath
$SizeMB = [math]::Round($InstallerInfo.Length / 1MB, 2)

Write-Host ""
Write-Host "=== Build Successful ===" -ForegroundColor Green
Write-Host "  Path:      $InstallerPath" -ForegroundColor White
Write-Host "  Size:      $SizeMB MB" -ForegroundColor White
Write-Host "  Modified:  $($InstallerInfo.LastWriteTime)" -ForegroundColor White
Write-Host "  Version:   $Version" -ForegroundColor White
```

**스크립트 작성 후 즉시 실행:**

PowerShell 도구를 사용하여:
```
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

또는 PowerShell 도구가 사용 가능하면 직접:
```
& "installer\build-installer.ps1"
```

**중요:**
- dotnet publish 는 1-3분 소요될 수 있음 — Bash timeout 을 600000(10분)으로 설정
- publish 디렉토리는 ~250MB (self-contained .NET 8 + OpenCV 네이티브)
- 최종 인스톨러는 lzma2 압축으로 ~100-150MB 예상
- ffmpeg.exe (`installer/ffmpeg/ffmpeg.exe`) 는 clean 대상이 아님 — 스크립트가 건드리지 않도록 OutputDir 만 *.exe 필터로 정리

**실패 시 대응:**
- "ISCC.exe 없음" → user 에게 Inno Setup 6 설치 경로 확인 요청 (checkpoint 불필요, 에러 메시지로 충분)
- "dotnet publish 실패" → 출력에서 컴파일 에러 식별 후 보고
- ASLTv1.exe 가 lock 되어 있으면 1단계의 Stop-Process 로 자동 해결됨
  </action>
  <verify>
    <automated>test -f installer/build-installer.ps1 &amp;&amp; test -f installer/Output/ASLT-Setup-v1.0.1.exe &amp;&amp; test -f bin/Release/net8.0-windows/win-x64/publish/ASLTv1.exe &amp;&amp; test "$(stat -c%s installer/Output/ASLT-Setup-v1.0.1.exe 2>/dev/null || stat -f%z installer/Output/ASLT-Setup-v1.0.1.exe)" -gt 50000000</automated>
  </verify>
  <done>
- installer/build-installer.ps1 존재, 7단계 모두 포함 (stop-process, version-check, clean, publish, publish-verify, ISCC, installer-verify)
- bin/Release/net8.0-windows/win-x64/publish/ASLTv1.exe 존재 (publish 성공)
- installer/Output/ASLT-Setup-v1.0.1.exe 존재 (50MB 이상 — self-contained 빌드)
- installer/ffmpeg/ffmpeg.exe 보존됨 (clean 단계에서 삭제되지 않음)
- 스크립트 실행 출력에 "Build Successful" + 경로/크기/타임스탬프/버전 표시
  </done>
</task>

</tasks>

<verification>
**최종 통합 검증:**

```bash
# 1. 버전 일관성 (csproj 4개 + iss 1개)
grep -c "1\.0\.1" ASLTv1.0.csproj  # 4
grep "MyAppVersion" installer/ASLT-Setup.iss  # "1.0.1"

# 2. stale 인스톨러 부재
test ! -f installer/Output/ASLT-Setup-v1.0.0.exe

# 3. 신규 인스톨러 존재 + 사이즈
ls -lh installer/Output/ASLT-Setup-v1.0.1.exe

# 4. ffmpeg 보존
test -f installer/ffmpeg/ffmpeg.exe

# 5. 스크립트 재실행 가능 (idempotent)
# (실제 재실행은 build 시간 때문에 생략 — 스크립트 구조만 검증)
grep -E "Stop-Process|Remove-Item|dotnet publish|ISCC" installer/build-installer.ps1
```

**iss 라인 무결성 검증:**
```bash
# 라인 7만 변경되어야 함
git diff installer/ASLT-Setup.iss | grep -E "^[+-]" | grep -v "^[+-]{3}" | wc -l  # 정확히 2 (라인 1+ 1-)
```
</verification>

<success_criteria>
- [ ] ASLTv1.0.csproj 4개 Version 필드 모두 1.0.1
- [ ] installer/ASLT-Setup.iss 라인 7 만 1.0.1 로 변경 (AppId 등 다른 라인 유지)
- [ ] installer/Output/ASLT-Setup-v1.0.0.exe 삭제됨
- [ ] installer/build-installer.ps1 작성됨 (7단계: stop-process, version-check, clean, publish, publish-verify, ISCC, installer-verify)
- [ ] 스크립트 실제 실행 완료 — exit code 0
- [ ] installer/Output/ASLT-Setup-v1.0.1.exe 생성됨 (50MB+ 사이즈, 오늘 날짜 타임스탬프)
- [ ] installer/ffmpeg/ffmpeg.exe 그대로 유지
- [ ] 빌드 출력 리포트에 경로/사이즈/타임스탬프/버전 4가지 포함
</success_criteria>

<output>
After completion, create `.planning/quick/260427-eyf-installer-1-0-1/260427-eyf-SUMMARY.md` documenting:
- 변경된 파일 (csproj 라인 13-17, iss 라인 7, build-installer.ps1 신규)
- 삭제된 파일 (ASLT-Setup-v1.0.0.exe)
- 생성된 인스톨러 (ASLT-Setup-v1.0.1.exe — 경로, 사이즈, 타임스탬프)
- 스크립트 실행 결과 요약 (publish 시간, ISCC 시간, 총 빌드 시간)
- 향후 버전 업데이트 시 사용법 (`.\installer\build-installer.ps1`)
</output>
