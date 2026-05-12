---
quick_id: 260512-ifn
type: quick
description: perf instrumentation for video hot paths
wave: 1
depends_on: []
files_modified:
  - Helpers/PerfLog.cs
  - Services/VideoService.cs
  - Forms/MainForm.cs
autonomous: true
requirements: []  # quick task — no roadmap requirement IDs

must_haves:
  truths:
    - "사용자가 F12를 눌러 PerfLog를 ON/OFF 토글할 수 있고, 토글 결과가 시각적으로 확인된다 (MessageBox 또는 Log.Information)."
    - "PerfLog.Enabled=false 기본 상태에서 LoadFrame/Paint/MouseMove 경로의 동작·성능이 기존과 동일하다 (Stopwatch 미시작, Log 미출력)."
    - "PerfLog.Enabled=true 상태에서 영상 로드 시 Serilog 로그에 `[PERF] LoadFrame f={Frame} {W}x{H} decode={ms}ms toBmp={ms}ms skipSeek={bool}` 라인이 매 프레임 출력된다."
    - "PerfLog.Enabled=true 상태에서 영상 페인트 시 `[PERF] Paint f={Frame} boxes={N} elapsed={ms}ms pbSize={W}x{H} imgSize={IW}x{IH}` 라인이 매 페인트 출력된다."
    - "PerfLog.Enabled=true + drawing/resize/drag 진행 중 마우스 이동 시 1초 윈도우마다 `[PERF] MouseMove invalidates={N}/sec mode={Mode}` 라인이 출력된다."
    - "`dotnet build` 가 신규 에러 없이 성공한다."
  artifacts:
    - path: "Helpers/PerfLog.cs"
      provides: "런타임 토글 가능한 정적 PerfLog.Enabled 플래그"
      contains: "internal static class PerfLog"
    - path: "Services/VideoService.cs"
      provides: "LoadFrame 디코드/변환 ms 측정 (가드된 Stopwatch + Log.Debug)"
      contains: "PerfLog.Enabled"
    - path: "Forms/MainForm.cs"
      provides: "pictureBoxVideo_Paint elapsed ms + pictureBoxVideo_MouseMove invalidate 카운터 (1초 윈도우) + F12 토글"
      contains: "PerfLog.Enabled"
  key_links:
    - from: "MainForm.MainForm_KeyDown (F12 분기)"
      to: "Helpers/PerfLog.cs (PerfLog.Enabled)"
      via: "토글 + 시각 피드백"
      pattern: "PerfLog\\.Enabled\\s*=\\s*!"
    - from: "Services/VideoService.cs (LoadFrame 본문)"
      to: "Helpers/PerfLog.cs (PerfLog.Enabled 가드)"
      via: "Stopwatch + Log.Debug"
      pattern: "if\\s*\\(\\s*PerfLog\\.Enabled"
    - from: "Forms/MainForm.cs (pictureBoxVideo_Paint)"
      to: "Helpers/PerfLog.cs (PerfLog.Enabled 가드)"
      via: "Stopwatch + Log.Debug (try-finally 로 정상 페인트 흐름 보호)"
      pattern: "pictureBoxVideo_Paint[\\s\\S]{0,800}PerfLog\\.Enabled"
    - from: "Forms/MainForm.cs (pictureBoxVideo_MouseMove)"
      to: "Helpers/PerfLog.cs (PerfLog.Enabled 가드)"
      via: "invalidate 카운터 + 1초 윈도우 flush"
      pattern: "pictureBoxVideo_MouseMove[\\s\\S]{0,2000}PerfLog\\.Enabled"
---

<objective>
ASLT 의 고해상도/장시간 영상 재생 시 성능 병목을 ms 단위로 측정하기 위한 **진단용 perf 로그 계측**을 추가한다.
사용자가 코드 추적으로 식별한 3개 핫패스(`VideoService.LoadFrame`, `MainForm.pictureBoxVideo_Paint`, `MainForm.pictureBoxVideo_MouseMove`)에 가드된 Stopwatch 측정을 삽입하고, `Helpers/PerfLog.cs` 신규 파일에 런타임 토글 플래그를 노출한다. F12 단축키로 토글한다.

