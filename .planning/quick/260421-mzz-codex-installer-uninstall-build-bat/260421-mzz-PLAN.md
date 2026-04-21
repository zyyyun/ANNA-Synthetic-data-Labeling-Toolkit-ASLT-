---
phase: quick
plan: 260421-mzz
quick_id: 260421-mzz
type: execute
wave: 1
depends_on: []
files_modified:
  - installer/ASLT-Setup.iss
  - installer/build.bat
autonomous: true
requirements: [CODEX-P1, CODEX-P2, CODEX-P3]
must_haves:
  truths:
    - "Uninstaller removes logs at %LOCALAPPDATA%\\ANNA\\ASLT\\logs (clean-uninstall contract honored after 84c498e log path move)"
    - "build.bat does not fail builds based on publish file count — only name-based integrity checks remain as correctness gate"
    - "build.bat installer size check resolves the actual installer produced regardless of version in MyAppVersion (no v1.0.0 hardcode)"
    - "build.bat emits explicit ERROR when Output\\ contains no installer (no silent 0 MB success)"
  artifacts:
    - path: "installer/ASLT-Setup.iss"
      provides: "Inno Setup script with extended [UninstallDelete] covering LocalAppData log path"
      contains: "{localappdata}\\ANNA\\ASLT\\logs"
    - path: "installer/build.bat"
      provides: "Build script with wildcard installer resolution and no file-count gate"
      contains: "ASLT-Setup-v*.exe"
  key_links:
    - from: "installer/ASLT-Setup.iss [UninstallDelete]"
      to: "%LOCALAPPDATA%\\ANNA\\ASLT\\logs (created at runtime by Services/LogService.cs after 84c498e)"
      via: "Type: filesandordirs; Name: \"{localappdata}\\ANNA\\ASLT\\logs\""
      pattern: "localappdata.*ANNA.ASLT.logs"
    - from: "installer/build.bat installer-size block"
      to: "installer/Output/ASLT-Setup-v{MyAppVersion}.exe (produced by ISCC from .iss OutputBaseFilename)"
      via: "wildcard for-loop over ASLT-Setup-v*.exe capturing path + size"
      pattern: "ASLT-Setup-v\\*\\.exe"
---

<objective>
커밋 84c498e(로그 경로를 {app}\logs → %LOCALAPPDATA%\ANNA\ASLT\logs로 이전) 이후 Codex 교차검증에서 드러난 3건의 후속 정리를 적용한다.

Purpose: 커밋이 선언한 "clean-uninstall + 무결성 검증" 계약을 실제 동작과 일치시키고, 향후 SDK/버전 변화에도 빌드가 거짓 양·음성 없이 통과하게 만든다.

Output:
- installer/ASLT-Setup.iss — [UninstallDelete] 섹션에 LocalAppData 로그 경로 3줄 추가
- installer/build.bat — 400 파일 카운트 게이트 삭제, 설치기 크기 점검 블록을 와일드카드 + 존재 가드 버전으로 교체

Scope boundary (DO NOT touch): Services/LogService.cs. 로그 경로 자체는 올바른 방향이며 수정하지 않는다.
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@C:\Users\ANNA\.claude\plans\humming-hugging-bear.md
@installer/ASLT-Setup.iss
@installer/build.bat

<interfaces>
<!-- Target code blocks already identified by line number in the authoritative spec.
     Executor should apply the exact replacements below — no exploration needed. -->

**Current state of installer/ASLT-Setup.iss lines 67-72:**
```
[UninstallDelete]
; D-15: Remove everything under <InstallDir> including logs/ — but NOT user JSON outside InstallDir
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\ffmpeg"
; Final sweep: any leftover files inside {app} that were created post-install
Type: dirifempty; Name: "{app}"
```

**Current state of installer/build.bat lines 49-55 (to be deleted):**
```
REM Count files: self-contained publish should produce 400+ files
for /f %%A in ('dir /a-d /b /s "%PUBLISH_DIR%" ^| find /c /v ""') do set FILE_COUNT=%%A
if %FILE_COUNT% LSS 400 (
  echo ERROR: Publish produced only %FILE_COUNT% files. Expected 400+ for self-contained build.
  exit /b 1
)
echo Publish verified: %FILE_COUNT% files present.
```

