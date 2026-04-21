---
phase: quick
plan: 260421-mzz
subsystem: infra
tags: [inno-setup, installer, build, batch, uninstall, localappdata]

# Dependency graph
requires:
  - phase: 84c498e
    provides: LogService logs moved to %LOCALAPPDATA%\ANNA\ASLT\logs; initial build.bat integrity checks
provides:
  - ASLT-Setup.iss [UninstallDelete] covering LocalAppData runtime logs
  - build.bat wildcard installer resolution with no-installer ERROR guard
  - build.bat without brittle 400-file-count gate
affects: [installer release, clean-uninstall verification, version bumps]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Inno Setup {localappdata} + dirifempty sweep for per-user runtime cleanup"
    - "build.bat wildcard resolution + 'if not defined' guard (no version hardcode, no silent 0 MB)"

key-files:
  created: []
  modified:
    - installer/ASLT-Setup.iss
    - installer/build.bat

key-decisions:
  - "Legacy {app}\\logs [UninstallDelete] entry preserved as defensive sweep for pre-84c498e upgraders"
  - "dirifempty on {localappdata}\\ANNA\\ASLT and {localappdata}\\ANNA keeps cleanup safe when other ANNA products coexist"
  - "Publish file count removed as correctness gate — name-based checks (ASLTv1.exe, coreclr.dll, OpenCvSharpExtern.dll) remain sole integrity gate"
  - "Installer resolution switched to ASLT-Setup-v*.exe wildcard — build.bat is version-agnostic; MyAppVersion bumps need no build.bat edits"
  - "Explicit ERROR when Output\\ produces no installer — replaces prior 0 MB silent-success regression mode"

patterns-established:
  - "Quick-task atomic commits: one independent fix per commit so cherry-pick/revert stays clean"
  - "Content-based Edit targeting (not fixed line numbers) when earlier tasks shift lines in the same file"

requirements-completed: [CODEX-P1, CODEX-P2, CODEX-P3]

# Metrics
duration: 2min
completed: 2026-04-21
---

# Quick 260421-mzz: Codex Installer/Build Follow-up Summary

**LocalAppData log cleanup added to Inno Setup uninstaller; build.bat decoupled from version hardcode and file-count brittleness while preserving name-based integrity gates.**

## Performance

- **Duration:** ~2 min
- **Started:** 2026-04-21T07:37:31Z
- **Completed:** 2026-04-21T07:39:44Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments

- **CODEX-P1 — Clean-uninstall contract honored:** `[UninstallDelete]` now removes `%LOCALAPPDATA%\ANNA\ASLT\logs` (where LogService writes per 84c498e), with `dirifempty` sweeps on `{localappdata}\ANNA\ASLT` and `{localappdata}\ANNA`. Legacy `{app}\logs` entry retained for pre-84c498e upgraders.
- **CODEX-P2 — Brittle numeric gate removed:** build.bat no longer hard-fails on `FILE_COUNT < 400`. Name-based required-file checks (ASLTv1.exe, coreclr.dll, OpenCvSharpExtern.dll) remain the sole correctness gate for publish integrity.
- **CODEX-P3 — Version-agnostic installer resolution:** build.bat resolves installer via `ASLT-Setup-v*.exe` wildcard, captures INSTALLER_PATH inside the for-loop, guards with `if not defined INSTALLER_PATH` (explicit ERROR when Output\ is empty), and prints the resolved path. No `ASLT-Setup-v1.0.0.exe` hardcode remains anywhere in the file.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add LocalAppData log cleanup to uninstall section (CODEX-P1)** — `4c14851` (fix)
2. **Task 2: Remove 400-file-count gate from build.bat (CODEX-P2)** — `d193500` (fix)
3. **Task 3: Wildcard installer resolution + existence guard (CODEX-P3)** — `ef84820` (fix)

_No plan metadata commit for this quick task — STATE.md and SUMMARY.md bundled separately at hand-back time._