Purpose: 사용자가 의심 영상을 재생하며 Serilog 출력을 보고 실제 병목 단계를 확정한다. 최적화 구현은 본 task 비범위.
Output:
  - 신규 파일 `Helpers/PerfLog.cs`
  - `Services/VideoService.cs::LoadFrame` 내부 계측
  - `Forms/MainForm.cs::pictureBoxVideo_Paint` 내부 계측
  - `Forms/MainForm.cs::pictureBoxVideo_MouseMove` 내부 1초 윈도우 invalidate 카운터
  - `Forms/MainForm.cs::MainForm_KeyDown` 의 F12 분기 (토글 + 시각 피드백)

**CLAUDE.md 준수**: C# .NET 8 WinForms 유지, 기존 코드 기반 개선만, Critical/High 결함 0건 (계측이 정상 흐름을 깨면 안 됨 — Stopwatch 가드 + try-finally), 보안 영향 없음 (진단 로그만).
</objective>

<execution_context>
@$HOME/.claude/get-shit-done/workflows/execute-plan.md
@$HOME/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@./CLAUDE.md
@.planning/STATE.md

<interfaces>
<!-- Executor 가 즉시 사용할 수 있는 검증된 코드 컨텍스트. 추가 탐색 없이 진행 가능. -->

### Serilog 설정 (이미 활성)
From `Services/LogService.cs:49-52`:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()                  // ← Debug 레벨 받음, Log.Debug 그대로 사용 가능
    .WriteTo.Console(outputTemplate: LOG_TEMPLATE)
    .WriteTo.Sink(new HmacChainSink(pathTemplate, fileFormatter, hmacKey))
```
**중요**: `Log.Debug(...)` 그대로 사용. Log.Information 격상 불필요.

### `Services/VideoService.cs` 관련 정보
- 파일 상단 (line 1-2): `using System.Diagnostics;` + `using Serilog;` 이미 import — 추가 import 불필요.
- `LoadFrame(int frameIndex)` 시그니처: line 201, `public Bitmap? LoadFrame(int frameIndex)`.
- 측정 대상 1 — 디코드: `videoCapture.Read(currentFrame)` 는 line 225 (첫 Read 만 측정 — retry loop line 228-233 은 제외, 의도된 fallback 이라 별도 신호 가치 없음).
- 측정 대상 2 — Bitmap 변환: `BitmapConverter.ToBitmap(currentFrame)` 는 line 241.
- 기존 변수 활용: `bool skipSeek` (line 216), `FrameWidth`/`FrameHeight` Properties (line 74-78).
- 출력 위치: `currentFrameIndex = frameIndex;` (line 244) **직전** — bitmap 변환까지 완료된 시점.
- 기존 catch 블록 (line 249-258) 은 이미 Log.Error 호출하므로 perf 측정 실패 시 자연 흡수됨. Stopwatch 코드는 메서드 본문 안에 직접 삽입.

### `Forms/MainForm.cs` 관련 정보
- 파일 상단 (line 1-16): `using Serilog;` 이미 import. **`using System.Diagnostics;` 는 import 안 됨** — 이 파일에서 `System.Diagnostics.Debug.WriteLine` 은 fully-qualified 로 호출 (line 3096, 3122). Stopwatch 도 동일하게 `System.Diagnostics.Stopwatch` 풀네임 또는 `using` 추가 중 선택.
- **권장: 파일 상단에 `using System.Diagnostics;` 추가** (다른 import 와 일관). 기존 `System.Diagnostics.Debug.WriteLine` 호출은 fully-qualified 형태 그대로 두어도 컴파일 OK.
- `using ASLTv1.Helpers;` 이미 import — `PerfLog` 접근 자유.
- `pictureBoxVideo_Paint` 시그니처: line 1682, 메서드 끝 line 1733. 즉시 return 경로 line 1684 (`if (pictureBoxVideo.Image == null) return;`) 가 있으므로 Stopwatch.StartNew 는 그 **이후** 에 두어 조기 return 시 측정 안 함 (false alarm 방지).
- `pictureBoxVideo_MouseMove` 시그니처: line 1933, 메서드 끝 line 1985. `pictureBoxVideo.Invalidate()` 호출은 line 1945 (drawing), 1950 (resize), 1971 (drag). 별도 분기 분리 불필요 — 단일 카운터로 통합.
- `MainForm_KeyDown` 진입점: line 2787. `KeyPreview = true` 활성 (Designer line 759). F12 는 **현재 미사용** (F1/F2/F3 만 사용). F12 분기 추가 위치: line 2806 직후 (F1/F2/F3 블록 종료 직후 — `if (!e.Control && !e.Shift && !e.Alt)` 블록 안).
- 박스 카운트 변수: `cachedCurrentFrameBoxes` (List<BoundingBox>, line 1697 에서 foreach 됨).

### `Helpers/PerfLog.cs` 신규 파일 — 정확한 컨텐츠
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
파일은 이거 한 줄짜리 책임만 가짐 — 더 추가하지 말 것.

### 토글 시각 피드백
프로젝트엔 상태바(StatusStrip) 가 없음 (확인됨). 두 옵션 중 **MessageBox** 사용:
```csharp
Log.Information("[PERF] toggle Enabled={Enabled}", PerfLog.Enabled);
MessageBox.Show($"PerfLog: {(PerfLog.Enabled ? "ON" : "OFF")}", "Perf Instrumentation",
    MessageBoxButtons.OK, MessageBoxIcon.Information);
