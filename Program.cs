using ASLTv1.Forms;
using ASLTv1.Helpers;
using ASLTv1.Services;
using Serilog;

namespace ASLTv1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 260512-pf6 Task A: OpenCV 의 FFmpeg backend 가 H.264 디코더를 multi-thread 로 사용하도록 설정.
            // VideoCapture 가 생성되기 전 (Environment 단계) 에 설정해야 효과 있음 — 생성 후 변경은 무시됨.
            // "threads;0" 은 FFmpeg 에 자동 코어 검출 위임 — 보통 (logical core 수) 만큼 사용.
            // 효과: 1080p H.264 sequential decode 8ms → 2-4ms 예상 (CPU 코어 수에 비례).
            // GPU/외부 SDK 없이 순수 CPU 멀티스레드 활용. 4x 재생 cycle 의 decode 비용 절반 이하로 감소.
            Environment.SetEnvironmentVariable(
                "OPENCV_FFMPEG_CAPTURE_OPTIONS",
                "threads;0",
                EnvironmentVariableTarget.Process);

            LogService.Initialize();
            LogService.AuditAppStart();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) =>
            {
                // DF-1-17 (D-17a): UI 스레드 처리되지 않은 예외 감사 이벤트
                LogService.AuditException("UIThread", e.Exception);
                Log.Fatal(e.Exception, "[FATAL] UI 스레드에서 처리되지 않은 예외 발생");
                MessageBox.Show(
                    $"예기치 않은 오류가 발생했습니다.\n\n{e.Exception.Message}\n\n로그 파일을 확인해주세요.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    // DF-1-17 (D-17a): 백그라운드 스레드 처리되지 않은 예외 감사 이벤트
                    LogService.AuditException("BackgroundThread", ex);
                    Log.Fatal(ex, "[FATAL] 백그라운드 스레드에서 처리되지 않은 예외 발생");
                }
            };

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
        }
    }
}
