---
phase: 260512-kma
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Helpers/WinmmTimer.cs
  - Program.cs
  - Helpers/PerfLog.cs
  - Forms/MainForm.cs
  - Services/VideoService.cs
autonomous: true
requirements:
  - PERF-TIMER-FIX
  - PERF-V2-JITTER-INST
must_haves:
  truths:
    - "앱 시작 시 winmm timeBeginPeriod(1) 호출되어 OS scheduler granularity 가 1ms 로 변경됨 (항상 ON, PerfLog 무관)"
    - "앱 종료 시 timeEndPeriod(1) 가 BeginPeriod 와 1:1 매칭으로 호출됨"
    - "PerfLog.Enabled=true 상태에서 30프레임마다 Playback avgGap/maxGap/fps 한 줄이 Serilog Debug 로 출력됨"
    - "PerfLog.Enabled=true 상태에서 Paint 로그 끝에 paintLatency 필드가 ms 단위로 출력됨"
    - "PerfLog.Enabled=true 상태에서 VideoService LoadFrame 로그 끝에 gc2 필드 (Gen2 collection delta) 가 출력됨"
    - "PerfLog.Enabled=false 기본 상태에서는 B/C/D 측정 코드 분기 진입 안 함 (오버헤드 0)"
    - "dotnet build Debug 0 errors, 신규 에러/경고 0"
  artifacts:
    - path: "Helpers/WinmmTimer.cs"
      provides: "winmm.dll timeBeginPeriod / timeEndPeriod P/Invoke wrapper"
      contains: "internal static class WinmmTimer"
    - path: "Program.cs"
      provides: "Application 진입점에서 BeginPeriod(1) 호출 + 종료 매칭"
      contains: "WinmmTimer.BeginPeriod"
    - path: "Helpers/PerfLog.cs"
      provides: "v2 jitter 계측용 6개 static property (LastLoadFrameTimestamp/FrameCounter/IntervalSumTicks/MaxIntervalTicks/LastImageSetTimestamp/LastGen2Count)"
      contains: "LastLoadFrameTimestamp"
    - path: "Forms/MainForm.cs"
      provides: "LoadFrame frame 간격 누적 + 30프레임마다 Playback 로그 + image set timestamp + Paint 로그에 paintLatency 필드"
      contains: "[PERF] Playback avgGap"
    - path: "Services/VideoService.cs"
      provides: "LoadFrame perf 로그에 gc2 필드 추가"
      contains: "gc2"
  key_links:
    - from: "Program.cs::Main"
      to: "WinmmTimer.BeginPeriod / EndPeriod"
      via: "ApplicationConfiguration.Initialize 직후 BeginPeriod(1), Application.Run 후 finally 에서 EndPeriod(1)"
      pattern: "WinmmTimer\\.(Begin|End)Period\\(1\\)"
    - from: "Forms/MainForm.cs::LoadFrame"
      to: "PerfLog 카운터 (LastLoadFrameTimestamp/FrameCounter/...)"
      via: "PerfLog.Enabled 가드 안에서 Stopwatch.GetTimestamp() delta 누적"
      pattern: "PerfLog\\.LastLoadFrameTimestamp"
    - from: "Forms/MainForm.cs::pictureBoxVideo_Paint"
      to: "PerfLog.LastImageSetTimestamp"
      via: "LoadFrame 의 Image 세팅 직후 timestamp 저장 → Paint 시작 시 delta 계산"
      pattern: "paintLatency=\\{LatencyMs"
    - from: "Services/VideoService.cs::LoadFrame"
      to: "PerfLog.LastGen2Count"
      via: "PerfLog.Enabled 가드 안에서 GC.CollectionCount(2) delta 계산 후 로그에 gc2 필드 append"
      pattern: "gc2=\\{Gen2Delta\\}"
---

<objective>
v1 perf 계측 (260512-ifn) 결과 30fps 영상이 실제 ~21fps 로 재생되는 결함이 확인됨. 원인은 Windows OS scheduler granularity (~15.625ms) 가 `System.Windows.Forms.Timer.Interval = 33` 을 ~46.875ms 로 강제 반올림하는 것. 본 작업은:

