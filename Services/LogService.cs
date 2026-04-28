using Serilog;
using Serilog.Formatting.Display;

namespace ASLTv1.Services
{
    /// <summary>
    /// Serilog 기반 로그 서비스.
    /// 날짜별 파일 로테이션과 감사(Audit) 이벤트 기록을 제공한다.
    /// DF-1-17 (D-17): HMAC 무결성 체인 + 30일 보존 + 9종 확장 감사 이벤트.
    /// </summary>
    public static class LogService
    {
        #region Constants

        /// <summary>로그 출력 템플릿 (콘솔/파일 공통)</summary>
        private const string LOG_TEMPLATE =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>D-17c: 로그 파일 보존 기간(일)</summary>
        private const int RETAIN_DAYS = 30;

        #endregion

        #region Initialization

        /// <summary>
        /// Serilog 로거를 초기화한다.
        /// %LOCALAPPDATA%\ANNA\ASLT\logs 디렉터리에 날짜별 로그 파일(ASLT-yyyy-MM-dd.log)을 생성한다.
        /// 일반 사용자 권한으로 쓰기 가능한 표준 위치 — Program Files 설치 시에도 정상 동작.
        /// DF-1-17 (D-17b): 각 로그 라인에 HMAC-SHA256 기반 무결성 체인을 부착한다.
        /// DF-1-17 (D-17c): 30일 초과 로그 파일은 시작 시 자동 삭제된다.
        /// </summary>
        public static void Initialize()
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ANNA", "ASLT", "logs");
            Directory.CreateDirectory(logDir);
            string pathTemplate = Path.Combine(logDir, "ASLT-.log");

            // D-17b: HMAC 체인용 비밀키 확보 (머신 최초 실행 시 자동 생성)
            // NOTE: HmacKeyProvider 는 logger 초기화 전 호출되므로 내부에서 Log.* 을 사용하지 않는다.
            byte[] hmacKey = HmacKeyProvider.GetOrCreateKey(out bool keyWasCreated);

            // D-17b: 커스텀 Sink 사용 — 일자별 파일 + HMAC 체인
            var fileFormatter = new MessageTemplateTextFormatter(LOG_TEMPLATE, null);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: LOG_TEMPLATE)
                .WriteTo.Sink(new HmacChainSink(pathTemplate, fileFormatter, hmacKey))
                .CreateLogger();

            // D-17c: 30일 보존 정책 — 시작 시 오래된 파일 정리 (Serilog retainedFileCountLimit 대체)
            CleanupOldLogs(logDir, RETAIN_DAYS);

