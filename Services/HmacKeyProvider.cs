using System;
using System.IO;
using System.Security.Cryptography;

namespace ASLTv1.Services
{
    /// <summary>
    /// D-17b: HMAC 체인용 비밀키 관리.
    /// %LOCALAPPDATA%\ANNA\ASLT\.hmac-key 에 32 bytes 랜덤 키를 보관한다.
    /// 머신 최초 실행 시 자동 생성되며, 이후 재사용된다.
    /// KISA 가이드 — 키는 ACL 로 현재 사용자만 접근 가능하도록 Hidden 속성 + LocalApplicationData 경로 사용.
    /// </summary>
    public static class HmacKeyProvider
    {
        private const int KEY_SIZE_BYTES = 32;

        /// <summary>
        /// HMAC 비밀키 디렉터리: %LOCALAPPDATA%\ANNA\ASLT
        /// </summary>
        private static readonly string KeyDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANNA", "ASLT");

        /// <summary>
        /// HMAC 비밀키 파일 경로: %LOCALAPPDATA%\ANNA\ASLT\.hmac-key
        /// </summary>
        public static readonly string KeyPath = Path.Combine(KeyDir, ".hmac-key");

        /// <summary>
        /// HMAC 비밀키를 가져오거나, 없으면 새로 생성한다.
        /// **주의: 이 메서드는 LogService.Initialize 이전에 호출될 수 있으므로 Log.* 을 사용하지 않는다.**
        /// 키 생성 이벤트는 caller(LogService.Initialize) 가 logger 초기화 후 기록한다.
        /// </summary>
        /// <param name="wasCreated">키가 새로 생성되었으면 true, 기존 키 재사용 시 false</param>
        /// <returns>32 bytes HMAC 비밀키</returns>
        public static byte[] GetOrCreateKey(out bool wasCreated)
        {
            wasCreated = false;
            Directory.CreateDirectory(KeyDir);

            if (File.Exists(KeyPath))
            {
                try
                {
                    byte[] existing = File.ReadAllBytes(KeyPath);
                    if (existing.Length == KEY_SIZE_BYTES) return existing;
                    // 파일 크기 비정상 — 재생성 필요
                }
                catch
                {
                    // 읽기 실패 시 재생성 fallback
                }
            }

            byte[] key = new byte[KEY_SIZE_BYTES];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(key);

            File.WriteAllBytes(KeyPath, key);
            try { File.SetAttributes(KeyPath, FileAttributes.Hidden); } catch { /* best-effort — 일부 파일 시스템에서 미지원 */ }

            wasCreated = true;
            return key;
        }

        /// <summary>
        /// 편의 오버로드 — wasCreated 플래그가 필요 없을 때 사용.
        /// </summary>
        public static byte[] GetOrCreateKey()
        {
            return GetOrCreateKey(out _);
        }
    }
}