1. **Timer fix (A)** — `winmm.timeBeginPeriod(1)` 호출로 scheduler granularity 를 1ms 로 변경. 항상 ON (PerfLog 무관, 정상 사용자도 혜택).
2. **잔존 jitter 계측 (B/C/D)** — timer fix 후에도 남는 jitter 추적을 위해 v1 계측에 3개 신호 추가:
   - B: Playback frame 간격 / 실측 fps (30프레임 윈도우)
   - C: PictureBox 자동 스케일링 비용 (image set → paint 시작까지의 latency)
   - D: GC Gen2 collection delta (large bitmap 압박 시 GC stall 추적)

Purpose: timer fix 적용 후 30fps 영상이 실제 ~30fps 로 재생되는지 verifiable signal 확보 + 잔존 jitter 의 원인 (paintLatency vs GC2 압박 vs ...) 분리 가능한 baseline 제공. CLAUDE.md 의 GS인증 1등급 / Critical·High 결함 0건 원칙 준수 (정상 사용자 경로에 측정 오버헤드 0).

Output:
- `Helpers/WinmmTimer.cs` (신규, P/Invoke wrapper)
- `Program.cs` (BeginPeriod/EndPeriod 매칭 호출)
- `Helpers/PerfLog.cs` (6개 static property 추가)
- `Forms/MainForm.cs` (LoadFrame fps 카운터 + image set timestamp + Paint paintLatency 필드)
- `Services/VideoService.cs` (LoadFrame 로그에 gc2 필드)
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@./CLAUDE.md
@.planning/STATE.md
@.planning/quick/260512-ifn-perf-instrumentation-for-video-hot-paths/260512-ifn-SUMMARY.md
@Helpers/PerfLog.cs
@Program.cs

<interfaces>
<!-- Executor 가 그대로 사용. 코드베이스 탐색 불필요. -->

**현재 `Helpers/PerfLog.cs` 전체 (확장 대상):**
```csharp
namespace ASLTv1.Helpers
{
    /// <summary>
    /// 진단용 perf 로깅 토글. Release 빌드에서도 런타임에 ON/OFF 가능.
    /// 기본 false — 정상 사용자 경로에 영향 없음.
    /// 본 클래스는 진단 전용이며 보안/감사 로그와 별개.
    /// </summary>
    internal static class PerfLog
    {
        public static bool Enabled { get; set; } = false;
    }
}
```

**현재 `Program.cs::Main` 의 try 블록 (수정 대상):**
```csharp
try
{
    ApplicationConfiguration.Initialize();
    Application.Run(new MainForm());
}
finally
{
    LogService.AuditAppStop();
    LogService.CloseAndFlush();
}
```

**현재 `Forms/MainForm.cs::LoadFrame` (line 513~595, image set 부분):**
- line 523: `var bitmap = _videoService.LoadFrame(frameIndex);`
- line 524-528: `if (bitmap != null) { pictureBoxVideo.Image?.Dispose(); pictureBoxVideo.Image = bitmap; }`
  → image set 직후 `PerfLog.LastImageSetTimestamp = Stopwatch.GetTimestamp();` 삽입 위치 (PerfLog.Enabled 가드)
- LoadFrame 메서드 끝 직전 (UpdateTimeLabels/Invalidate 후) → Playback fps 누적 블록 삽입 (PerfLog.Enabled 가드)

**현재 `Forms/MainForm.cs::pictureBoxVideo_Paint` (line 1688~1758):**
- line 1692: `Stopwatch? swPaint = PerfLog.Enabled ? Stopwatch.StartNew() : null;`
- line 1745-1756: 기존 perf 로그 블록 — `[PERF] Paint f={Frame} boxes={N} elapsed={Ms}ms pbSize={W}x{H} imgSize={IW}x{IH}`
  → 이 메시지 끝에 `paintLatency={LatencyMs:F1}ms` 필드 추가
- paintLatency 계산은 `Stopwatch? swPaint = ...` 라인 직후 (PerfLog.Enabled 가드 안) 에 수행

**현재 `Services/VideoService.cs::LoadFrame` (line 202~):**
- line 229: `Stopwatch? swDecode = PerfLog.Enabled ? Stopwatch.StartNew() : null;`
  → 이 라인 직전에 `int gen2Delta = 0; if (PerfLog.Enabled) { int gen2Now = GC.CollectionCount(2); gen2Delta = gen2Now - PerfLog.LastGen2Count; PerfLog.LastGen2Count = gen2Now; }` 삽입
