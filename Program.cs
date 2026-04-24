using ASLTv1.Forms;
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
                Application.Run(new MainForm());
            }
            finally
            {
                LogService.AuditAppStop();
                LogService.CloseAndFlush();
            }
        }
    }
}
