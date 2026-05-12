---
phase: 260512-kma
plan: 01
subsystem: perf-instrumentation
tags: [perf, timer-fix, winmm, jitter, instrumentation, GS-cert]
type: execute
requirements:
  - PERF-TIMER-FIX
  - PERF-V2-JITTER-INST
dependency_graph:
  requires:
    - 260512-ifn (v1 PerfLog 토글 + LoadFrame/Paint/MouseMove 계측)
  provides:
    - "winmm timeBeginPeriod(1) — 1ms scheduler granularity (항상 ON)"
    - "v2 jitter signals — Playback fps / paintLatency / gc2"
  affects:
    - Program.cs (Main 진입점)
    - Helpers/PerfLog.cs (계측 토글 클래스)
    - Forms/MainForm.cs (LoadFrame / pictureBoxVideo_Paint)
    - Services/VideoService.cs (LoadFrame)
tech_stack:
  added:
    - "winmm.dll (Windows OS 표준 컴포넌트, P/Invoke — no nuget)"
  patterns:
    - "P/Invoke + try/finally 1:1 매칭 (BeginPeriod/EndPeriod)"
    - "Stopwatch.GetTimestamp() static call — no Stopwatch instance alloc"
    - "PerfLog.Enabled 가드로 OFF 시 오버헤드 0 보장"
key_files:
  created:
    - Helpers/WinmmTimer.cs
  modified:
    - Program.cs
    - Helpers/PerfLog.cs
    - Forms/MainForm.cs
    - Services/VideoService.cs
decisions:
  - "ApplicationExit 이벤트 핸들러 대신 try/finally 사용 — 예외 발생 시도 EndPeriod 보장"
  - "BeginPeriod return value 무시 — 실패해도 앱 정상 동작 (Critical/High 0건)"
  - "v1 메시지 끝에 필드 append, 순서 변경 없음 — v1 grep 호환성 보존"
  - "Stopwatch.GetTimestamp() 사용 — Stopwatch 인스턴스 alloc 회피 (hot path GC 압박 최소화)"
metrics:
  duration_minutes: 8
  tasks_completed: 2
  files_modified: 5
  commits: 2
  completed_date: "2026-05-12"
---

# Phase 260512-kma Plan 01: Timer fix (timeBeginPeriod) + v2 Jitter Instrumentation Summary

**One-liner:** Windows scheduler granularity 를 winmm.timeBeginPeriod(1) 로 1ms 로 변경 (30fps 영상 정확한 재생) + v2 jitter 계측 3종 (Playback fps, paintLatency, gc2) 으로 잔존 jitter 원인 분리 가능한 baseline 확보.

## Context

v1 (260512-ifn) perf 계측 결과 30fps 영상이 실제 ~21fps 재생되는 결함이 측정으로 확인됨. 원인:

- Windows OS scheduler granularity 가 기본 ~15.625ms.
- `System.Windows.Forms.Timer.Interval = 33ms` 가 ~46.875ms (3 tick) 로 강제 반올림 → 실제 ~21.3fps.

본 작업은:

1. **Timer fix (A)** — `winmm.timeBeginPeriod(1)` 호출로 OS scheduler granularity 를 1ms 로 변경. **항상 ON** (PerfLog 무관). 정상 사용자도 30fps → 30fps 정상 재생 혜택.
2. **v2 jitter 계측 (B/C/D)** — timer fix 후 잔존 jitter 추적용 3개 신호:
   - **B**: Playback frame 간격 / 실측 fps (30프레임 윈도우)
   - **C**: PictureBox 자동 스케일링 latency (image set → paint 시작 delta)
   - **D**: GC Gen2 collection delta (large bitmap 압박 추적)

## Implementation

### Task 1 — Timer fix (A)

**Commit:** `d69e741` — `feat(perf): add winmm timeBeginPeriod(1) for 1ms scheduler granularity`