- line 261-265: 기존 perf 로그 블록 — `[PERF] LoadFrame f={Frame} {W}x{H} decode={DecodeMs}ms toBmp={ToBmpMs}ms skipSeek={SkipSeek}`
  → 끝에 `gc2={Gen2Delta}` 필드 추가, 인자 리스트에 `gen2Delta` 추가

**Imports 상태 (확인 완료):**
- `Forms/MainForm.cs` line 3: `using System.Diagnostics;` ✓
- `Forms/MainForm.cs` line 16: `using ASLTv1.Helpers;` ✓
- `Services/VideoService.cs`: `using ASLTv1.Helpers;` ✓ (v1 ifn 작업 시 추가됨)
- `Services/VideoService.cs`: `using System.Diagnostics;` ✓
- `Program.cs`: `using ASLTv1.Helpers;` 필요 (현재 없음 — Task 1 에서 추가)
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Timer fix (A) — WinmmTimer P/Invoke wrapper + Program.cs 매칭 호출</name>
  <files>Helpers/WinmmTimer.cs, Program.cs</files>
  <action>
**Step 1.1 — `Helpers/WinmmTimer.cs` 신규 생성:**
정확히 아래 내용으로 (다른 코멘트나 추가 멤버 금지):

```csharp
using System.Runtime.InteropServices;

namespace ASLTv1.Helpers
{
    /// <summary>
    /// winmm.dll timeBeginPeriod / timeEndPeriod P/Invoke wrapper.
    /// Windows OS scheduler granularity 를 1ms 로 변경 (기본 ~15.625ms).
    /// System.Windows.Forms.Timer 등 코어 타이밍의 정확도 확보용 (미디어/게임 앱 표준 패턴).
    /// BeginPeriod 호출 횟수와 EndPeriod 호출 횟수는 반드시 1:1 매칭되어야 함.
    /// PerfLog 와 무관 — 정상 사용자도 적용됨 (30fps 영상의 정확한 재생).
    /// </summary>
    internal static class WinmmTimer
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        internal static extern uint BeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        internal static extern uint EndPeriod(uint uMilliseconds);
    }
}
```

**Step 1.2 — `Program.cs` 수정:**

(a) 파일 상단 using 영역에 `using ASLTv1.Helpers;` 추가 (이미 존재하면 skip).

(b) `Main()` 의 `try { ApplicationConfiguration.Initialize(); ... } finally { ... }` 블록을 아래와 같이 변경:

기존 (line 37-46):
```csharp
try
{
    ApplicationConfiguration.Initialize();
    Application.Run(new MainForm());
}
finally
{
    LogService.AuditAppStop();
    LogService.CloseAndFlush();
}
```

변경 후:
```csharp
try
{
    ApplicationConfiguration.Initialize();

    // PERF-TIMER-FIX: Windows scheduler granularity 를 1ms 로 변경.
    // System.Windows.Forms.Timer.Interval=33 이 기본 ~15.625ms granularity 에서 46.875ms 로 굳는 결함을 해소.
    // BeginPeriod 의 return value (0=TIMERR_NOERROR) 는 무시 — 실패해도 앱은 정상 동작해야 함 (CLAUDE.md: Critical/High 0건).
    // EndPeriod 와 1:1 매칭 필수 — finally 에서 정확히 1회 호출.
    WinmmTimer.BeginPeriod(1);

    Application.Run(new MainForm());
}
finally
{
    // PERF-TIMER-FIX: BeginPeriod(1) 과 1:1 매칭.
    // BeginPeriod 호출이 실패했더라도 EndPeriod 호출은 안전 (no-op).
    WinmmTimer.EndPeriod(1);

    LogService.AuditAppStop();
    LogService.CloseAndFlush();
}
```

**금지 사항:**
- ApplicationExit 이벤트 핸들러 패턴 사용 금지 — try/finally 가 더 결정적이고 단순함 (예외 발생 시도 finally 보장).
- BeginPeriod 의 return value 검사 금지 — 실패해도 앱은 정상 동작해야 함.
- PerfLog 가드 금지 — A 는 항상 ON.

**Step 1.3 — 빌드 검증:**
```
dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal
```
결과: 0 errors. 신규 경고 0 건 (CS8632 nullable 경고는 pre-existing 으로 무시).