**Current state of installer/build.bat lines 64-75 (to be replaced):**
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

**Preserved gates (DO NOT delete):**
- build.bat lines 36-47: Name-based required-file checks for ASLTv1.exe, coreclr.dll, OpenCvSharpExtern.dll — these remain the correctness gate after Fix 2.
- build.bat line 57-62: ISCC compile step (unchanged).
- ASLT-Setup.iss line 69: `Type: filesandordirs; Name: "{app}\logs"` — keep as legacy defense for users upgrading from pre-84c498e builds.
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add LocalAppData log cleanup to installer uninstall section (CODEX-P1)</name>
  <files>installer/ASLT-Setup.iss</files>
  <action>
Edit installer/ASLT-Setup.iss. Replace the existing [UninstallDelete] block (lines 67-72) with the expanded version below that adds LocalAppData cleanup while preserving the legacy {app}\logs entry for pre-84c498e upgraders.

New [UninstallDelete] block (exact replacement):
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

Rationale (carry into commit body if useful):
- Inno Setup {localappdata} resolves to current uninstaller user profile — covers single-user install/uninstall.
- dirifempty on {localappdata}\ANNA\ASLT and {localappdata}\ANNA is safe when other ANNA products coexist.
- Keep {app}\logs line for defensive cleanup of upgraders from older builds.

Do NOT touch [Files], [Setup], [Icons], [Run], [Languages], [Tasks] sections. Do NOT regenerate AppId. Do NOT modify Services/LogService.cs (explicit scope boundary per humming-hugging-bear.md).

Commit guidance (atomic, independent from Task 2/3):
- Message: `fix(installer): remove LocalAppData logs on uninstall (clean-uninstall contract)`
- Staged files: installer/ASLT-Setup.iss only
  </action>
  <verify>
    <automated>grep -n "localappdata.*ANNA.ASLT.logs" installer/ASLT-Setup.iss &amp;&amp; grep -n "dirifempty.*localappdata.*ANNA.ASLT\"" installer/ASLT-Setup.iss &amp;&amp; grep -n "dirifempty.*localappdata.*ANNA\"" installer/ASLT-Setup.iss &amp;&amp; grep -n "filesandordirs.*{app}.logs" installer/ASLT-Setup.iss</automated>
  </verify>
  <done>
installer/ASLT-Setup.iss [UninstallDelete] contains four cleanup lines: {app}\logs (legacy, preserved), {app}\ffmpeg (existing), {localappdata}\ANNA\ASLT\logs (new filesandordirs), {localappdata}\ANNA\ASLT (new dirifempty), {localappdata}\ANNA (new dirifempty), {app} (existing dirifempty). Inno Setup script still parses (no ISCC syntax errors when compiled).
  </done>
</task>

<task type="auto">
  <name>Task 2: Remove 400-file-count gate from build.bat (CODEX-P2)</name>
  <files>installer/build.bat</files>
  <action>
Edit installer/build.bat. Delete lines 49-55 (the REM comment, the `for /f %%A` file-count loop, the `if %FILE_COUNT% LSS 400` block, and the `echo Publish verified: %FILE_COUNT% files present.` line).

Target block to remove (exact, verbatim):
```
REM Count files: self-contained publish should produce 400+ files
for /f %%A in ('dir /a-d /b /s "%PUBLISH_DIR%" ^| find /c /v ""') do set FILE_COUNT=%%A
if %FILE_COUNT% LSS 400 (
  echo ERROR: Publish produced only %FILE_COUNT% files. Expected 400+ for self-contained build.
  exit /b 1
)
echo Publish verified: %FILE_COUNT% files present.
```

DO NOT touch the preceding name-based checks (lines 36-47 — ASLTv1.exe, coreclr.dll, OpenCvSharpExtern.dll existence). They remain the sole correctness gate for publish-output integrity and that is intentional.

