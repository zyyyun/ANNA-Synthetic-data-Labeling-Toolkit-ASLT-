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

        // PERF-V2-JITTER-INST: Playback frame 간격 누적 (30프레임 윈도우)
        public static long LastLoadFrameTimestamp { get; set; } = 0;
        public static int FrameCounter { get; set; } = 0;
        public static long IntervalSumTicks { get; set; } = 0;
        public static long MaxIntervalTicks { get; set; } = 0;

        // PERF-V2-JITTER-INST: PictureBox 자동 스케일링 latency (image set → paint 시작)
        public static long LastImageSetTimestamp { get; set; } = 0;

        // PERF-V2-JITTER-INST: GC Gen2 collection delta (large bitmap 압박 추적)
        public static int LastGen2Count { get; set; } = 0;
    }
}
