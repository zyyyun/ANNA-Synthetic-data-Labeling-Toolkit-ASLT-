---
quick_id: 260512-ifn
type: quick
description: perf instrumentation for video hot paths
status: completed
completed_at: 2026-05-12
duration_minutes: 3
commits:
  - 1ea2e46  # feat(perf): add PerfLog toggle flag + F12 keyboard shortcut
  - 8fd294e  # feat(perf): instrument VideoService.LoadFrame decode + ToBitmap latency
  - 0e8a8d0  # feat(perf): instrument MainForm pictureBoxVideo_Paint + MouseMove
files_created:
  - Helpers/PerfLog.cs
files_modified:
  - Forms/MainForm.cs
  - Services/VideoService.cs
verification: dotnet build ASLTv1.0.csproj -c Debug — 0 errors, 42 pre-existing CS8632 warnings
---

# Quick Task 260512-ifn: Perf Instrumentation for Video Hot Paths Summary

## One-liner

런타임 토글 가능한 PerfLog (`Enabled=false` 기본) 와 가드된 Stopwatch 측정을 `VideoService.LoadFrame` 디코드/ToBitmap 와 `MainForm.pictureBoxVideo_Paint`/`MouseMove` 핫패스에 삽입 — F12 단축키로 ON/OFF 전환, Serilog Debug 로 ms 단위 경과 시간 출력. 정상 사용자 경로 무영향 (토글 OFF 시 Stopwatch 미생성).

## What changed

- **`Helpers/PerfLog.cs`** (신규): 단일 책임 정적 클래스 — `public static bool Enabled { get; set; } = false`. 기본 비활성.
- **`Services/VideoService.cs::LoadFrame`**: `using ASLTv1.Helpers;` 추가. 첫 `videoCapture.Read` 호출과 `BitmapConverter.ToBitmap` 호출 각각을 `PerfLog.Enabled ? Stopwatch.StartNew() : null` 삼항으로 가드해 측정. 측정 종료 후 `Log.Debug("[PERF] LoadFrame f={Frame} {W}x{H} decode={DecodeMs}ms toBmp={ToBmpMs}ms skipSeek={SkipSeek}", ...)` 한 줄 출력. retry loop 는 의도된 fallback 으로 측정 제외. try-catch 구조와 FrameChanged 이벤트는 미변경.
- **`Forms/MainForm.cs`**:
  - `using System.Diagnostics;` 추가 (line 3).
  - `#region Fields` 영역에 `_perfMouseMoveInvalidates`, `_perfMouseMoveLastFlushTicks`, `_perfMouseMoveLastModeTag` 필드 3개 추가 (line 100-103).
  - `pictureBoxVideo_Paint` 가 try-finally 로 감싸짐 — 조기 return 가드(`pictureBoxVideo.Image == null`)는 try 바깥 유지, finally 에서 `swPaint != null` 가드 후 `Log.Debug("[PERF] Paint f={Frame} boxes={N} elapsed={Ms}ms pbSize={W}x{H} imgSize={IW}x{IH}", ...)` 출력. 페인트 결과 자체는 계측 실패와 무관.
  - 신규 private 헬퍼 `PerfRecordMouseMoveInvalidate(string modeTag)` 추가 — 1초 윈도우 누적 카운트 + flush + 리셋.
  - `pictureBoxVideo_MouseMove` 의 세 `Invalidate()` 호출 (drawing/resize/drag 분기) 직전에 헬퍼 호출 삽입.
  - `MainForm_KeyDown` 의 F1/F2/F3 분기 옆에 F12 분기 추가 — `PerfLog.Enabled = !PerfLog.Enabled` 토글 + `Log.Information("[PERF] toggle Enabled={Enabled}", ...)` audit + 모달 MessageBox 피드백 ("ON"/"OFF") + `e.Handled = true; return;` 로 후속 분기 차단.

## Verification

```
dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal
```

**결과**: 0 errors, 42 warnings (전부 pre-existing CS8632 nullable 주석 — `<Nullable>disable</Nullable>` 설정에서 발생하는 codebase-wide 패턴; 새로 추가된 `Stopwatch?` 1건도 동일한 기존 패턴을 따름).

## Static greps (Definition of Done)