DO NOT touch `echo [4/5] Verifying publish output integrity...` (the step 4 header) or `echo [5/5] Compiling Inno Setup script...` (the step 5 header). After deletion, step 4 should flow directly from the three name-based checks into step 5 with no file-count noise between them.

Rationale (per humming-hugging-bear.md): File count is not a correctness property — it tracks SDK/trimming/native-layout implementation details. Name-based gates already verify core self-contained payload presence. The numeric gate only produces false negatives when the SDK changes layout.

Commit guidance (atomic, independent from Task 1/3):
- Message: `fix(build): drop brittle 400-file-count gate — keep name-based integrity checks`
- Staged files: installer/build.bat only
  </action>
  <verify>
    <automated>! grep -n "FILE_COUNT" installer/build.bat &amp;&amp; ! grep -n "LSS 400" installer/build.bat &amp;&amp; ! grep -n "find /c /v" installer/build.bat &amp;&amp; grep -n "ASLTv1.exe missing" installer/build.bat &amp;&amp; grep -n "coreclr.dll missing" installer/build.bat &amp;&amp; grep -n "OpenCvSharpExtern.dll missing" installer/build.bat</automated>
  </verify>
  <done>
installer/build.bat no longer contains FILE_COUNT, `LSS 400`, or `find /c /v ""` tokens. The three name-based required-file checks (ASLTv1.exe, coreclr.dll, OpenCvSharpExtern.dll) are preserved verbatim. Running `installer\build.bat` on a healthy publish still succeeds; the log line `Publish verified: N files present.` no longer appears.
  </done>
</task>

<task type="auto">
  <name>Task 3: Replace hardcoded installer filename with wildcard + existence guard (CODEX-P3)</name>
  <files>installer/build.bat</files>
  <action>
Edit installer/build.bat. Replace the installer-size sanity block (lines 64-75, i.e. from the `REM Installer size sanity check ...` comment through the end including `endlocal`) with the wildcard-based version below.

NOTE: By the time this task runs, Task 2 may have already modified the file and shifted line numbers. The block to replace is identifiable by its leading comment `REM Installer size sanity check (expected 90-150 MB with LZMA2 solid compression)` and ends with `endlocal`. Use content-based matching, not fixed line numbers.

Exact replacement block:
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