**Step 1.4 — atomic commit:**
```
git add Helpers/WinmmTimer.cs Program.cs
git commit -m "feat(perf): add winmm timeBeginPeriod(1) for 1ms scheduler granularity"
```
  </action>
  <verify>
    <automated>dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal 2>&1 | grep -E "(error|Error|Build succeeded)"</automated>
    Static greps (PowerShell 또는 Bash):
    - `Get-Content Helpers/WinmmTimer.cs | Select-String "internal static extern uint BeginPeriod"` → 1 match
    - `Get-Content Helpers/WinmmTimer.cs | Select-String "internal static extern uint EndPeriod"` → 1 match
    - `Get-Content Program.cs | Select-String "WinmmTimer.BeginPeriod\(1\)"` → 1 match (try 안)
    - `Get-Content Program.cs | Select-String "WinmmTimer.EndPeriod\(1\)"` → 1 match (finally 안)
    - `git log -1 --oneline` 의 메시지가 "feat(perf): add winmm timeBeginPeriod" 로 시작
  </verify>
  <done>
    - `Helpers/WinmmTimer.cs` 신규 파일 존재, BeginPeriod/EndPeriod 2 P/Invoke extern 선언.
    - `Program.cs` 의 `Main()` try 블록에서 `ApplicationConfiguration.Initialize()` 직후 `WinmmTimer.BeginPeriod(1)` 호출.
    - `Program.cs` 의 `Main()` finally 블록 최상단에서 `WinmmTimer.EndPeriod(1)` 호출 (LogService 정리보다 먼저).
    - `using ASLTv1.Helpers;` Program.cs 에 존재.
    - dotnet build Debug 0 errors, 신규 경고 0.
    - 1개 atomic commit.
    - PerfLog 가드 없음 — 모든 빌드에서 항상 ON.
  </done>
</task>

<task type="auto">
  <name>Task 2: v2 jitter 계측 (B/C/D) — PerfLog 확장 + MainForm Playback fps + image set ts + Paint paintLatency 필드 + VideoService gc2 필드</name>
  <files>Helpers/PerfLog.cs, Forms/MainForm.cs, Services/VideoService.cs</files>
  <action>
모든 신규 측정 코드는 반드시 `if (PerfLog.Enabled) { ... }` 또는 `PerfLog.Enabled ? ... : default` 가드 안에 들어가야 함. OFF 시 오버헤드 0 보장.

**Step 2.1 — `Helpers/PerfLog.cs` 확장:**

기존 클래스 본문에 6개 static property 추가. 클래스 전체를 아래로 교체 (Edit tool 사용):

```csharp
namespace ASLTv1.Helpers
{
    /// <summary>
    /// 진단용 perf 로깅 토글. Release 빌드에서도 런타임에 ON/OFF 가능.
    /// 기본 false — 정상 사용자 경로에 영향 없음.
    /// 본 클래스는 진단 전용이며 보안/감사 로그와 별개.
    /// </summary>
    internal static class PerfLog
    {
        public static bool Enabled { get; set; } = false;

        // PERF-V2-JITTER-INST: Playback frame 간격 누적 (30프레임 윈도우)
        public static long LastLoadFrameTimestamp { get; set; } = 0;
        public static int FrameCounter { get; set; } = 0;
        public static long IntervalSumTicks { get; set; } = 0;
        public static long MaxIntervalTicks { get; set; } = 0;

        // PERF-V2-JITTER-INST: PictureBox 자동 스케일링 latency (image set → paint 시작)
        public static long LastImageSetTimestamp { get; set; } = 0;

        // PERF-V2-JITTER-INST: GC Gen2 collection delta (large bitmap 압박 추적)
        public static int LastGen2Count { get; set; } = 0;
    }
}
```

**Step 2.2 — `Forms/MainForm.cs::LoadFrame` 수정 (B: Playback fps + C: image set ts):**

(a) image set 직후 (line 524-528 의 `pictureBoxVideo.Image = bitmap;` 직후) — PerfLog.Enabled 가드 안에서 timestamp 기록:

기존 (line 524-528):
```csharp
if (bitmap != null)
{
    pictureBoxVideo.Image?.Dispose();
    pictureBoxVideo.Image = bitmap;
}
```