```
MessageBox 가 모달이지만 진단용 토글이므로 명확한 시각 피드백 우선.

### MouseMove 1초 윈도우 — 상태 변수
MainForm 의 #region Fields 영역(예: 다른 cached counter 들 근처)에 두 필드 추가:
```csharp
// PerfLog: MouseMove invalidate counter (1초 윈도우)
private int _perfMouseMoveInvalidates;
private long _perfMouseMoveLastFlushTicks; // DateTime.UtcNow.Ticks
private string _perfMouseMoveLastModeTag = "idle";
```
플러시 로직: 카운터 증가 직후 `(DateTime.UtcNow.Ticks - _perfMouseMoveLastFlushTicks) >= TimeSpan.FromSeconds(1).Ticks` 이면 출력 후 카운터·timestamp 리셋.
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Helpers/PerfLog.cs 신규 파일 + F12 토글 단축키</name>
  <files>Helpers/PerfLog.cs, Forms/MainForm.cs</files>
  <action>
1. 신규 파일 `Helpers/PerfLog.cs` 작성 — 위 `<interfaces>` 의 "Helpers/PerfLog.cs 신규 파일 — 정확한 컨텐츠" 블록을 그대로 사용. 추가 멤버/속성/메서드 금지 (책임은 단일 bool 플래그 노출만).

2. `Forms/MainForm.cs` 파일 상단 using 블록에 **`using System.Diagnostics;`** 한 줄 추가 (line 1-16 범위, 알파벳 순서 무시하고 다른 System.* 들 사이에 자연스러운 위치). 기존 `System.Diagnostics.Debug.WriteLine` 호출 (line 3096, 3122) 은 그대로 두어도 OK.

3. `MainForm_KeyDown` (line 2787) 내부, F1/F2/F3 분기 직후(line 2805 의 F3 처리 다음 줄, 같은 `if (!e.Control && !e.Shift && !e.Alt)` 블록 안)에 F12 분기 추가:
```csharp
if (e.KeyCode == Keys.F12)
{
    PerfLog.Enabled = !PerfLog.Enabled;
    Log.Information("[PERF] toggle Enabled={Enabled}", PerfLog.Enabled);
    MessageBox.Show(
        $"PerfLog: {(PerfLog.Enabled ? "ON" : "OFF")}",
        "Perf Instrumentation",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
    e.Handled = true;
    return;
}
```
- F1/F2/F3 와 동일한 modifier 가드(`!e.Control && !e.Shift && !e.Alt`) 안에 두어 Ctrl+F12 등 다른 조합과 충돌 없음.
- `e.Handled = true; return;` 로 후속 분기로 흐르지 않게 차단.

4. **검증 명령 실행**:
   ```
   dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug
   ```
   에러 없이 성공해야 함. 신규 경고 발생 시 합리적으로 처리 (예: 미사용 변수 제거). 기존 경고는 유지.

5. **수동 sanity (선택)**: 빌드 산출물 실행 후 F12 두 번 눌러 ON/OFF MessageBox 표시되고 Log 파일에 `[PERF] toggle Enabled=True/False` 라인이 기록되는지 확인. (executor 가 빌드만 확인하고 수동 단계 생략 가능 — 작동은 다음 task 들과 함께 통합 검증.)

6. 변경 사항을 atomic commit:
```
git -C "C:\Users\ANNA\AOLTv1.0" add Helpers/PerfLog.cs Forms/MainForm.cs
git -C "C:\Users\ANNA\AOLTv1.0" commit -m "feat(perf): add PerfLog toggle flag + F12 keyboard shortcut

진단용 perf 계측을 위한 런타임 토글 플래그 도입. F12 로 ON/OFF 전환 + MessageBox 피드백.
기본 false — 정상 사용자 경로에 성능/동작 영향 없음.
계측 자체는 후속 commit 에서 추가."
```
  </action>
  <verify>
    <automated>cd "C:\Users\ANNA\AOLTv1.0" && dotnet build ASLTv1.0.csproj -c Debug -v minimal</automated>
  </verify>
  <done>
- `Helpers/PerfLog.cs` 존재, `internal static class PerfLog { public static bool Enabled { get; set; } = false; }` 정확히 정의.
- `Forms/MainForm.cs` 상단에 `using System.Diagnostics;` 추가됨.
- `MainForm_KeyDown` 에 F12 분기 추가 — PerfLog.Enabled 토글 + Log.Information + MessageBox + `e.Handled = true`.
- `dotnet build` 성공 (Debug, x64 또는 AnyCPU).
- atomic commit 1건 생성.
  </done>
</task>

<task type="auto">
  <name>Task 2: VideoService.LoadFrame 디코드/변환 ms 계측</name>
  <files>Services/VideoService.cs</files>
  <action>
1. `Services/VideoService.cs` 상단(line 1-7 범위) 의 `using` 블록에 `using ASLTv1.Helpers;` 추가 — 기존 `using ASLTv1.Models;` (line 7) 옆 자연스러운 위치. (`using System.Diagnostics;` 와 `using Serilog;` 는 이미 존재.)

2. `LoadFrame(int frameIndex)` (line 201) 본문 수정. **수정 범위는 line 222 (currentFrame?.Dispose() 직전) ~ line 244 (currentFrameIndex = frameIndex 직전) 사이만**. try-catch 블록 구조와 retry loop, FrameChanged 이벤트는 변경 금지.

   기존 (line 222-244):
   ```csharp
   currentFrame?.Dispose();
   currentFrame = new Mat();
   videoCapture.Read(currentFrame);

   // 일부 코덱은 첫 Read 후에도 empty Mat — 후속 Read 호출에서 디코더 버퍼가 채워짐.
   int retryCount = 0;
   while (currentFrame.Empty() && retryCount < 3)
   {
       videoCapture.Read(currentFrame);
       retryCount++;
   }

   // 첫 read 가 성공/실패 여부와 무관하게 freshly-opened 표시는 해제 (seek 경로로 진입)
   _isFreshlyOpened = false;

   Bitmap? bitmap = null;
   if (!currentFrame.Empty())
   {
       bitmap = BitmapConverter.ToBitmap(currentFrame);
   }

   currentFrameIndex = frameIndex;
   ```

   수정 후:
   ```csharp
   currentFrame?.Dispose();
   currentFrame = new Mat();

   // PerfLog: 디코드 ms 측정 — 첫 Read 만 측정 (retry loop 는 의도된 fallback 으로 신호 가치 없음)
   long decodeMs = 0;
   Stopwatch? swDecode = PerfLog.Enabled ? Stopwatch.StartNew() : null;
   videoCapture.Read(currentFrame);
   if (swDecode != null)
   {
       swDecode.Stop();
       decodeMs = swDecode.ElapsedMilliseconds;
   }

   // 일부 코덱은 첫 Read 후에도 empty Mat — 후속 Read 호출에서 디코더 버퍼가 채워짐.
   int retryCount = 0;
   while (currentFrame.Empty() && retryCount < 3)
   {
       videoCapture.Read(currentFrame);
       retryCount++;
   }

   // 첫 read 가 성공/실패 여부와 무관하게 freshly-opened 표시는 해제 (seek 경로로 진입)
   _isFreshlyOpened = false;

   Bitmap? bitmap = null;
   long toBmpMs = 0;
   if (!currentFrame.Empty())
   {
       Stopwatch? swToBmp = PerfLog.Enabled ? Stopwatch.StartNew() : null;
       bitmap = BitmapConverter.ToBitmap(currentFrame);
       if (swToBmp != null)
       {
           swToBmp.Stop();
           toBmpMs = swToBmp.ElapsedMilliseconds;
       }
   }

   if (PerfLog.Enabled)
   {
       Log.Debug("[PERF] LoadFrame f={Frame} {W}x{H} decode={DecodeMs}ms toBmp={ToBmpMs}ms skipSeek={SkipSeek}",
           frameIndex, FrameWidth, FrameHeight, decodeMs, toBmpMs, skipSeek);
   }

   currentFrameIndex = frameIndex;
   ```

   **중요**:
   - Stopwatch 인스턴스 자체는 PerfLog.Enabled=false 일 때 절대 생성되지 않음 — `?:` 삼항으로 가드.
   - PerfLog.Enabled 가 false 면 ms 변수들은 0 으로 유지되지만 Log.Debug 도 건너뛰므로 무의미.
   - 기존 try/catch 구조 (line 249-258) 변경 금지 — Stopwatch 가 예외를 던지지 않으므로 try-finally 도 불필요.
   - retry loop 측정 안 함 (의도) — 정상 경로만 측정.
   - `skipSeek` 변수는 line 216 에 이미 존재 → 그대로 활용.

3. **검증 명령 실행**:
   ```
   dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug
   ```

4. atomic commit:
```
git -C "C:\Users\ANNA\AOLTv1.0" add Services/VideoService.cs
git -C "C:\Users\ANNA\AOLTv1.0" commit -m "feat(perf): instrument VideoService.LoadFrame decode + ToBitmap latency

PerfLog.Enabled 가드 하 첫 videoCapture.Read 와 BitmapConverter.ToBitmap 의
경과 ms 를 Serilog Debug 로그로 출력. Stopwatch 는 토글 OFF 시 미생성 — 핫경로 오버헤드 0.
retry loop 는 의도된 fallback 으로 측정 제외."
```
  </action>
  <verify>
    <automated>cd "C:\Users\ANNA\AOLTv1.0" && dotnet build ASLTv1.0.csproj -c Debug -v minimal</automated>
  </verify>
  <done>
- `Services/VideoService.cs` 상단에 `using ASLTv1.Helpers;` 추가.
- `LoadFrame` 본문에 두 개의 `Stopwatch?` (디코드, ToBitmap) + 통합 Log.Debug 한 줄 추가.
- 모든 perf 코드는 `PerfLog.Enabled` 가드 안 (Stopwatch 자체도 가드 — 삼항 연산자).
- try-catch 구조, retry loop, FrameChanged 이벤트, currentFrameIndex 할당 모두 변경 없음.
- `dotnet build` 성공.
- atomic commit 1건 생성.
  </done>
</task>

<task type="auto">
  <name>Task 3: MainForm Paint elapsed ms + MouseMove invalidate counter</name>
  <files>Forms/MainForm.cs</files>
  <action>
1. `Forms/MainForm.cs` 의 **#region Fields** 또는 적절한 private 필드 영역에 MouseMove 1초 윈도우 상태 변수 3개 추가:
```csharp
// PerfLog: MouseMove invalidate counter (1초 윈도우 누적)
private int _perfMouseMoveInvalidates;
private long _perfMouseMoveLastFlushTicks; // DateTime.UtcNow.Ticks
private string _perfMouseMoveLastModeTag = "idle";
```
위치: 다른 cached counter (`cachedCurrentFrameBoxes`, `lastCachedFrameForPaint` 같은 private 필드) 근처. 정확한 line 은 executor 가 결정 — `private List<BoundingBox> cachedCurrentFrameBoxes` 또는 `private int lastCachedFrameForPaint` 선언 근처가 자연스러움.

2. **`pictureBoxVideo_Paint`** (line 1682) 수정. 기존 본문은 line 1684 의 `if (pictureBoxVideo.Image == null) return;` 가드 후 작업을 수행하고 line 1733 에서 종료. Stopwatch 는 조기 return 가드 **이후** 에 시작 (null image 시 측정 안 함):

   수정 전 (line 1682-1689):
   ```csharp
   private void pictureBoxVideo_Paint(object sender, PaintEventArgs e)
   {
       if (pictureBoxVideo.Image == null) return;

       Graphics g = e.Graphics;
       g.SmoothingMode = SmoothingMode.AntiAlias;
       int currentFrameIndex = _videoService.CurrentFrameIndex;
   ```

   수정 후:
   ```csharp
   private void pictureBoxVideo_Paint(object sender, PaintEventArgs e)
   {
       if (pictureBoxVideo.Image == null) return;

       Stopwatch? swPaint = PerfLog.Enabled ? Stopwatch.StartNew() : null;
       try
       {
           Graphics g = e.Graphics;
           g.SmoothingMode = SmoothingMode.AntiAlias;
           int currentFrameIndex = _videoService.CurrentFrameIndex;
   ```

   그리고 메서드 끝 (line 1733 의 `}` 직전) 에 `finally` 추가. 기존 line 1719-1732 의 first-paint handshake 블록은 try 블록 안에 그대로 유지. 메서드 끝부분 수정 전:
   ```csharp
           // RELI-06 (...): first-paint handshake — ...
           if (!_isVideoReady && _videoService.IsVideoLoaded && pictureBoxVideo.Image != null)
           {
               _isVideoReady = true;
               if (_pendingAutoPlay)
               {
                   _pendingAutoPlay = false;
                   if (!isPlaying)
                   {
                       btnPlay_Click(null, EventArgs.Empty);
                   }
               }
           }
       }
   ```

   수정 후 (try 닫고 finally 추가):
   ```csharp
           // RELI-06 (...): first-paint handshake — ...
           if (!_isVideoReady && _videoService.IsVideoLoaded && pictureBoxVideo.Image != null)
           {
               _isVideoReady = true;
               if (_pendingAutoPlay)
               {
                   _pendingAutoPlay = false;
                   if (!isPlaying)
                   {
                       btnPlay_Click(null, EventArgs.Empty);
                   }
               }
           }
       }
       finally
       {
           if (swPaint != null)
           {
               swPaint.Stop();
               // PerfLog: paint 경과. boxes/이미지 크기 함께 출력 — 박스 N · 해상도가 비용에 어떻게 기여하는지 확인.
               Log.Debug("[PERF] Paint f={Frame} boxes={N} elapsed={Ms}ms pbSize={W}x{H} imgSize={IW}x{IH}",
                   _videoService.CurrentFrameIndex,
                   cachedCurrentFrameBoxes?.Count ?? 0,
                   swPaint.ElapsedMilliseconds,
                   pictureBoxVideo.Width, pictureBoxVideo.Height,
                   pictureBoxVideo.Image?.Width ?? 0,
                   pictureBoxVideo.Image?.Height ?? 0);
           }
       }
   }
   ```

   **중요**:
   - try-finally 로 Paint 흐름 보호 — Log.Debug 실패해도 Paint 결과는 항상 출력.
   - PerfLog 미활성 시 swPaint 가 null 이라 finally 블록의 if 가드도 통과 안 함 — 오버헤드 0.
   - `cachedCurrentFrameBoxes` 는 메서드 본문에서 갱신되므로 finally 시점에 최신 값 사용 (line 1690-1694 의 갱신 후).
   - `pictureBoxVideo.Image` 는 finally 진입 시 여전히 valid (UI 스레드 단일 실행).

3. **`pictureBoxVideo_MouseMove`** (line 1933) 수정. 세 곳의 `pictureBoxVideo.Invalidate()` (line 1945, 1950, 1971) 직전에 카운터 증가 + 1초 윈도우 플러시 호출. 중복 코드를 피하려고 단일 private 헬퍼 메서드를 메서드 본문 끝 또는 Region 안에 추가:

   먼저 새 private 헬퍼 추가 (`pictureBoxVideo_MouseMove` 메서드 직후 또는 같은 region 안):
   ```csharp
   // PerfLog: MouseMove invalidate 누적 카운터 + 1초 윈도우 flush
   private void PerfRecordMouseMoveInvalidate(string modeTag)
   {
       if (!PerfLog.Enabled) return;
       _perfMouseMoveInvalidates++;
       _perfMouseMoveLastModeTag = modeTag;
       long nowTicks = DateTime.UtcNow.Ticks;
       if (_perfMouseMoveLastFlushTicks == 0)
       {
           _perfMouseMoveLastFlushTicks = nowTicks;
           return;
       }
       if (nowTicks - _perfMouseMoveLastFlushTicks >= TimeSpan.FromSeconds(1).Ticks)
       {
           Log.Debug("[PERF] MouseMove invalidates={N}/sec mode={Mode}",
               _perfMouseMoveInvalidates, _perfMouseMoveLastModeTag);
           _perfMouseMoveInvalidates = 0;
           _perfMouseMoveLastFlushTicks = nowTicks;
       }
   }
   ```

   그리고 세 곳의 Invalidate() 직전에 호출 삽입:

   - **line 1945 직전 (drawing 분기)**:
     ```csharp
     drawingBox.Rectangle = new Rectangle(x, y, width, height);
     PerfRecordMouseMoveInvalidate("drawing");
     pictureBoxVideo.Invalidate();
     ```

   - **line 1950 직전 (resize 분기)**:
     ```csharp
     PerformResize(e.Location);
     PerfRecordMouseMoveInvalidate("resize");
     pictureBoxVideo.Invalidate();
     ```

   - **line 1971 직전 (drag 분기)**:
     ```csharp
     if (pictureBoxVideo.Image != null)
         selectedBox.Rectangle = CoordinateHelper.ClampToImage(selectedBox.Rectangle, pictureBoxVideo.Image.Width, pictureBoxVideo.Image.Height);
     PerfRecordMouseMoveInvalidate("drag");
     pictureBoxVideo.Invalidate();
     ```

   다른 Invalidate 호출 (예: MouseUp line 2009, 2030 등) 은 본 task 범위 밖 — 수정 금지.

4. **검증 명령 실행**:
   ```
   dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug
   ```

5. atomic commit:
```
git -C "C:\Users\ANNA\AOLTv1.0" add Forms/MainForm.cs
git -C "C:\Users\ANNA\AOLTv1.0" commit -m "feat(perf): instrument MainForm pictureBoxVideo_Paint + MouseMove