## Files Created/Modified

- `installer/ASLT-Setup.iss` — `[UninstallDelete]` section expanded; 5 new lines added, legacy `{app}\logs` preserved.
- `installer/build.bat` — File-count block removed (7 lines deleted); installer-size block replaced with wildcard + existence guard (12 lines added, 3 lines changed); header REM line 4 updated to `{MyAppVersion}` to purge final `v1.0.0` hardcode.

### Before / after — `installer/ASLT-Setup.iss` [UninstallDelete]

**Before (lines 67-72):**
```
[UninstallDelete]
; D-15: Remove everything under <InstallDir> including logs/ — but NOT user JSON outside InstallDir
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\ffmpeg"
; Final sweep: any leftover files inside {app} that were created post-install
Type: dirifempty; Name: "{app}"
```

**After (lines 67-77):**
```
[UninstallDelete]
; D-15: Remove everything under <InstallDir> including logs/ — but NOT user JSON outside InstallDir
; Legacy path (pre-84c498e builds wrote logs here)
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\ffmpeg"
; Runtime logs moved to LocalAppData in 84c498e — clean those on uninstall too
Type: filesandordirs; Name: "{localappdata}\ANNA\ASLT\logs"
Type: dirifempty; Name: "{localappdata}\ANNA\ASLT"
Type: dirifempty; Name: "{localappdata}\ANNA"
; Final sweep: any leftover files inside {app} that were created post-install
Type: dirifempty; Name: "{app}"
```

### Before / after — `installer/build.bat` file-count gate (Task 2)

**Before (lines 49-55):**
```
REM Count files: self-contained publish should produce 400+ files
for /f %%A in ('dir /a-d /b /s "%PUBLISH_DIR%" ^| find /c /v ""') do set FILE_COUNT=%%A
if %FILE_COUNT% LSS 400 (
  echo ERROR: Publish produced only %FILE_COUNT% files. Expected 400+ for self-contained build.
  exit /b 1
)
echo Publish verified: %FILE_COUNT% files present.
```

**After:** Block deleted. Step 4 name-based checks flow directly into `echo [5/5] Compiling Inno Setup script...` with no file-count noise.

### Before / after — `installer/build.bat` installer-size block (Task 3)

**Before (lines 64-75, pre-Task-3):**
```
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
```

**After (lines 57-78):**
```
REM Installer size sanity check (expected 90-150 MB with LZMA2 solid compression)
set INSTALLER_PATH=
set INSTALLER_SIZE=0
for %%F in ("%~dp0Output\ASLT-Setup-v*.exe") do (
  set INSTALLER_PATH=%%F
  set INSTALLER_SIZE=%%~zF
)
if not defined INSTALLER_PATH (
  echo ERROR: No installer produced in Output\.
  exit /b 1
)
set /a INSTALLER_MB=%INSTALLER_SIZE% / 1048576
echo Installer size: %INSTALLER_MB% MB
if %INSTALLER_MB% LSS 80 (
  echo WARNING: Installer is only %INSTALLER_MB% MB, expected 90-150 MB.
  echo This may indicate missing payload. Verify publish content manually.
)

echo.
echo BUILD SUCCESS.
echo Installer: %INSTALLER_PATH%
endlocal
```

## Scope Boundary Confirmation

- **`Services/LogService.cs` NOT modified.** `git diff 84c498e HEAD -- Services/LogService.cs` returns zero lines. The log-path move in 84c498e is already correct and intentional per humming-hugging-bear.md scope boundary.
- **Legacy `{app}\logs` entry in `[UninstallDelete]` PRESERVED** as defensive cleanup for users upgrading from pre-84c498e builds.
- **Name-based required-file checks in build.bat PRESERVED verbatim** (ASLTv1.exe, coreclr.dll, OpenCvSharpExtern.dll).
- **ISCC compile step untouched** (step 5 header and `%ISCC% "%~dp0ASLT-Setup.iss"` with errorlevel check unchanged).