            // 키 생성 이벤트는 logger 가 완전히 준비된 후에만 기록 (advisor 가이드)
            if (keyWasCreated)
            {
                Log.Information("[AUDIT] HMAC 키 생성: {Path}", HmacKeyProvider.KeyPath);
            }
        }

        /// <summary>
        /// D-17c: 로그 디렉터리에서 <paramref name="retainDays"/> 일 초과 파일을 삭제한다.
        /// 파일 수정일 기준. 삭제 실패는 조용히 삼킨다(초기화 경로 안정성 우선).
        /// </summary>
        private static void CleanupOldLogs(string logDir, int retainDays)
        {
            try
            {
                DateTime cutoff = DateTime.Now.AddDays(-retainDays);
                foreach (var file in Directory.GetFiles(logDir, "ASLT-*.log"))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime < cutoff)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // 개별 파일 삭제 실패는 무시 — 나머지 파일 계속 정리
                    }
                }
            }
            catch
            {
                // Directory.GetFiles 실패도 무시 — 초기화 경로 실패 방지
            }
        }

        #endregion

        #region Audit Events (기존 4종)

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

        #region Audit Events (D-17a 확장 9종)

        /// <summary>
        /// [AUDIT] 영상 파일 로드 성공 이벤트를 기록한다.
        /// </summary>
        /// <param name="videoPath">로드된 영상 파일 경로</param>
        public static void AuditVideoLoad(string videoPath)
        {
            Log.Information("[AUDIT] 영상 로드: {Path}", videoPath);
        }

        /// <summary>
        /// [AUDIT] 사용자가 BBOX 를 생성한 이벤트를 기록한다.
        /// Undo/Redo 재실행 경로, 프로그래밍적 전파(propagation) 경로에서는 호출하지 않는다 — 신호 품질 유지.
        /// </summary>
        /// <param name="label">클래스 라벨 (person/vehicle/event)</param>
        /// <param name="id">객체 ID</param>
        /// <param name="frameIndex">프레임 번호</param>
        public static void AuditBBoxCreate(string label, int id, int frameIndex)
        {
            Log.Information("[AUDIT] BBOX 생성: {Label}_{Id:D2} frame={Frame}", label, id, frameIndex);
        }

        /// <summary>
        /// [AUDIT] 사용자가 BBOX 를 삭제한 이벤트를 기록한다.
        /// Undo/Redo 재실행 경로, 프로그래밍적 정리(exit-shrink) 경로에서는 호출하지 않는다.
        /// </summary>
        public static void AuditBBoxDelete(string label, int id, int frameIndex)
        {
            Log.Information("[AUDIT] BBOX 삭제: {Label}_{Id:D2} frame={Frame}", label, id, frameIndex);
        }

        /// <summary>
        /// [AUDIT] Waypoint 생성 이벤트를 기록한다.
        /// </summary>
        public static void AuditWaypointCreate(string label, int objectId, int entryFrame, int exitFrame)
        {
            Log.Information("[AUDIT] Waypoint 생성: {Label}_{Id:D2} [{EntryFrame}-{ExitFrame}]",
                label, objectId, entryFrame, exitFrame);
        }

        /// <summary>
        /// [AUDIT] Waypoint 삭제 이벤트를 기록한다.
        /// </summary>
        public static void AuditWaypointDelete(string label, int objectId, int entryFrame, int exitFrame)
        {
            Log.Information("[AUDIT] Waypoint 삭제: {Label}_{Id:D2} [{EntryFrame}-{ExitFrame}]",
                label, objectId, entryFrame, exitFrame);
        }

        /// <summary>
        /// [AUDIT] JSON 파일 로드 성공 이벤트를 기록한다.
        /// </summary>
        public static void AuditJsonLoad(string jsonPath)
        {
            Log.Information("[AUDIT] JSON 로드: {Path}", jsonPath);
        }

        /// <summary>
        /// [AUDIT] JSON 파일 삭제 이벤트를 기록한다.
        /// Wave 3 에서 JsonService.DeleteJsonForVideo 내부에 심은 "[AUDIT] JSON 파일 삭제" 원시 호출의 상위 래퍼.
        /// </summary>
        public static void AuditJsonDelete(string jsonPath)
        {
            Log.Information("[AUDIT] JSON 파일 삭제: {Path}", jsonPath);
        }

        /// <summary>
        /// [AUDIT] JSON 내보내기(Export) 성공 이벤트를 기록한다.
        /// </summary>
        public static void AuditExport(string jsonPath, int imageCount, int annotationCount)
        {
            Log.Information("[AUDIT] 내보내기: {Path} (images={Images}, annotations={Annotations})",
                jsonPath, imageCount, annotationCount);
        }

        /// <summary>
        /// [AUDIT] 글로벌 예외 핸들러에서 처리되지 않은 예외 발생 이벤트를 기록한다.
        /// </summary>
        /// <param name="where">발생 위치 (예: "UIThread", "BackgroundThread")</param>
        /// <param name="ex">예외 객체 (null 허용)</param>
        public static void AuditException(string where, Exception? ex)
        {
            Log.Error(ex, "[AUDIT] 예외 발생: {Where} — {Message}", where, ex?.Message ?? "(null)");
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
