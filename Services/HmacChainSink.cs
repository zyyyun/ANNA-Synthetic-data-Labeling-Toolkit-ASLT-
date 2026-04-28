using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace ASLTv1.Services
{
    /// <summary>
    /// D-17b: Serilog ILogEventSink 구현 — 각 로그 라인에 `|prev_hmac=...|hmac=...` 부착.
    /// HMAC-SHA256 기반 체인으로 로그 변조를 검출할 수 있다.
    ///
    /// - 파일 경로 포맷: {baseDir}\ASLT-{yyyy-MM-dd}.log (Serilog RollingInterval.Day 호환)
    /// - 첫 라인 prev_hmac: "GENESIS" (파일 부재 시)
    /// - 일자 경계 (rollover): **프로세스 내에서 발생하는 rollover 는 in-memory 의 _lastHmac 을 유지**
    ///   새 파일의 첫 라인이 어제의 마지막 hmac 를 prev 로 참조하여 cross-day chain continuity 유지.
    /// - 앱 재시작 후 기존 파일에 append 하는 경우엔 디스크에서 마지막 hmac 복구 (recovery path).
    /// </summary>
    public class HmacChainSink : ILogEventSink
    {
        private const string GENESIS = "GENESIS";
        private const string PREV_MARKER = "|prev_hmac=";
        private const string HMAC_MARKER = "|hmac=";

        private readonly string _logDir;
        private readonly string _baseName;   // e.g. "ASLT-"
        private readonly string _ext;        // e.g. ".log"
        private readonly ITextFormatter _formatter;
        private readonly byte[] _key;
        private readonly object _lock = new object();
        private string? _lastHmac;      // 마지막으로 기록한 HMAC — cross-day chain 유지
        private string? _lastFilePath;  // 현재 일자 파일 경로 — 경계 감지용

        public HmacChainSink(string pathTemplate, ITextFormatter formatter, byte[] key)
        {
            if (string.IsNullOrEmpty(pathTemplate)) throw new ArgumentNullException(nameof(pathTemplate));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _key = key ?? throw new ArgumentNullException(nameof(key));

            _logDir = Path.GetDirectoryName(pathTemplate) ?? ".";
            _baseName = Path.GetFileNameWithoutExtension(pathTemplate);  // "ASLT-"
            _ext = Path.GetExtension(pathTemplate);                       // ".log"

            Directory.CreateDirectory(_logDir);
        }

        /// <summary>
        /// Serilog 가 로그 이벤트 발생 시 호출. thread-safe (lock).
        /// </summary>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            lock (_lock)
            {
                try
                {
                    string dateStr = logEvent.Timestamp.ToString("yyyy-MM-dd");
                    string filePath = Path.Combine(_logDir, $"{_baseName}{dateStr}{_ext}");

                    // 일자 경계 (rollover) 감지:
                    // - 프로세스 내 자연 rollover: _lastHmac 유지 (어제 마지막 hmac 를 새 파일 첫 라인의 prev 로 사용)
                    // - 앱 재시작 후 기존 파일 append: 디스크에서 마지막 hmac 복구
                    if (_lastFilePath != filePath)
                    {
                        if (File.Exists(filePath))
                        {
                            // 복구 경로: 이 파일이 이미 내용 있음 → 마지막 hmac 를 이어받는다
                            _lastHmac = ReadLastHmac(filePath);
                        }
                        else if (_lastFilePath == null)
                        {
                            // 프로세스 내 첫 emit + 파일도 부재:
                            // 이전 일자 파일이 있다면 그 파일의 마지막 hmac 로 체인 이어가기
                            string? priorHmac = TryReadLastHmacOfMostRecentPriorFile();
                            _lastHmac = priorHmac; // null 이면 GENESIS 로 처리됨
                        }
                        // else: 프로세스 내 자연 rollover (_lastFilePath != null && new file) → _lastHmac 그대로 유지
                        _lastFilePath = filePath;
                    }

                    // 베이스 메시지 직렬화
                    string baseLine;
                    using (var writer = new StringWriter())
                    {
                        _formatter.Format(logEvent, writer);
                        baseLine = writer.ToString().TrimEnd('\r', '\n');
                    }

                    string prevHmac = _lastHmac ?? GENESIS;
                    string payload = baseLine + PREV_MARKER + prevHmac;
                    string hmacHex = ComputeHmacHex(payload);

                    string finalLine = baseLine + PREV_MARKER + prevHmac + HMAC_MARKER + hmacHex + Environment.NewLine;

                    // 방어선: 파일이 외부에서 열려있어 Append 실패 시 Debug.WriteLine 로 fallback.
                    // 글로벌 예외 핸들러 → Log.* → Emit → 실패 → throw → 재귀 로 인한 스택오버플로 방지.
                    try
                    {
                        File.AppendAllText(filePath, finalLine, Encoding.UTF8);
                        _lastHmac = hmacHex;
                    }
                    catch (IOException ioex)
                    {
                        Debug.WriteLine($"[HmacChainSink] 로그 파일 쓰기 실패 (공유 위반): {ioex.Message}");
                        // _lastHmac 를 업데이트하지 않음 — 다음 Emit 이 동일한 prev 로 시도
                    }
                    catch (UnauthorizedAccessException uex)
                    {
                        Debug.WriteLine($"[HmacChainSink] 로그 파일 쓰기 실패 (권한): {uex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // 마지막 방어선 — Sink 에서 예외가 밖으로 나가면 Serilog 가 ActiveLogger를 중단하거나
                    // 글로벌 예외 핸들러와 충돌할 수 있음.
                    Debug.WriteLine($"[HmacChainSink] Emit 예외: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 지정 파일의 마지막 비공백 라인에서 hmac 값을 추출한다.
        /// 반환값 null = 파일 없음 또는 파싱 불가.
        /// </summary>
        private static string? ReadLastHmac(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                string? last = null;
                // FileShare.ReadWrite 로 열어 Serilog 자체 로테이션 동작과 경합 최소화
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line)) last = line;
                }
                if (string.IsNullOrEmpty(last)) return null;

                int idx = last!.LastIndexOf(HMAC_MARKER, StringComparison.Ordinal);
                if (idx < 0) return null;
                return last.Substring(idx + HMAC_MARKER.Length).TrimEnd();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HmacChainSink.ReadLastHmac] {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 로그 디렉터리에서 "ASLT-*.log" 파일 중 가장 최근(파일명의 날짜 기준) 파일의 마지막 hmac 를 읽는다.
        /// cross-day 체인 복구에 사용.
        /// </summary>
        private string? TryReadLastHmacOfMostRecentPriorFile()
        {
            try
            {
                var files = Directory.GetFiles(_logDir, $"{_baseName}*{_ext}");
                if (files.Length == 0) return null;
                // 파일명 오름차순 정렬 → 마지막이 가장 최근 일자
                Array.Sort(files, StringComparer.Ordinal);
                string mostRecent = files[files.Length - 1];
                return ReadLastHmac(mostRecent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HmacChainSink.TryReadLastHmacOfMostRecentPriorFile] {ex.Message}");
                return null;
            }
        }

        private string ComputeHmacHex(string data)
        {
            using var h = new HMACSHA256(_key);
            byte[] hash = h.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