PerfLog.Enabled 가드 하 Paint 메서드 전체 경과 ms 와 MouseMove 의 Invalidate 호출 빈도를
1초 윈도우로 누적해 Serilog Debug 로그로 출력. drawing/resize/drag 분기 통합 카운트.
try-finally 로 Paint 흐름 보호 — 계측 실패가 페인트 결과에 영향 없음."
```
  </action>
  <verify>
    <automated>cd "C:\Users\ANNA\AOLTv1.0" && dotnet build ASLTv1.0.csproj -c Debug -v minimal</automated>
  </verify>
  <done>
- MainForm 의 #region Fields 영역에 `_perfMouseMoveInvalidates`, `_perfMouseMoveLastFlushTicks`, `_perfMouseMoveLastModeTag` 3개 필드 추가.
- `pictureBoxVideo_Paint` 본문이 try-finally 로 감싸짐 (조기 return 가드는 try 밖 유지), finally 에 PerfLog.Enabled 가드된 Log.Debug.
- `PerfRecordMouseMoveInvalidate(string modeTag)` 헬퍼 추가됨.
- `pictureBoxVideo_MouseMove` 의 세 Invalidate 호출 (drawing/resize/drag) 직전에 헬퍼 호출 삽입.
- `dotnet build` 성공.
- atomic commit 1건 생성.
- 기존 동작/성능에 영향 없음 (PerfLog.Enabled=false 기본 시 모든 가드 통과 안 함).
  </done>
</task>

</tasks>

<verification>

## Build verification
```
dotnet build "C:\Users\ANNA\AOLTv1.0\ASLTv1.0.csproj" -c Debug -v minimal
```
신규 에러 0 — 신규 경고 발생 시 합리적으로 처리.

## Static greps (Definition of Done 검증)

```bash
# PerfLog 클래스 존재 + 단일 Enabled 속성
grep -n "internal static class PerfLog" Helpers/PerfLog.cs
grep -n "public static bool Enabled" Helpers/PerfLog.cs