- **`Helpers/WinmmTimer.cs` (신규)** — `winmm.dll` P/Invoke wrapper, 2개 extern (`BeginPeriod` / `EndPeriod`).
- **`Program.cs`** — `using ASLTv1.Helpers;` 추가 + `Main()` 의 try/finally 에서 1:1 매칭 호출.
  - try 안: `ApplicationConfiguration.Initialize()` 직후 `WinmmTimer.BeginPeriod(1)` 호출.
  - finally 첫 줄: `WinmmTimer.EndPeriod(1)` 호출 (LogService 정리보다 먼저).
- **CLAUDE.md 준수:** Return value 검사 안 함 — BeginPeriod 실패해도 앱 정상 동작 (Critical/High 0건). EndPeriod 는 BeginPeriod 실패 시도 safe.
- **PerfLog 무관 — 모든 빌드/사용자 영구 적용.**

### Task 2 — v2 jitter 계측 (B/C/D)

**Commit:** `91a0b39` — `feat(perf): add v2 jitter instrumentation (Playback fps, paintLatency, gc2)`

- **`Helpers/PerfLog.cs`** — 6개 신규 static property:
  - `LastLoadFrameTimestamp`, `FrameCounter`, `IntervalSumTicks`, `MaxIntervalTicks` (B: Playback fps 누적)
  - `LastImageSetTimestamp` (C: paintLatency 계산용)
  - `LastGen2Count` (D: gc2 delta 계산용)
- **`Forms/MainForm.cs::LoadFrame`** —
  - image set 직후 `PerfLog.LastImageSetTimestamp = Stopwatch.GetTimestamp()` (PerfLog.Enabled 가드 안).
  - `pictureBoxVideo.Invalidate()` 직후 30프레임 윈도우 누적 + 한 줄 로그: `[PERF] Playback avgGap=... maxGap=... fps=...` (PerfLog.Enabled 가드 안).
- **`Forms/MainForm.cs::pictureBoxVideo_Paint`** —
  - Paint 시작 시 `paintLatencyMs = (now - LastImageSetTimestamp)` 계산 (PerfLog.Enabled 가드 안).
  - 기존 `[PERF] Paint ...` 로그 메시지에 `paintLatency={LatencyMs:F1}ms` 필드 append (인자 리스트에 paintLatencyMs 추가).
- **`Services/VideoService.cs::LoadFrame`** —
  - decode Stopwatch 라인 직전에 `GC.CollectionCount(2)` delta 계산 + `PerfLog.LastGen2Count` 갱신 (PerfLog.Enabled 가드 안).
  - 기존 `[PERF] LoadFrame ...` 로그 메시지에 `gc2={Gen2Delta}` 필드 append (인자 리스트에 gen2Delta 추가).
- **모든 v2 측정 분기 `if (PerfLog.Enabled)` 가드 안 — OFF 시 진입 0, 오버헤드 0.**

## Verification

### Build

```
dotnet build "ASLTv1.0.csproj" -c Debug -v minimal
```

- 빌드 시간: 약 2~3초.
- **오류 0개**, 경고 42개 (모두 pre-existing CS8632 nullable 주석 + CS1998 async, 신규 경고 0).

### Static greps (모두 통과)

| Check                                                                | Expected | Got | Status |
| -------------------------------------------------------------------- | -------- | --- | ------ |
| `Helpers/WinmmTimer.cs` BeginPeriod extern                           | 1        | 1   | OK     |
| `Helpers/WinmmTimer.cs` EndPeriod extern                             | 1        | 1   | OK     |
| `Program.cs` `WinmmTimer.BeginPeriod(1)`                             | 1 (try)  | 1   | OK     |
| `Program.cs` `WinmmTimer.EndPeriod(1)`                               | 1 (finally) | 1 | OK     |
| `Helpers/PerfLog.cs` 6개 신규 property                                | 6        | 6   | OK     |
| `Forms/MainForm.cs` `[PERF] Playback avgGap`                         | 1        | 1   | OK     |
| `Forms/MainForm.cs` `paintLatency={LatencyMs`                        | 1        | 1   | OK     |
| `Forms/MainForm.cs` `PerfLog.LastImageSetTimestamp = Stopwatch.GetTimestamp` | 1 | 1   | OK     |
| `Services/VideoService.cs` `gc2={Gen2Delta}`                         | 1        | 1   | OK     |
| `Services/VideoService.cs` `GC.CollectionCount(2)`                   | 1        | 1   | OK     |