변경 후:
```csharp
if (bitmap != null)
{
    pictureBoxVideo.Image?.Dispose();
    pictureBoxVideo.Image = bitmap;

    // PERF-V2-JITTER-INST (C: paintLatency): PictureBox 자동 스케일링 비용 추적을 위해
    // image set 시점 timestamp 를 기록. Paint 시작에서 delta = paintLatency 계산.
    if (PerfLog.Enabled)
    {
        PerfLog.LastImageSetTimestamp = Stopwatch.GetTimestamp();
    }
}
```

(b) `LoadFrame` 메서드 끝 직전 (try 블록 안, 메서드의 마지막 `pictureBoxVideo.Invalidate();` 라인 직후 — line ~594) 에 Playback fps 누적 블록 추가:

```csharp
                pictureBoxVideo.Invalidate();

                // PERF-V2-JITTER-INST (B: Playback fps): LoadFrame 호출 간격을 30프레임 윈도우로 누적,
                // 30개 채워지면 avgGap/maxGap/fps 한 줄 출력 후 리셋.
                // 첫 LoadFrame (LastLoadFrameTimestamp==0) 은 delta 의미 없어 skip, timestamp 만 기록.
                if (PerfLog.Enabled)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (PerfLog.LastLoadFrameTimestamp != 0)
                    {
                        long delta = now - PerfLog.LastLoadFrameTimestamp;
                        PerfLog.IntervalSumTicks += delta;
                        if (delta > PerfLog.MaxIntervalTicks) PerfLog.MaxIntervalTicks = delta;
                        PerfLog.FrameCounter++;
                        if (PerfLog.FrameCounter >= 30)
                        {
                            double avgMs = (PerfLog.IntervalSumTicks * 1000.0 / Stopwatch.Frequency) / PerfLog.FrameCounter;
                            double maxMs = PerfLog.MaxIntervalTicks * 1000.0 / Stopwatch.Frequency;
                            double fps = avgMs > 0 ? 1000.0 / avgMs : 0;
                            Log.Debug("[PERF] Playback avgGap={AvgMs:F1}ms maxGap={MaxMs:F1}ms fps={Fps:F1}", avgMs, maxMs, fps);
                            PerfLog.FrameCounter = 0;
                            PerfLog.IntervalSumTicks = 0;
                            PerfLog.MaxIntervalTicks = 0;
                        }
                    }
                    PerfLog.LastLoadFrameTimestamp = now;
                }
```

**위치 정확성 검증:** 삽입 위치는 try 블록 안, `pictureBoxVideo.Invalidate();` 직후, try 닫는 `}` 전. catch 블록 밖.

**Step 2.3 — `Forms/MainForm.cs::pictureBoxVideo_Paint` 수정 (C: paintLatency 필드):**

(a) `Stopwatch? swPaint = PerfLog.Enabled ? Stopwatch.StartNew() : null;` 라인 (line 1692) 직후, try 블록 안 시작에 paintLatency 계산 코드 추가:

기존 (line 1692-1694):
```csharp
            Stopwatch? swPaint = PerfLog.Enabled ? Stopwatch.StartNew() : null;
            try
            {
                Graphics g = e.Graphics;
```

변경 후:
```csharp
            Stopwatch? swPaint = PerfLog.Enabled ? Stopwatch.StartNew() : null;
            // PERF-V2-JITTER-INST (C: paintLatency): image set → paint 시작 delta.
            // PictureBox 자동 스케일링 비용 추정 — high res 이미지 + zoom mode 의 letterbox 비용 추적.
            double paintLatencyMs = 0;
            if (PerfLog.Enabled)
            {
                long imageSetTs = PerfLog.LastImageSetTimestamp;
                if (imageSetTs != 0)
                    paintLatencyMs = (Stopwatch.GetTimestamp() - imageSetTs) * 1000.0 / Stopwatch.Frequency;
            }
            try
            {
                Graphics g = e.Graphics;
```

(b) 기존 perf 로그 (line 1749-1755) 메시지에 `paintLatency={LatencyMs:F1}ms` 필드 추가:

기존:
```csharp
                    Log.Debug("[PERF] Paint f={Frame} boxes={N} elapsed={Ms}ms pbSize={W}x{H} imgSize={IW}x{IH}",
                        _videoService.CurrentFrameIndex,
                        cachedCurrentFrameBoxes?.Count ?? 0,
                        swPaint.ElapsedMilliseconds,
                        pictureBoxVideo.Width, pictureBoxVideo.Height,
                        pictureBoxVideo.Image?.Width ?? 0,
                        pictureBoxVideo.Image?.Height ?? 0);
```

