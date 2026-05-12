using System.Runtime.InteropServices;

namespace ASLTv1.Helpers
{
    /// <summary>
    /// winmm.dll timeBeginPeriod / timeEndPeriod P/Invoke wrapper.
    /// Windows OS scheduler granularity 를 1ms 로 변경 (기본 ~15.625ms).
    /// System.Windows.Forms.Timer 등 코어 타이밍의 정확도 확보용 (미디어/게임 앱 표준 패턴).
    /// BeginPeriod 호출 횟수와 EndPeriod 호출 횟수는 반드시 1:1 매칭되어야 함.
    /// PerfLog 와 무관 — 정상 사용자도 적용됨 (30fps 영상의 정확한 재생).
    /// </summary>
    internal static class WinmmTimer
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        internal static extern uint BeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        internal static extern uint EndPeriod(uint uMilliseconds);
    }
}
