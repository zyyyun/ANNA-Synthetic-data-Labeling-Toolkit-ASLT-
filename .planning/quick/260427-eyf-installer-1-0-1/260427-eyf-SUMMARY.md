---
phase: quick-260427-eyf
plan: 01
subsystem: installer
tags: [installer, automation, version-bump, build-pipeline, gs-cert]
type: summary
status: complete
requires: []
provides:
  - "End-to-end installer build automation (single PowerShell entry point)"
  - "Versioned installer artifact for v1.0.1 GS인증 제출"
  - "Reusable build pipeline for future 1.0.x releases"
affects:
  - "ASLTv1.0.csproj: 4 version fields"
  - "installer/ASLT-Setup.iss: MyAppVersion define"
  - "installer/Output/: stale 1.0.0 removed, fresh 1.0.1 produced"
tech_stack_added: []
tech_stack_patterns:
  - "PowerShell ErrorActionPreference=Stop + LASTEXITCODE 가드 + Push/Pop-Location 으로 7단계 fail-fast 자동화"
  - "csproj XML 파싱으로 build script 가 단일 source-of-truth (Version) 추출 — drift 방지"
  - "OutputDir 정리 시 *.exe 필터로 ffmpeg/ 보존 (recursesubdirs 반대 방향 가드)"
key_files_created:
  - "installer/build-installer.ps1"
  - ".planning/quick/260427-eyf-installer-1-0-1/260427-eyf-SUMMARY.md"
key_files_modified:
  - "ASLTv1.0.csproj (lines 13/15/16/17 — 4 Version fields)"
  - "installer/ASLT-Setup.iss (line 7 — MyAppVersion only)"
key_files_deleted:
  - "installer/Output/ASLT-Setup-v1.0.0.exe (stale, gitignored)"
artifacts_produced:
  - "installer/Output/ASLT-Setup-v1.0.1.exe (98.17 MB, 2026-04-27 10:51:11)"
  - "bin/Release/net8.0-windows/win-x64/publish/ (~250MB self-contained)"
decisions:
  - "build-installer.ps1 가 csproj XML 파싱으로 Version 자동 추출 — iss 의 #define 과 일관성 검증 가능"
  - "Stop-Process gate 1단계로 file-lock 회피 (이전 빌드 시 ASLTv1.exe 가 실행 중일 수 있음)"
  - "OutputDir 정리는 *.exe 필터로 한정 — ffmpeg/ 와 같은 인접 디렉토리 절대 보호 (recursesubdirs 회피)"
metrics:
  duration_min: 3
  completed_date: "2026-04-27"
  tasks_completed: 2
  commits: 2
---

# Quick 260427-eyf: Installer 1.0.1 Build Automation Summary

ASLT 인스톨러 빌드 파이프라인을 단일 PowerShell 스크립트로 자동화하고, 05.5/05.6 결함수정이 반영된 v1.0.1 인스톨러 산출물을 신규 생성했다.

## What Was Built

### 1. installer/build-installer.ps1 (신규, 90 lines)

7단계 fail-fast 자동화 스크립트:

| 단계 | 작업 | 가드 |
|------|------|------|
| 1 | `Stop-Process ASLTv1` | file-lock 회피 (silent if not running) |
| 2 | `csproj` Version 추출 | `[xml]` 파싱, 단일 source-of-truth |
| 3 | clean `bin/Release` + `Output/*.exe` | ffmpeg/ 미터치 (필터 한정) |
| 4 | `dotnet publish -c Release -r win-x64 --self-contained` | LASTEXITCODE 검증 |
| 5 | `ASLTv1.exe` + `OpenCvSharpExtern.dll` 존재 검증 | publish 검증 |
| 6 | ISCC.exe 컴파일 | 절대경로 체크 + LASTEXITCODE 검증 |
| 7 | `ASLT-Setup-v{Version}.exe` 검증 + 리포트 | Path/Size/Modified/Version/Total 출력 |

**재실행 가능성:** 모든 단계가 idempotent — 중간 실패 후 재실행해도 동일 결과.

### 2. 버전 동기화 (1.0.0 → 1.0.1)

**ASLTv1.0.csproj** (라인 13/15/16/17):
- `<Version>1.0.1</Version>`
- `<AssemblyVersion>1.0.1.0</AssemblyVersion>`
- `<FileVersion>1.0.1.0</FileVersion>`
- `<InformationalVersion>1.0.1</InformationalVersion>`

**installer/ASLT-Setup.iss** (라인 7만):
- `#define MyAppVersion "1.0.1"`
- AppId/OutputBaseFilename/[Files]/[UninstallDelete] 등 그 외 라인 모두 보존 (D-14 in-place upgrade 보장)
- `git diff` 결과: 정확히 1개 라인 변경 (1 add + 1 remove)

### 3. 신규 인스톨러 산출물