변경 후 (필드 끝에 paintLatency 추가, 인자 리스트에도 추가):
```csharp
                    Log.Debug("[PERF] Paint f={Frame} boxes={N} elapsed={Ms}ms paintLatency={LatencyMs:F1}ms pbSize={W}x{H} imgSize={IW}x{IH}",
                        _videoService.CurrentFrameIndex,
                        cachedCurrentFrameBoxes?.Count ?? 0,
                        swPaint.ElapsedMilliseconds,
                        paintLatencyMs,
                        pictureBoxVideo.Width, pictureBoxVideo.Height,
                        pictureBoxVideo.Image?.Width ?? 0,
                        pictureBoxVideo.Image?.Height ?? 0);
```

**Step 2.4 — `Services/VideoService.cs::LoadFrame` 수정 (D: gc2 필드):**

(a) decode Stopwatch 라인 (line 229) 직전에 GC Gen2 delta 계산 블록 추가:

기존 (line 227-230):
```csharp
                // PerfLog: 디코드 ms 측정 — 첫 Read 만 측정 (retry loop 는 의도된 fallback 으로 신호 가치 없음)
                long decodeMs = 0;
                Stopwatch? swDecode = PerfLog.Enabled ? Stopwatch.StartNew() : null;
                videoCapture.Read(currentFrame);
```

변경 후:
```csharp
                // PERF-V2-JITTER-INST (D: gc2): 호출 간 Gen2 collection delta — large bitmap 압박 추적.
                int gen2Delta = 0;
                if (PerfLog.Enabled)
                {
                    int gen2Now = GC.CollectionCount(2);
                    gen2Delta = gen2Now - PerfLog.LastGen2Count;
                    PerfLog.LastGen2Count = gen2Now;
                }

                // PerfLog: 디코드 ms 측정 — 첫 Read 만 측정 (retry loop 는 의도된 fallback 으로 신호 가치 없음)
                long decodeMs = 0;
                Stopwatch? swDecode = PerfLog.Enabled ? Stopwatch.StartNew() : null;
                videoCapture.Read(currentFrame);
```

(b) 기존 perf 로그 (line 261-265) 메시지에 `gc2={Gen2Delta}` 필드 추가:

기존:
```csharp
                if (PerfLog.Enabled)
                {
                    Log.Debug("[PERF] LoadFrame f={Frame} {W}x{H} decode={DecodeMs}ms toBmp={ToBmpMs}ms skipSeek={SkipSeek}",
                        frameIndex, FrameWidth, FrameHeight, decodeMs, toBmpMs, skipSeek);
                }
```

변경 후:
```csharp
                if (PerfLog.Enabled)
                {
                    Log.Debug("[PERF] LoadFrame f={Frame} {W}x{H} decode={DecodeMs}ms toBmp={ToBmpMs}ms skipSeek={SkipSeek} gc2={Gen2Delta}",
                        frameIndex, FrameWidth, FrameHeight, decodeMs, toBmpMs, skipSeek, gen2Delta);
                }
```

**Step 2.5 — 빌드 검증:**
```
dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal
```
결과: 0 errors. 신규 경고 0 (pre-existing CS8632 만 허용).

**Step 2.6 — atomic commit:**
```
git add Helpers/PerfLog.cs Forms/MainForm.cs Services/VideoService.cs
git commit -m "feat(perf): add v2 jitter instrumentation (Playback fps, paintLatency, gc2)"
```

