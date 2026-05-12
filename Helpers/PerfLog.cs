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