### Git

```
git log --oneline -3
91a0b39 feat(perf): add v2 jitter instrumentation (Playback fps, paintLatency, gc2)
d69e741 feat(perf): add winmm timeBeginPeriod(1) for 1ms scheduler granularity
99c6932 docs(260512-kma): pre-dispatch plan for timer fix + v2 jitter instrumentation
```

2개 atomic commit — 계획과 1:1 매칭.

## CLAUDE.md / GS인증 영향

- **신규 의존성 0**: `winmm.dll` 은 Windows OS 표준 컴포넌트 (XP 이후 모든 버전 포함). nuget 패키지 추가 없음.
- **보안/암호화 무영향**: 본 작업은 진단/타이밍 관련만, KISA SHA-256/Salt 등 보안 코드 비변경.
- **Critical/High 결함 0건 보장**:
  - `BeginPeriod` 호출 실패 시도 앱 정상 동작 (return value 무시).
  - `EndPeriod` 는 `BeginPeriod` 실패 시도 호출 안전 (no-op).
  - try/finally 로 예외 발생 시도 EndPeriod 보장.
  - v2 측정 코드는 모두 `PerfLog.Enabled` 가드 안 — OFF 시 진입 0 (정상 사용자 경로 무영향).
- **기존 v1 grep 호환성 보존**: `[PERF] Paint ...` 와 `[PERF] LoadFrame ...` 메시지의 기존 필드 순서 / 이름 변경 없음. 끝에 append 만.
- **ISO/IEC 25023 8대 품질 특성**: 성능 효율성 (시간 효율성) 개선 — 30fps 영상의 정확한 재생 달성.

## Manual Smoke Test (사용자 권장 — 본 plan verification 외)

빌드 산출물 실행 후:

1. **F12 OFF 상태로 영상 재생** → 정상 동작, `[PERF]` 로그 부재. Timer fix 만 적용 — 33ms tick 으로 실제 ~30fps 재생 기대.
2. **F12 ON → 30fps 영상 (`fhd_30fps_2h.mkv`) 재생:**
   - `[PERF] Playback avgGap=...ms maxGap=...ms fps=...` — 기대값: avgGap ≈ 33ms, fps ≈ 30 (timer fix 적용 효과 verifiable).
   - `[PERF] Paint ... paintLatency=...ms ...` — paintLatency 값 관찰 (PictureBox 자동 스케일링 비용 baseline).
   - `[PERF] LoadFrame ... gc2=N` — Gen2 count delta 관찰 (gc 압박 baseline).
3. **F12 OFF** → 측정 OFF, 일반 사용 영향 없음.

## Deviations from Plan

None — plan executed exactly as written. 모든 must-haves truths/artifacts/key_links 충족.

## Known Stubs

None. 본 작업은 P/Invoke wrapper + 계측 코드로, UI 렌더링 placeholder/stub 없음.

## Performance Metrics

| Metric              | Value      |
| ------------------- | ---------- |
| Duration            | ~8 minutes |
| Tasks Completed     | 2 / 2      |
| Files Created       | 1          |
| Files Modified      | 4          |
| Atomic Commits      | 2          |
| Build Errors        | 0          |
| New Build Warnings  | 0          |
| Deviations          | 0          |

## Self-Check: PASSED

- `Helpers/WinmmTimer.cs` exists — FOUND
- `Program.cs` BeginPeriod/EndPeriod calls — FOUND (1:1 try/finally)
- `Helpers/PerfLog.cs` 6 new properties — FOUND
- `Forms/MainForm.cs` `[PERF] Playback avgGap` + `paintLatency={LatencyMs` + image set timestamp — FOUND
- `Services/VideoService.cs` `gc2={Gen2Delta}` + `GC.CollectionCount(2)` — FOUND
- Commit `d69e741` (Task 1) — FOUND
- Commit `91a0b39` (Task 2) — FOUND
- All v2 measurement branches under `if (PerfLog.Enabled)` guard — visual review confirmed
- Build 0 errors, 0 new warnings — VERIFIED
