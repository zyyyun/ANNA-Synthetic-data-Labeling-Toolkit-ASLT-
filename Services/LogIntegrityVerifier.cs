using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ASLTv1.Services
{
    /// <summary>
    /// D-17b: HMAC 무결성 체인 검증 유틸리티.
    /// 지정 로그 파일의 각 라인에 부착된 `|prev_hmac=...|hmac=...` 를 순차적으로 재계산하여
    /// 변조된 라인이 있는지 검출한다. 감사 로그 신뢰성의 외부 검증 수단.
    /// </summary>
    public static class LogIntegrityVerifier
    {
        private const string GENESIS = "GENESIS";
        private const string PREV_MARKER = "|prev_hmac=";
        private const string HMAC_MARKER = "|hmac=";

        /// <summary>
        /// 검증 결과.
        /// </summary>
        public class Result
        {
            public bool IsValid { get; set; }
            public int? TamperedLine { get; set; }
            public string Reason { get; set; } = "";
            public int TotalLines { get; set; }
            public int VerifiedLines { get; set; }
        }

        /// <summary>
        /// 로그 파일의 HMAC 체인을 순차 검증한다.
        /// </summary>
        /// <param name="logFilePath">검증할 로그 파일 경로</param>
        /// <param name="key">HmacKeyProvider.GetOrCreateKey() 로 얻은 32-byte 키</param>
        /// <param name="initialPrevHmac">첫 라인의 expected prev_hmac — GENESIS(파일이 첫날) 또는 전날 파일의 마지막 hmac</param>
        /// <returns>검증 결과. IsValid=true 면 체인 온전, false 면 TamperedLine 에 변조 감지 라인 번호.</returns>
        public static Result VerifyLogFile(string logFilePath, byte[] key, string initialPrevHmac = GENESIS)
        {
            if (string.IsNullOrEmpty(logFilePath))
                return new Result { IsValid = false, Reason = "로그 파일 경로가 비어있습니다." };

            if (key == null || key.Length == 0)
                return new Result { IsValid = false, Reason = "HMAC 키가 비어있습니다." };

            if (!File.Exists(logFilePath))
                return new Result { IsValid = false, Reason = $"파일이 존재하지 않습니다: {logFilePath}" };

            string prev = initialPrevHmac ?? GENESIS;
            int lineNum = 0;
            int verified = 0;

            try
            {
                using var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNum++;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    int hmacIdx = line.LastIndexOf(HMAC_MARKER, StringComparison.Ordinal);
                    int prevIdx = line.LastIndexOf(PREV_MARKER, StringComparison.Ordinal);

                    if (hmacIdx < 0 || prevIdx < 0 || prevIdx > hmacIdx)
                    {
                        return new Result
                        {
                            IsValid = false,
                            TamperedLine = lineNum,
                            Reason = "HMAC 마커 누락 또는 순서 오류",
                            TotalLines = lineNum,
                            VerifiedLines = verified
                        };
                    }

                    string baseLine = line.Substring(0, prevIdx);
                    int prevStart = prevIdx + PREV_MARKER.Length;
                    string prevHmac = line.Substring(prevStart, hmacIdx - prevStart);
                    string actualHmac = line.Substring(hmacIdx + HMAC_MARKER.Length);

                    if (!prevHmac.Equals(prev, StringComparison.Ordinal))
                    {
                        return new Result
                        {
                            IsValid = false,
                            TamperedLine = lineNum,
                            Reason = $"prev_hmac 체인 불일치 (expected={prev}, got={prevHmac})",
                            TotalLines = lineNum,
                            VerifiedLines = verified
                        };
                    }

                    string expected = ComputeHmacHex(key, baseLine + PREV_MARKER + prevHmac);
                    if (!expected.Equals(actualHmac, StringComparison.Ordinal))
                    {
                        return new Result
                        {
                            IsValid = false,
                            TamperedLine = lineNum,
                            Reason = "HMAC 재계산 불일치 — 라인 내용이 변조됨",
                            TotalLines = lineNum,
                            VerifiedLines = verified
                        };
                    }

                    prev = actualHmac;
                    verified++;
                }

                return new Result
                {
                    IsValid = true,
                    Reason = "체인 무결성 검증 완료",
                    TotalLines = lineNum,
                    VerifiedLines = verified
                };
            }
            catch (Exception ex)
            {
                return new Result
                {
                    IsValid = false,
                    Reason = $"검증 중 예외 발생: {ex.GetType().Name}: {ex.Message}",
                    TotalLines = lineNum,
                    VerifiedLines = verified
                };
            }
        }

        private static string ComputeHmacHex(byte[] key, string data)
        {
            using var h = new HMACSHA256(key);
            byte[] hash = h.ComputeHash(Encoding.UTF8.GetBytes(data));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