# VideoService 의 PerfLog 가드된 Stopwatch (2개) + Log.Debug LoadFrame 1줄
grep -c "PerfLog.Enabled ? Stopwatch.StartNew()" Services/VideoService.cs
# expect: 2
grep -c "\[PERF\] LoadFrame" Services/VideoService.cs
# expect: 1

# MainForm 의 PerfLog Paint Log + MouseMove flush 헬퍼
grep -c "\[PERF\] Paint" Forms/MainForm.cs
# expect: 1
grep -c "\[PERF\] MouseMove invalidates" Forms/MainForm.cs
# expect: 1
grep -c "PerfRecordMouseMoveInvalidate" Forms/MainForm.cs
# expect: >= 4 (1 declaration + 3 call sites)

# F12 토글
grep -c "Keys.F12" Forms/MainForm.cs
# expect: >= 1
grep -c "PerfLog.Enabled = !PerfLog.Enabled" Forms/MainForm.cs
# expect: 1

# 기본값이 false 인지 (정상 사용자 경로 무영향 검증)
grep -n "Enabled { get; set; } = false" Helpers/PerfLog.cs
# expect: 1 match
```

## Manual smoke (사용자 수행)

1. 빌드 산출물 실행.
2. F12 누르면 MessageBox "PerfLog: ON" 표시. 닫음.
3. 의심 영상 로드 → 프레임 이동/재생 → 로그 파일에 `[PERF] LoadFrame ...` 라인 다수 기록.
4. 박스를 마우스로 그리기/리사이즈/드래그 → 진행 중 1초마다 `[PERF] MouseMove invalidates=N/sec mode=...` 한 줄씩.
5. 페인트 발생할 때마다 `[PERF] Paint ...` 라인 기록.
6. F12 다시 눌러 OFF → MessageBox "PerfLog: OFF" → 이후 [PERF] 라인 더 이상 출력 안 됨.
7. 토글 OFF 인 정상 상태로 영상 재생 → 기존 동작 동일.

</verification>

<success_criteria>

- [ ] `Helpers/PerfLog.cs` 존재, `Enabled` 정적 bool, 기본 false.
- [ ] `VideoService.LoadFrame` 안에 Stopwatch 2개 (decode, toBmp) + 통합 Log.Debug 1줄, 모두 PerfLog.Enabled 가드.
- [ ] `MainForm.pictureBoxVideo_Paint` 가 try-finally 로 감싸지고 finally 에 PerfLog.Enabled 가드된 Log.Debug.
- [ ] `MainForm.pictureBoxVideo_MouseMove` 의 drawing/resize/drag 분기 Invalidate 직전에 PerfRecordMouseMoveInvalidate 호출, 1초 윈도우로 통합 카운트 출력.
- [ ] F12 단축키로 PerfLog.Enabled 토글 + MessageBox 시각 피드백 + Log.Information audit.
- [ ] `dotnet build` 성공, 신규 에러 0.
- [ ] PerfLog.Enabled=false 기본 — 정상 사용자 경로 무영향 (Stopwatch 미생성, Log 미출력).
- [ ] 3개 atomic git commit (PerfLog+F12, VideoService 계측, MainForm 계측).

</success_criteria>

<output>
After completion, ensure `.planning/quick/260512-ifn-perf-instrumentation-for-video-hot-paths/` 폴더에 3개 commit 이 반영된 상태에서 quick task 종료.

(Quick task 라서 별도 SUMMARY.md 작성 의무 없음 — STATE.md 의 Quick Tasks Completed 표 갱신은 마무리 단계에서 처리.)
</output>