Key behavioral changes:
1. `ASLT-Setup-v1.0.0.exe` → wildcard `ASLT-Setup-v*.exe` — decouples build.bat from the `MyAppVersion` value in ASLT-Setup.iss. Since build.bat line 24 wipes `Output\` at start and Inno Setup's `OutputBaseFilename=ASLT-Setup-v{#MyAppVersion}` produces exactly one file, the wildcard always resolves to the unique current installer.
2. INSTALLER_PATH captured inside the for-loop (alongside INSTALLER_SIZE) so the final `echo Installer:` reports the actual filename rather than a stale hardcode.
3. New `if not defined INSTALLER_PATH` guard — explicit ERROR exit when ISCC silently produces nothing, replacing the prior "0 MB success" silent regression mode.

DO NOT touch anything before the `REM Installer size sanity check` comment — the step 5 ISCC compile block (`%ISCC% "%~dp0ASLT-Setup.iss"` and its errorlevel check) must remain unchanged.

Commit guidance (atomic, independent from Task 1/2):
- Message: `fix(build): resolve installer by wildcard + guard against missing Output (version-agnostic)`
- Staged files: installer/build.bat only
  </action>
  <verify>
    <automated>grep -n "ASLT-Setup-v\*\.exe" installer/build.bat &amp;&amp; grep -n "INSTALLER_PATH=%%F" installer/build.bat &amp;&amp; grep -n "if not defined INSTALLER_PATH" installer/build.bat &amp;&amp; grep -n "echo Installer: %INSTALLER_PATH%" installer/build.bat &amp;&amp; ! grep -n "ASLT-Setup-v1\.0\.0\.exe" installer/build.bat</automated>
  </verify>
  <done>
installer/build.bat contains: (1) wildcard for-loop over `"%~dp0Output\ASLT-Setup-v*.exe"` capturing both INSTALLER_PATH and INSTALLER_SIZE, (2) `if not defined INSTALLER_PATH` existence guard that exits with ERROR, (3) final `echo Installer: %INSTALLER_PATH%` referencing the resolved path. No remaining `ASLT-Setup-v1.0.0.exe` hardcoded strings anywhere in the file. Running the full build.bat on a successful Inno Setup compile prints `Installer: <full path to actual produced installer>` with a non-zero MB value.
  </done>
</task>

</tasks>

<verification>
Plan-level checks after all three tasks complete:

1. **Static compile (installer/ASLT-Setup.iss):** From a Windows shell, run `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" C:\Users\ANNA\AOLTv1.0\installer\ASLT-Setup.iss` and confirm zero errors. The new [UninstallDelete] entries must parse as valid Inno Setup directives.

2. **Full build smoke test (installer/build.bat):** Run `C:\Users\ANNA\AOLTv1.0\installer\build.bat` from a clean state. Expected log progression:
   - `[4/5] Verifying publish output integrity...` followed directly by `[5/5] Compiling Inno Setup script...` (no file-count lines in between).
   - Final output `Installer: C:\Users\ANNA\AOLTv1.0\installer\Output\ASLT-Setup-v1.0.0.exe` with a valid MB value (not `0 MB`).

3. **Version-agnosticism sanity (optional manual):** Temporarily change `#define MyAppVersion "1.0.0"` → `"1.0.99"` in ASLT-Setup.iss and re-run full build.bat. Expected: final `Installer: ...\ASLT-Setup-v1.0.99.exe` with valid size. Revert the change. (Only valid when running full build.bat — re-running ISCC alone does not exercise the size block.)

4. **Clean-uninstall end-to-end (manual, post-release candidate only):**
   - Install built .exe into default path.
   - Launch ASLT, let it run a few seconds so LogService creates `%LOCALAPPDATA%\ANNA\ASLT\logs\AOLT-yyyy-MM-dd.log`.
   - Run uninstaller via Control Panel.
   - Confirm: `C:\Program Files\ANNA\ASLT` removed (existing contract), `%LOCALAPPDATA%\ANNA\ASLT\logs` removed (new contract), `%LOCALAPPDATA%\ANNA` removed if empty (dirifempty).
</verification>

<success_criteria>
- [ ] installer/ASLT-Setup.iss [UninstallDelete] has three new lines targeting {localappdata}\ANNA\ASLT\logs (filesandordirs), {localappdata}\ANNA\ASLT (dirifempty), {localappdata}\ANNA (dirifempty) — existing {app}\logs, {app}\ffmpeg, {app} entries preserved.
- [ ] installer/build.bat no longer contains any FILE_COUNT variable, `LSS 400` comparison, or `find /c /v ""` construct.
- [ ] installer/build.bat no longer contains the string `ASLT-Setup-v1.0.0.exe` anywhere — only `ASLT-Setup-v*.exe` wildcard.
- [ ] installer/build.bat contains `if not defined INSTALLER_PATH` guard that exits with ERROR.
- [ ] installer/build.bat final `echo Installer:` line references `%INSTALLER_PATH%` (not a hardcoded path).
- [ ] Three separate atomic commits produced (one per fix) so they can be cherry-picked or reverted independently.
- [ ] Services/LogService.cs unchanged.
- [ ] ISCC compile succeeds; full build.bat run prints valid `Installer: <path>` with non-zero MB.
</success_criteria>

<output>
After completion, create `.planning/quick/260421-mzz-codex-installer-uninstall-build-bat/260421-mzz-SUMMARY.md` documenting:
- Which lines changed in each file (before/after snippets).
- Three commit SHAs (one per fix) in order.
- Result of ISCC compile and full build.bat dry-run verification (A + B in humming-hugging-bear.md §검증).
- Explicit confirmation that Services/LogService.cs was NOT modified and that the legacy {app}\logs uninstall entry is preserved.
- Any deviations from humming-hugging-bear.md (expected: none).
</output>