## Decisions Made

None beyond what the plan already recorded. All decisions listed in the frontmatter `key-decisions` were pre-specified by humming-hugging-bear.md and reaffirmed here for traceability.

## Deviations from Plan

None — plan executed exactly as written.

The header REM line `REM Output: installer\Output\ASLT-Setup-v1.0.0.exe` on build.bat line 4 was updated to `REM Output: installer\Output\ASLT-Setup-v{MyAppVersion}.exe` as part of Task 3 to satisfy the explicit success criterion "installer/build.bat no longer contains the string ASLT-Setup-v1.0.0.exe anywhere in the file." This is not a deviation — it's completing the letter of the plan's success criterion on a comment line that the three original code-target blocks did not touch.

## Issues Encountered

None.

## Verification

### A. Static string checks (performed)

- `grep -c 'localappdata.*ANNA.ASLT.logs' installer/ASLT-Setup.iss` → `1` (expected)
- `grep 'FILE_COUNT|LSS 400|find /c /v|ASLT-Setup-v1\.0\.0\.exe' installer/build.bat` → **no matches** (all four must be absent)
- `grep 'ASLT-Setup-v\*\.exe' installer/build.bat` → line 60 (wildcard present)
- `grep 'INSTALLER_PATH=%%F' installer/build.bat` → line 61
- `grep 'if not defined INSTALLER_PATH' installer/build.bat` → line 64
- `grep 'echo Installer: %INSTALLER_PATH%' installer/build.bat` → line 77
- `grep 'ASLTv1.exe missing|coreclr.dll missing|OpenCvSharpExtern.dll missing' installer/build.bat` → lines 38, 42, 46 (all three preserved)
- `git diff 84c498e HEAD -- Services/LogService.cs` → 0 lines (untouched)

### B. End-to-end validation (out of scope for executor, noted for release candidate)

Per humming-hugging-bear.md §검증, the following require environment with .NET 8 SDK + Inno Setup 6 + `installer\ffmpeg\ffmpeg.exe`:

- **A (ISCC static compile)** — run `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\ASLT-Setup.iss` and confirm no errors on the expanded `[UninstallDelete]`.
- **B (full build.bat smoke)** — run `installer\build.bat` and confirm `[4/5] → [5/5]` transitions without file-count lines, final `Installer: ...\Output\ASLT-Setup-v1.0.0.exe` with valid MB.
- **C (clean-uninstall E2E)** — install, run ASLT to generate `%LOCALAPPDATA%\ANNA\ASLT\logs\AOLT-*.log`, uninstall, confirm both `C:\Program Files\ANNA\ASLT` and `%LOCALAPPDATA%\ANNA\ASLT\logs` removed.

These are deferred to manual run on the release candidate; not blocking commit acceptance per the plan's task-level verification contract.

## Next Phase Readiness

- Installer/build pipeline is now version-agnostic and correctness-gated by name-based checks only.
- Clean-uninstall contract declared by 84c498e is now fully honored in the Inno Setup script.
- No new blockers. The three commits (4c14851, d193500, ef84820) can each be cherry-picked or reverted independently if downstream testing surfaces a regression.

## Self-Check: PASSED

All four must-have artifact/truth claims verified:
- FOUND: `installer/ASLT-Setup.iss` contains `{localappdata}\ANNA\ASLT\logs`
- FOUND: `installer/build.bat` contains `ASLT-Setup-v*.exe` wildcard
- FOUND: commit `4c14851` in git log
- FOUND: commit `d193500` in git log
- FOUND: commit `ef84820` in git log
- CONFIRMED: `Services/LogService.cs` diff vs 84c498e is 0 lines

---
*Quick task: 260421-mzz-codex-installer-uninstall-build-bat*
*Completed: 2026-04-21*
