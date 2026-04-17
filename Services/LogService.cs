using Serilog;

namespace ASLTv1.Services
{
    /// <summary>
    /// Serilog 기반 로그 서비스.
    /// 날짜별 파일 로테이션과 감사(Audit) 이벤트 기록을 제공한다.
    /// </summary>
    public static class LogService
    {
        #region Constants

        /// <summary>로그 출력 템플릿</summary>
        private const string LOG_TEMPLATE =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        #endregion

        #region Initialization

        /// <summary>
        /// Serilog 로거를 초기화한다.
        /// %LOCALAPPDATA%\ANNA\ASLT\logs 디렉터리에 날짜별 로그 파일(AOLT-yyyy-MM-dd.log)을 생성한다.
        /// 일반 사용자 권한으로 쓰기 가능한 표준 위치 — Program Files 설치 시에도 정상 동작.
        /// 30일 초과 로그 파일은 자동 삭제된다.
        /// </summary>
        public static void Initialize()
        {
            // RELI fix: Program Files 아래 설치 시 app 폴더 쓰기 불가 → LocalAppData 사용
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ANNA", "ASLT", "logs");
            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: LOG_TEMPLATE)
                .WriteTo.File(
                    path: Path.Combine(logDir, "AOLT-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: LOG_TEMPLATE,
                    encoding: System.Text.Encoding.UTF8)
                .CreateLogger();
        }

        #endregion

        #region Audit Events

        /// <summary>
        /// [AUDIT] 애플리케이션 시작 이벤트를 기록한다.
        /// </summary>
        public static void AuditAppStart()
        {
            Log.Information("[AUDIT] 애플리케이션 시작");
        }

        /// <summary>
        /// [AUDIT] 애플리케이션 종료 이벤트를 기록한다.
        /// </summary>
        public static void AuditAppStop()
        {
            Log.Information("[AUDIT] 애플리케이션 종료");
        }

        /// <summary>
        /// [AUDIT] JSON 저장 이벤트를 기록한다.
        /// </summary>
        /// <param name="filePath">저장된 JSON 파일 경로</param>
        public static void AuditJsonSave(string filePath)
        {
            Log.Information("[AUDIT] JSON 저장: {FilePath}", filePath);
        }

        /// <summary>
        /// [AUDIT] 라이선스 오류 이벤트를 기록한다.
        /// </summary>
        /// <param name="reason">오류 사유</param>
        public static void AuditLicenseError(string reason)
        {
            Log.Information("[AUDIT] 라이선스 오류: {Reason}", reason);
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// 로거를 종료하고 버퍼를 플러시한다.
        /// 애플리케이션 종료 시 반드시 호출해야 한다.
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }

        #endregion
    }
}