| Pattern | Expected | Actual |
| --- | --- | --- |
| `internal static class PerfLog` in Helpers/PerfLog.cs | 1 | 1 |
| `public static bool Enabled { get; set; } = false` in Helpers/PerfLog.cs | 1 | 1 |
| `PerfLog.Enabled ? Stopwatch.StartNew()` in Services/VideoService.cs | 2 | 2 |
| `[PERF] LoadFrame` in Services/VideoService.cs | 1 | 1 |
| `[PERF] Paint` + `[PERF] MouseMove invalidates` + `PerfRecordMouseMoveInvalidate` (decl + 3 calls) + `Keys.F12` + toggle in Forms/MainForm.cs | 8 lines (1+1+4+1+1) | 8 |

## Success Criteria Status

- [x] `Helpers/PerfLog.cs` 존재, `Enabled` 정적 bool, 기본 false.
- [x] `VideoService.LoadFrame` 안에 Stopwatch 2개 (decode, toBmp) + 통합 Log.Debug 1줄, 모두 PerfLog.Enabled 가드.
- [x] `MainForm.pictureBoxVideo_Paint` 가 try-finally 로 감싸지고 finally 에 PerfLog.Enabled 가드된 Log.Debug.
- [x] `MainForm.pictureBoxVideo_MouseMove` 의 drawing/resize/drag 분기 Invalidate 직전에 PerfRecordMouseMoveInvalidate 호출, 1초 윈도우로 통합 카운트 출력.
- [x] F12 단축키로 PerfLog.Enabled 토글 + MessageBox 시각 피드백 + Log.Information audit.
- [x] `dotnet build` 성공, 신규 에러 0.
- [x] PerfLog.Enabled=false 기본 — 정상 사용자 경로 무영향 (Stopwatch 미생성, Log 미출력).
- [x] 3개 atomic git commit (PerfLog+F12, VideoService 계측, MainForm 계측).

## Deviations from Plan

None — plan executed exactly as written. All Task 1-3 action steps applied verbatim with the exact code blocks supplied in `<interfaces>` and per-task `<action>` sections.

## CLAUDE.md Compliance

- **Tech stack**: C# .NET 8.0 WinForms 유지 — 신규 의존성 없음, 기존 코드 기반 개선만.
- **Defects**: Critical/High 결함 0건 유지 — 계측은 try-finally 로 정상 페인트 흐름 보호, Stopwatch 가드 (삼항) 로 OFF 시 인스턴스 생성 자체 없음.
- **Security**: 진단 로그만 — KISA 가이드/암호화 영향 없음. PerfLog 출력은 `Log.Debug` (Serilog), 감사 로그 (`Log.Information [AUDIT]`) 와 분리.
- **정상 사용자 경로 무영향**: `PerfLog.Enabled=false` 기본 → 모든 측정 분기 통과 안 함 → 기존 LoadFrame/Paint/MouseMove 동작·성능 동일.

## Manual Smoke (사용자 수행 권장)

1. 빌드 산출물 실행.
2. F12 누르면 MessageBox "PerfLog: ON" 표시. 닫음.
3. 의심 영상 로드 → 프레임 이동/재생 → 로그 파일에 `[PERF] LoadFrame ...` 라인 다수 기록.
4. 박스를 마우스로 그리기/리사이즈/드래그 → 진행 중 1초마다 `[PERF] MouseMove invalidates=N/sec mode=...` 한 줄씩.
5. 페인트 발생할 때마다 `[PERF] Paint ...` 라인 기록.
6. F12 다시 눌러 OFF → MessageBox "PerfLog: OFF" → 이후 [PERF] 라인 더 이상 출력 안 됨.
7. 토글 OFF 인 정상 상태로 영상 재생 → 기존 동작 동일.

## Self-Check: PASSED

- File `Helpers/PerfLog.cs` exists (verified)
- File `Services/VideoService.cs` modified (verified — using ASLTv1.Helpers added, 2 Stopwatch guards + 1 Log.Debug)
- File `Forms/MainForm.cs` modified (verified — using System.Diagnostics added, 3 fields, F12 toggle, try-finally Paint wrapper, PerfRecordMouseMoveInvalidate helper + 3 call sites)
- Commit `1ea2e46` exists (verified via `git log`)
- Commit `8fd294e` exists (verified via `git log`)
- Commit `0e8a8d0` exists (verified via `git log`)
- `dotnet build` succeeded with 0 errors