**금지 사항:**
- 측정 코드가 `PerfLog.Enabled` 가드 밖에 위치하면 안 됨 (OFF 시 오버헤드 0 원칙 위반).
- 기존 메시지 포맷 / 필드 순서 변경 금지 — 끝에 append 만. v1 grep 호환성 보존.
- `Stopwatch` 인스턴스 생성 금지 — `Stopwatch.GetTimestamp()` static 호출만.
- MouseMove 계측 변경 금지 — v1 그대로 유지.
- Mat/Bitmap 풀링, 다운샘플링 같은 최적화 시도 금지 — 본 task 의 범위 밖.
  </action>
  <verify>
    <automated>dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal 2>&1 | grep -E "(error|Error|Build succeeded)"</automated>

    Static greps (PowerShell — 메시지/필드 존재 확인):
    - `Get-Content Helpers/PerfLog.cs | Select-String "LastLoadFrameTimestamp"` → 1 match
    - `Get-Content Helpers/PerfLog.cs | Select-String "FrameCounter"` → 1 match
    - `Get-Content Helpers/PerfLog.cs | Select-String "IntervalSumTicks"` → 1 match
    - `Get-Content Helpers/PerfLog.cs | Select-String "MaxIntervalTicks"` → 1 match
    - `Get-Content Helpers/PerfLog.cs | Select-String "LastImageSetTimestamp"` → 1 match
    - `Get-Content Helpers/PerfLog.cs | Select-String "LastGen2Count"` → 1 match
    - `Get-Content Forms/MainForm.cs | Select-String "\[PERF\] Playback avgGap"` → 1 match
    - `Get-Content Forms/MainForm.cs | Select-String "paintLatency=\{LatencyMs"` → 1 match
    - `Get-Content Forms/MainForm.cs | Select-String "PerfLog.LastImageSetTimestamp = Stopwatch.GetTimestamp"` → 1 match
    - `Get-Content Services/VideoService.cs | Select-String "gc2=\{Gen2Delta\}"` → 1 match
    - `Get-Content Services/VideoService.cs | Select-String "GC.CollectionCount\(2\)"` → 1 match

    OFF-가드 검증:
    - 모든 v2 신규 측정 코드 (LastLoadFrameTimestamp, LastImageSetTimestamp, paintLatencyMs, gen2Delta 계산) 가 `if (PerfLog.Enabled)` 또는 `PerfLog.Enabled ?` 가드 안에 위치하는지 시각 검토.

    Git:
    - `git log -2 --oneline` 의 최신 commit 메시지가 "feat(perf): add v2 jitter instrumentation" 로 시작.
  </verify>
  <done>
    - `Helpers/PerfLog.cs` 에 6개 신규 static property (LastLoadFrameTimestamp, FrameCounter, IntervalSumTicks, MaxIntervalTicks, LastImageSetTimestamp, LastGen2Count) 추가.
    - `Forms/MainForm.cs::LoadFrame` 에 image set 직후 PerfLog.LastImageSetTimestamp 기록 (PerfLog.Enabled 가드 안).
    - `Forms/MainForm.cs::LoadFrame` 끝 직전에 30프레임 누적 + `[PERF] Playback avgGap=... maxGap=... fps=...` 로그 (PerfLog.Enabled 가드 안).
    - `Forms/MainForm.cs::pictureBoxVideo_Paint` 의 perf 로그에 `paintLatency={LatencyMs:F1}ms` 필드 추가, paintLatencyMs 계산은 PerfLog.Enabled 가드 안.
    - `Services/VideoService.cs::LoadFrame` 의 perf 로그에 `gc2={Gen2Delta}` 필드 추가, gen2Delta 계산은 PerfLog.Enabled 가드 안.
    - 모든 v2 측정 분기가 PerfLog.Enabled 가드 안 — OFF 시 진입 안 함.
    - dotnet build Debug 0 errors, 신규 경고 0.
    - 1개 atomic commit.
    - 기존 v1 메시지 포맷 grep 호환성 보존 (필드 append 만, 순서 변경 없음).
  </done>
</task>

</tasks>

<verification>
**Build-level:**
```
dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal
```
0 errors. 신규 경고 0 (pre-existing CS8632 만 허용).

