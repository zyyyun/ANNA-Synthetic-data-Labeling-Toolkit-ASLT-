using System;
using System.Drawing;
using System.Windows.Forms;
using ASLTv1.Services;
using ASLTv1.Theme;

namespace ASLTv1.Forms
{
    /// <summary>
    /// DF-1-11 (D-14): 최초 실행 시 3단계 가이드 팝업.
    /// 동영상 선택 → BBOX 생성 → Entry/Exit 설정 흐름을 카드 전환형 UI 로 안내.
    /// "다시 보지 않기" 체크 후 '완료' 시 SettingsService.MarkOnboardingShown() 호출.
    /// 다크 테마는 DarkTheme.Apply(this) 로 일괄 적용.
    /// </summary>
    public partial class OnboardingForm : Form
    {
        private int _currentStep = 0;
        private readonly (string Title, string Body)[] _steps = new[]
        {
            ("1. 동영상 선택",
             "상단 툴바의 '파일 선택' 버튼을 누르거나\n" +
             "메뉴에서 라벨링할 비디오 파일(MP4, AVI 등 H.264 코덱)을 선택하세요.\n\n" +
             "선택된 영상은 자동으로 첫 프레임부터 표시되며,\n" +
             "같은 폴더에 동명의 .srt 파일이 있으면 자막이 함께 로드됩니다."),
            ("2. BBOX 생성",
             "F1(Person) / F2(Vehicle) / F3(Event) 로 클래스를 선택한 뒤,\n" +
             "비디오 화면에서 마우스를 드래그해 객체 영역을 지정하세요.\n\n" +
             "Ctrl+1 ~ Ctrl+0 : 객체 ID 부여 (1~10)\n" +
             "Alt+1 ~ Alt+0  : Person 확장 ID (11~20)\n" +
             "Ctrl+Z / Ctrl+Y : 실행 취소 / 다시 실행"),
            ("3. Entry/Exit 설정",
             "추적을 시작할 프레임에서 Entry 버튼(또는 E 키),\n" +
             "끝나는 프레임에서 Exit 버튼(또는 X 키)을 눌러\n" +
             "Waypoint 구간을 설정합니다.\n\n" +
             "Ctrl+N : Exit 프레임 BBOX ID 를 Entry 와 자동 매칭\n" +
             "Ctrl+S : COCO JSON 형식으로 저장\n\n" +
             "Waypoint 목록은 우측 사이드바에서 확인/편집할 수 있습니다.")
        };

        public OnboardingForm() : this(showDoNotShowAgain: true) { }

        /// <param name="showDoNotShowAgain">
        /// false 일 경우 "다시 보지 않기" 체크박스를 숨긴다 ("가이드 켜기" 버튼으로 수동 진입한 경로).
        /// true(기본) 는 첫 실행 자동 표시 경로 — 사용자가 영구 비표시를 선택할 수 있어야 한다.
        /// </param>
        public OnboardingForm(bool showDoNotShowAgain)
        {
            InitializeComponent();
            DarkTheme.Apply(this);
            chkDoNotShowAgain.Visible = showDoNotShowAgain;
            RenderStep();
        }

        private void RenderStep()
        {
            labelTitle.Text = _steps[_currentStep].Title;
            labelBody.Text = _steps[_currentStep].Body;
            labelProgress.Text = $"{_currentStep + 1} / {_steps.Length}";
            btnPrev.Enabled = _currentStep > 0;
            btnNext.Text = _currentStep == _steps.Length - 1 ? "완료" : "다음";
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (_currentStep > 0)
            {
                _currentStep--;
                RenderStep();
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (_currentStep < _steps.Length - 1)
            {
                _currentStep++;
                RenderStep();
                return;
            }
            // 마지막 단계 '완료' 버튼
            if (chkDoNotShowAgain.Checked)
            {
                SettingsService.MarkOnboardingShown();
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
