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