**Static greps (모두 통과해야 완료):**
```
# A — Timer fix
Get-Content Helpers/WinmmTimer.cs | Select-String "internal static extern uint BeginPeriod" | Measure-Object  # = 1
Get-Content Helpers/WinmmTimer.cs | Select-String "internal static extern uint EndPeriod" | Measure-Object   # = 1
Get-Content Program.cs | Select-String "WinmmTimer.BeginPeriod\(1\)" | Measure-Object                         # = 1
Get-Content Program.cs | Select-String "WinmmTimer.EndPeriod\(1\)" | Measure-Object                           # = 1

# B/C/D — PerfLog 확장
Get-Content Helpers/PerfLog.cs | Select-String "LastLoadFrameTimestamp" | Measure-Object       # = 1
Get-Content Helpers/PerfLog.cs | Select-String "LastImageSetTimestamp" | Measure-Object        # = 1
Get-Content Helpers/PerfLog.cs | Select-String "LastGen2Count" | Measure-Object                # = 1

# B — Playback fps
Get-Content Forms/MainForm.cs | Select-String "\[PERF\] Playback avgGap" | Measure-Object       # = 1

# C — paintLatency
Get-Content Forms/MainForm.cs | Select-String "paintLatency=\{LatencyMs" | Measure-Object       # = 1
Get-Content Forms/MainForm.cs | Select-String "PerfLog.LastImageSetTimestamp = Stopwatch.GetTimestamp" | Measure-Object  # = 1

# D — gc2
Get-Content Services/VideoService.cs | Select-String "gc2=\{Gen2Delta\}" | Measure-Object       # = 1
Get-Content Services/VideoService.cs | Select-String "GC.CollectionCount\(2\)" | Measure-Object # = 1
```

**Git:**
- `git log -3 --oneline` 에 2개 atomic commit:
  - "feat(perf): add winmm timeBeginPeriod(1) for 1ms scheduler granularity"
  - "feat(perf): add v2 jitter instrumentation (Playback fps, paintLatency, gc2)"

**Manual smoke (사용자 빌드 후 수행 — 본 plan 의 verify 가 아니나 expected outcome 기록):**
1. 빌드 산출물 실행 → 정상 시작.
2. F12 OFF 상태로 영상 재생 → 정상 동작, [PERF] 로그 부재. (timer fix 만 적용 — 사용자 체감 33ms tick).
3. F12 ON → 30fps 영상 (fhd_30fps_2h.mkv) 재생:
   - `[PERF] Playback avgGap=...ms maxGap=...ms fps=...` — 기대값: avgGap ≈ 33ms, fps ≈ 30 (timer fix 적용 효과)
   - `[PERF] Paint ... paintLatency=...ms ...` — paintLatency 값 관찰 (PictureBox 스케일링 비용 baseline)
   - `[PERF] LoadFrame ... gc2=N` — Gen2 count delta 관찰 (gc 압박 baseline)
4. F12 OFF → 측정 OFF, 일반 사용 영향 없음.
</verification>

<success_criteria>
**Definition of Done (task_intent 와 1:1 매칭):**

1. [x] `Helpers/WinmmTimer.cs` (P/Invoke wrapper) 존재 — Task 1
2. [x] `Program.cs` 에서 앱 시작 시 BeginPeriod(1), 종료 시 EndPeriod(1) — Task 1
3. [x] `Helpers/PerfLog.cs` 에 6개 새 static property 추가 — Task 2 Step 2.1
4. [x] `Forms/MainForm.cs::LoadFrame` 에 frame 간격 누적 + 30프레임마다 Playback 로그 — Task 2 Step 2.2 (b)
5. [x] `Forms/MainForm.cs::LoadFrame` 에 image set timestamp 기록 — Task 2 Step 2.2 (a)
6. [x] `Forms/MainForm.cs::pictureBoxVideo_Paint` perf 로그 끝에 paintLatency 필드 — Task 2 Step 2.3
7. [x] `Services/VideoService.cs::LoadFrame` perf 로그 끝에 gc2 필드 — Task 2 Step 2.4
8. [x] dotnet build 0 errors — 각 task verify
9. [x] PerfLog.Enabled=false 기본 — B/C/D 계측 OFF, A timer fix 만 ON — Task 1 (가드 없음) + Task 2 (모든 분기 가드 안)
10. [x] CLAUDE.md 준수 — Critical/High 결함 0건 (try/finally 보장, BeginPeriod 실패 무시), 보안/암호화 영향 없음, 신규 의존성 없음 (winmm.dll 은 OS 표준 컴포넌트), 기존 v1 메시지 grep 호환

**Atomic commits:** 2개
- Task 1: `feat(perf): add winmm timeBeginPeriod(1) for 1ms scheduler granularity`
- Task 2: `feat(perf): add v2 jitter instrumentation (Playback fps, paintLatency, gc2)`
</success_criteria>

<output>
After completion, create `.planning/quick/260512-kma-timer-fix-timebeginperiod-v2-jitter-inst/260512-kma-SUMMARY.md` per `$HOME/.claude/get-shit-done/templates/summary.md` 형식.
</output>
