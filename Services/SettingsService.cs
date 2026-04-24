using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace ASLTv1.Services
{
    /// <summary>
    /// DF-1-11 (D-14): 앱 설정 로컬 저장소 (%LOCALAPPDATA%\ANNA\ASLT\settings.json).
    /// 온보딩 표시 여부 등 사용자 선호를 영속화한다.
    /// LogService 와 동일한 LocalAppData 루트를 사용하여 권한 이슈(Program Files 쓰기 불가)를 회피.
    /// </summary>
    public static class SettingsService
    {
        private static readonly string SettingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ANNA", "ASLT");
        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        /// <summary>앱 수준 사용자 선호 설정 컨테이너.</summary>
        public class AppSettings
        {
            /// <summary>온보딩 가이드 팝업을 이미 봤는지 여부 — true 면 재실행 시 표시하지 않는다.</summary>
            public bool OnboardingShown { get; set; } = false;
        }

        private static AppSettings _cached;

        /// <summary>
        /// 설정을 로드한다. 파일이 없거나 역직렬화 실패 시 기본값을 반환한다.
        /// 결과는 프로세스 내에서 캐시된다.
        /// </summary>
        public static AppSettings Load()
        {
            if (_cached != null) return _cached;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    _cached = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _cached = new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[Settings 로드 실패] 기본값 사용: {Path}", SettingsPath);
                _cached = new AppSettings();
            }
            return _cached;
        }

        /// <summary>
        /// 설정을 JSON 으로 직렬화하여 %LOCALAPPDATA%\ANNA\ASLT\settings.json 에 저장한다.
        /// I/O 실패는 로그만 남기고 throw 하지 않는다 (앱 동작에 영향 금지).
        /// </summary>
        public static void Save(AppSettings settings)
        {
            if (settings == null) return;
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
                _cached = settings;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Settings 저장 실패] {Path}", SettingsPath);
            }
        }

        /// <summary>온보딩 미표시 상태(= 최초 실행 또는 '다시 보지 않기' 미체크).</summary>
        public static bool IsFirstRun() => !Load().OnboardingShown;

        /// <summary>'다시 보지 않기' 체크 후 완료 시 호출. OnboardingShown=true 로 영속화.</summary>
        public static void MarkOnboardingShown()
        {
            var s = Load();
            s.OnboardingShown = true;
            Save(s);
        }
    }
}