**installer/Output/ASLT-Setup-v1.0.1.exe**
- 경로: `C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.1.exe`
- 크기: 98.17 MB (102,943,345 bytes)
- 타임스탬프: 2026-04-27 10:51:11
- 버전: 1.0.1

stale `ASLT-Setup-v1.0.0.exe` (Apr 17 build, 05.5/05.6 결함수정 미반영) 삭제됨.

## Build Execution Report

```
=== Build Successful ===
  Path:      C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.1.exe
  Size:      98.17 MB
  Modified:  04/27/2026 10:51:11
  Version:   1.0.1
  Total:     63.9s
```

**시간 분배:**
- dotnet publish: ~8s (빠른 incremental, 첫 실행 시 더 길 수 있음)
- ISCC compile: 55.9s (lzma2 + 250MB publish + ffmpeg.exe 압축)
- 총 빌드 시간: 63.9s

**ISCC 경고 (정보용, 차단 아님):**
- "Architecture identifier `x64` is deprecated. Substituting `x64os`" — Inno Setup 6 최신 권장은 `x64compatible`. 향후 GS인증 후 재정비 가능 (현재 1.0.x 호환성 유지).
- "PrivilegesRequired=admin + localappdata 사용 경고" — Quick 260421-mzz 의 [UninstallDelete] {localappdata}\ANNA\ASLT 정리 의도된 설계 (CODEX-P1 결정). 무시.

## Tasks Executed

| # | Name | Commit | Files |
|---|------|--------|-------|
| 1 | 버전 1.0.0 → 1.0.1 bump (csproj + iss) 및 stale 인스톨러 삭제 | 07436f5 | ASLTv1.0.csproj, installer/ASLT-Setup.iss, installer/Output/ASLT-Setup-v1.0.0.exe (FS deleted) |
| 2 | build-installer.ps1 작성 + 실행하여 ASLT-Setup-v1.0.1.exe 생성 | dbe7a84 | installer/build-installer.ps1, bin/Release/.../publish/* (산출), installer/Output/ASLT-Setup-v1.0.1.exe (산출, gitignored) |

## Verification Results

| Check | Result |
|-------|--------|
| csproj 4x "1.0.1" 패턴 | PASS (4 occurrences) |
| iss MyAppVersion = "1.0.1" | PASS |
| iss 변경 범위 = 라인 7 한 줄만 | PASS (git diff: 1 add + 1 remove) |
| stale ASLT-Setup-v1.0.0.exe 부재 | PASS |
| ASLT-Setup-v1.0.1.exe 존재 (>50MB) | PASS (98.17 MB) |
| ffmpeg/ffmpeg.exe 보존 | PASS (mtime Mar 17 13:27 unchanged) |
| publish/ASLTv1.exe 존재 | PASS |
| build script 7단계 키워드 검증 | PASS (14 keyword matches) |

## Deviations from Plan

None — 플랜이 작성된 그대로 정확히 실행됨. ISCC 의 deprecated 경고는 정보성으로, 인스톨러 동작에 영향 없음. CS8632 nullable 경고 (pre-existing, csproj 의 `<Nullable>disable</Nullable>` 설정 결과)는 본 task 범위 외이며 publish 성공에 영향 없음.

## Future Usage (향후 버전 업데이트 시)

1. **버전 bump:** `ASLTv1.0.csproj` 의 4개 Version 필드 + `installer/ASLT-Setup.iss` 의 `MyAppVersion` 정의를 새 버전으로 수정.
2. **단일 명령으로 빌드:**
   ```powershell
   .\installer\build-installer.ps1
   ```
   또는 (실행 정책 우회):
   ```cmd
   powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
   ```
3. **산출물 확인:** `installer/Output/ASLT-Setup-v{NewVersion}.exe` — 콘솔 리포트의 Path/Size/Modified 검증.

**AppId 절대 변경 금지:** `installer/ASLT-Setup.iss` 라인 14 `{{B4A2C1F0-8E4D-4A6B-9F3A-ASLT10000001}}` 유지 — 1.0.x in-place upgrade 보장 (Phase 05 D-14).

## Self-Check: PASSED

- installer/build-installer.ps1: FOUND
- ASLTv1.0.csproj (Version 1.0.1 x4): FOUND
- installer/ASLT-Setup.iss (MyAppVersion 1.0.1): FOUND
- installer/Output/ASLT-Setup-v1.0.1.exe (98.17 MB): FOUND
- installer/Output/ASLT-Setup-v1.0.0.exe: ABSENT (intended)
- installer/ffmpeg/ffmpeg.exe: PRESERVED
- bin/Release/net8.0-windows/win-x64/publish/ASLTv1.exe: FOUND
- Commit 07436f5 (Task 1): FOUND in git log
- Commit dbe7a84 (Task 2): FOUND in git log
