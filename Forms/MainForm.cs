using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ASLTv1.Models;
using ASLTv1.Services;
using ASLTv1.Helpers;
using ASLTv1.Theme;

namespace ASLTv1.Forms
{
    public enum DrawMode { Select, Draw }

    public partial class MainForm : Form
    {
        #region Constants

        private const int HANDLE_SIZE = 10;
        private const int MIN_BBOX_SIZE = 10;
        private const int MAX_UNDO_STACK = 100;
        private const int RESIZE_BORDER_WIDTH = 6;
        private const int HIT_MARGIN = 4;

        #endregion

        #region Services

        private VideoService _videoService;
        private JsonService _jsonService;
        private CancellationTokenSource _videoLoadCts;

        #endregion

        #region Annotation State

        private List<BoundingBox> boundingBoxes = new List<BoundingBox>();
        private Dictionary<int, List<BoundingBox>> _bboxByFrame = null;
        private BoundingBox selectedBox;
        private BoundingBox drawingBox;
        private List<WaypointMarker> waypointMarkers = new List<WaypointMarker>();
        private WaypointMarker selectedWaypoint;
        private Dictionary<int, CategoryData> categoryMap = new Dictionary<int, CategoryData>();
        private int nextAnnotationId = 1;
        private string currentSelectedLabel = "person";
        private int currentAssignedId = 1;
        private string currentJsonFile = "";
        private Dictionary<int, string> frameTimestampMap = new Dictionary<int, string>();

        #endregion

        #region UI State

        private DrawMode currentMode = DrawMode.Select;
        private bool isDrawing;
        private bool isDragging;
        private bool isResizing;
        private ResizeHandle currentResizeHandle = ResizeHandle.None;
        private System.Drawing.Point drawStartPoint;
        private System.Drawing.Point dragOffset;
        private System.Drawing.Point resizeStartPoint;
        private Rectangle originalResizeRect;
        // DF-1-07 (05.6-04 Task 5-B): MouseDown 시 drag 시작 전 Rectangle 스냅샷 — MouseUp 에서 변경된 경우에만 ModifyBox undo 푸시
        private Rectangle originalDragRect;
        private bool isWaitingForDoubleClick;
        private System.Drawing.Point lastClickPoint;
        private System.Threading.Timer doubleClickTimer;
        private int? entryFrameIndex;
        private int? exitFrameIndex;
        private bool suppressWaypointClickOnce;
        private bool _isDirty = false;
        private bool _isVideoLoading;  // RELI-06: 영상 로드 중이면 true — UI 잠금 트리거
        // RELI-06 (05.5-02 gap closure): 첫 프레임이 실제로 화면에 페인트된 직후 true 로 전이.
        // LoadFrame 및 모든 사용자-트리거 탐색 경로에서 이 플래그로 cold-decoder 구간의 seek 폭주를 차단한다.
        private bool _isVideoReady;
        // 자동 재생을 first-paint 이후로 연기하기 위한 one-shot 플래그.
        private bool _pendingAutoPlay;
        /// <summary>
        /// DF-1-05 (D-09): Waypoint 생성 등으로 자동 저장된 JSON 경로를 추적.
        /// 영상 전환 또는 앱 종료 시 "저장하지 않은 편집" 프롬프트에서 '아니요' 선택 시 롤백 삭제 대상.
        /// 저장 성공 시 <see cref="SaveCurrentLabelingData"/> 에서 세팅되고, 새 영상 로드 시 리셋된다.
        /// </summary>
        private string _autoSavedJsonPath = null;

        #endregion

        #region Rendering

        private Font labelFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private int lastCachedFrameForPaint = -1;
        private List<BoundingBox> cachedCurrentFrameBoxes = new List<BoundingBox>();

        #endregion

        #region Window Drag

        private bool isMovingWindow;
        private System.Drawing.Point windowMoveStartPoint;

        #endregion

        #region Undo/Redo

        private Stack<UndoAction> undoStack = new Stack<UndoAction>();
        private Stack<UndoAction> redoStack = new Stack<UndoAction>();

        #endregion

        #region Timeline/Playback

        private float timelineProgress;
        private bool isTimelineDragging;
        private double playbackSpeed = 1.0;
        private long lastFrameTime;
        private double msPerFrame;
        private bool isPlaying;

        #endregion

        #region Hit Testing

        private System.Drawing.Point lastClickViewPoint;
        private List<BoundingBox> lastHitCandidates = new List<BoundingBox>();
        private int lastHitIndex = -1;

        #endregion

        #region Event Inline Editing

        private TextBox eventListEditBox;

        #endregion

        #region Constructor & Init

        public MainForm()
        {
            InitializeComponent();

            _videoService = new VideoService();
            _jsonService = new JsonService();

            UpdateBoxCount();

            // Subtitle initial state
            btnToggleSubtitle.Text = "자막 열기";
            btnToggleSubtitle.BackColor = Color.FromArgb(100, 116, 139);
            if (labelSubtitleTimestamp != null)
            {
                labelSubtitleTimestamp.Visible = false;
            }

            EnableDoubleBuffering(panelTimeline);
            SetupWindowDragHandlers();
        }

        private void EnableDoubleBuffering(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, control, new object[] { true });
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            DarkTheme.Apply(this);

            // FUNC-11 (DF-2-05): 폼 최초 표시 시점에 좌측 모드 버튼 시각을 currentMode 기본값(Select) 과 동기화
            SetMode(DrawMode.Select);

            btnSelectFolder.Text = "파일 선택";

            this.Focus();
            this.Activate();

            listViewPersonWaypoints.KeyDown += (s, ev) => HandleListViewKeyDown(s, ev);
            listViewVehicleWaypoints.KeyDown += (s, ev) => HandleListViewKeyDown(s, ev);
            listViewEventWaypoints.KeyDown += (s, ev) => HandleListViewKeyDown(s, ev);

            _videoService.SetupFFmpegPath();
            if (!_videoService.IsFFmpegAvailable)
            {
                Log.Warning("FFmpeg 미설치 상태로 시작합니다.");
                MessageBox.Show(
                    "FFmpeg가 설치되지 않았습니다.\n\n" +
                    "자막 추출 기능을 사용하려면 다음 중 하나를 수행하세요:\n" +
                    "1. FFmpeg를 설치하고 시스템 PATH에 추가\n" +
                    "2. 프로그램 폴더의 ffmpeg/ 하위에 ffmpeg.exe 배치\n\n" +
                    "자막 추출 외 기능은 정상적으로 사용할 수 있습니다.",
                    "FFmpeg 안내",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            this.Resize += MainForm_Resize;
            UpdateMaximizeButtonIcon();

            // 초기 레이아웃에서도 타임라인 너비 적용
            panelVideoControls_Resize(panelVideoControls, EventArgs.Empty);
        }

        /// <summary>
        /// DF-1-11 (D-14): 메인 폼이 처음 표시된 직후 온보딩 가이드 팝업을 띄운다.
        /// SettingsService.IsFirstRun() == true 인 경우에만 표시 — "다시 보지 않기" 체크
        /// + '완료' 를 누르면 settings.json 에 OnboardingShown=true 영속화되어 재표시되지 않는다.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (Services.SettingsService.IsFirstRun())
            {
                try
                {
                    using (var onboard = new OnboardingForm())
                    {
                        onboard.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[온보딩 표시 실패] 앱 동작에 영향 없이 계속 진행");
                }
            }
        }

        private void HandleListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                MainForm_KeyDown(this, e);
                if (e.Handled) return;
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            UpdateMaximizeButtonIcon();
        }

        #endregion

        #region Window Controls

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMaximize_Click(object sender, EventArgs e)
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        private void btnMinimize_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;

        private void UpdateMaximizeButtonIcon()
        {
            btnMaximize.Text = this.WindowState == FormWindowState.Maximized ? "❐" : "□";
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            using (var aboutForm = new AboutForm())
            {
                aboutForm.ShowDialog(this);
            }
        }

        private void btnShowGuide_Click(object sender, EventArgs e)
        {
            try
            {
                // 수동 진입 — "다시 보지 않기" 체크박스 숨김 (영구 비표시는 첫 실행 자동 표시 경로 전용).
                using (var onboard = new OnboardingForm(showDoNotShowAgain: false))
                    onboard.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[가이드 표시 실패] 사용자 트리거");
            }
        }

        #endregion

        #region Video Loading

        private async void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Video Files|*.avi;*.mp4;*.mkv|All Files|*.*";
                ofd.Title = "Select Video File";
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (_isDirty && _videoService != null && _videoService.IsVideoLoaded)
                    {
                        var result = MessageBox.Show(
                            "저장하지 않은 편집이 있습니다. 저장 후 전환하시겠습니까?",
                            "저장 확인",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.Cancel)
                        {
                            return;
                        }
                        else if (result == DialogResult.Yes)
                        {
                            SaveCurrentLabelingData();
                        }
                        else // DialogResult.No — DF-1-05 (D-09): 자동 저장된 JSON 롤백 삭제
                        {
                            if (!string.IsNullOrEmpty(_autoSavedJsonPath) && File.Exists(_autoSavedJsonPath))
                            {
                                try
                                {
                                    File.Delete(_autoSavedJsonPath);
                                    Log.Information("[AUDIT] 영상 전환 시 미저장 JSON 롤백 삭제: {Path}", _autoSavedJsonPath);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "[JSON 롤백 삭제 오류] {Path}", _autoSavedJsonPath);
                                }
                            }
                            _autoSavedJsonPath = null;
                        }
                    }
                    await LoadVideoWithSubtitle(ofd.FileName);
                }
            }
        }

        private async Task LoadVideoWithSubtitle(string filePath)
        {
            // Cancel any previous in-flight load (RELI-03)
            _videoLoadCts?.Cancel();
            _videoLoadCts?.Dispose();
            _videoLoadCts = new CancellationTokenSource();
            var token = _videoLoadCts.Token;

            // Stop playback before loading new video (PERF-02)
            if (isPlaying)
            {
                isPlaying = false;
                btnPlay.Text = "\u25B6";
                timerPlayback.Stop();
            }

            // RELI-06: UI 잠금 + 로딩 라벨 표시
            _isVideoLoading = true;
            // RELI-06 (05.5-02 gap closure): ready-gate 리셋 — 첫 프레임 페인트까지 false 유지
            _isVideoReady = false;
            _pendingAutoPlay = false;
            panelVideoControls.Enabled = false;
            labelLoading.Visible = true;
            CenterLoadingLabel();
            labelLoading.BringToFront();
            Application.DoEvents();

            try
            {
                await _videoService.LoadVideoAsync(filePath, token);
                token.ThrowIfCancellationRequested();

                if (!_videoService.IsVideoLoaded)
                {
                    // DF-1-14 (D-15): 영어 메시지 → 한국어 통일, 제목 "Error" → "오류"
                    MessageBox.Show(
                        "비디오 파일을 열 수 없습니다.\n\n" +
                        "원인: 파일이 손상되었거나 지원하지 않는 확장자일 수 있습니다.\n" +
                        "해결 방법: MP4(H.264) 등 지원 형식의 파일인지 확인하세요.",
                        "오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Reset dirty state on successful video load (USAB-02)
                _isDirty = false;
                // DF-1-05 (D-09): 새 영상 로드 시 이전 영상 자동 저장 경로 리셋 — 롤백 대상 아님
                _autoSavedJsonPath = null;

                // Show first frame
                var bitmap = _videoService.LoadFrame(0);
                if (bitmap != null)
                {
                    pictureBoxVideo.Image?.Dispose();
                    pictureBoxVideo.Image = bitmap;
                }

                UpdateTimeLabels();
                // 제목은 "ASLT(v1.0)"으로 고정 — 영상 파일명 표기하지 않음 (사용자 요청)

                this.Focus();
                this.Activate();

                // Load JSON
                await LoadLabelingData(filePath);

                // RELI-06 (05.5-02 gap closure): 자동 재생을 first-paint handshake 로 연기.
                // _isVideoReady 는 pictureBoxVideo.Paint 에서 첫 프레임이 실제 페인트된 직후 true 로 전이되고,
                // 이 때 _pendingAutoPlay 가 소비되어 btnPlay_Click 이 호출된다.
                // 기존 code path(즉시 btnPlay_Click)는 cold-decoder 완료 전 timer 가 seek 폭주하는 원인이었음.
                _pendingAutoPlay = true;
                // Race: Paint 이벤트가 이미 fire 되어 _isVideoReady 가 true 로 전이된 경우
                // (LoadLabelingData 의 await 기간 중 paint 가 먼저 완료될 수 있음),
                // handshake 는 _pendingAutoPlay 를 false 상태에서 읽었으므로 auto-play 가 누락된다.
                // 이를 복구하기 위한 belt-and-suspenders:
                if (_isVideoReady)
                {
                    _pendingAutoPlay = false;
                    if (!isPlaying)
                    {
                        btnPlay_Click(null, EventArgs.Empty);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Information("[영상 로드] 이전 로드 작업이 취소됨: {FilePath}", filePath);
            }
            catch (IOException ioEx)
            {
                Log.Error(ioEx, "[영상 로드 오류 - I/O] {FilePath}", filePath);
                MessageBox.Show($"비디오 파일을 읽을 수 없습니다.\n\n" +
                    $"파일: {Path.GetFileName(filePath)}\n" +
                    $"원인: {ioEx.Message}\n\n" +
                    $"해결 방법: 파일이 존재하고 다른 프로그램에서 사용 중이지 않은지 확인하세요.",
                    "파일 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OpenCvSharp.OpenCVException ocvEx)
            {
                Log.Error(ocvEx, "[영상 로드 오류 - 코덱] {FilePath}", filePath);
                MessageBox.Show($"비디오 코덱을 지원하지 않습니다.\n\n" +
                    $"파일: {Path.GetFileName(filePath)}\n" +
                    $"원인: {ocvEx.Message}\n\n" +
                    $"해결 방법: MP4(H.264) 형식의 비디오 파일을 사용하세요.",
                    "코덱 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[영상 로드 오류] {FilePath}", filePath);
                MessageBox.Show($"비디오 로드 중 오류가 발생했습니다.\n\n" +
                    $"원인: {ex.Message}\n\n" +
                    $"해결 방법: 프로그램을 재시작하거나 다른 비디오 파일을 시도하세요.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // RELI-06: UI 잠금 해제 + 로딩 라벨 숨김 (성공/실패/취소 모두 커버)
                _isVideoLoading = false;
                if (!IsDisposed)
                {
                    labelLoading.Visible = false;
                    panelVideoControls.Enabled = true;

                    // DF-1-21: paint handshake 안전망 — 첫 영상 로드 시 paint 이벤트가 timing/layout 이슈로
                    // _isVideoReady 전이를 트리거하지 못하면 모든 재생 가드(timer tick, rewind/forward 등)가
                    // silent return 되어 사용자 입력 자체가 무시되는 증상이 보고됨.
                    // 로드가 finally 까지 도달했다는 것은 LoadFrame(0) 동기 완료 후이므로 cold-decoder 보호는
                    // 이미 종료된 시점 — 강제 전이가 안전하며 RELI-06 의 의도(cold seek 폭주 차단)와 충돌하지 않음.
                    if (!_isVideoReady && _videoService.IsVideoLoaded && pictureBoxVideo.Image != null)
                    {
                        _isVideoReady = true;
                        if (_pendingAutoPlay && !isPlaying)
                        {
                            _pendingAutoPlay = false;
                            btnPlay_Click(null, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// RELI-06: 로딩 라벨을 pictureBoxVideo 중앙에 배치.
        /// </summary>
        private void CenterLoadingLabel()
        {
            if (labelLoading == null || pictureBoxVideo == null) return;
            labelLoading.Location = new System.Drawing.Point(
                (pictureBoxVideo.Width - labelLoading.Width) / 2,
                (pictureBoxVideo.Height - labelLoading.Height) / 2);
        }

        private void LoadFrame(int frameIndex)
        {
            try
            {
                if (!_videoService.IsVideoLoaded) return;
                // RELI-06 (05.5-02): cold-decoder 기간 LoadFrame 차단 — 첫 프레임이 페인트되기 전엔 어떠한 seek 도 수행하지 않음.
                // D-08: 조용히 return (로그/다이얼로그 없음). 호출측 가드와 중복이지만 belt-and-suspenders.
                if (!_isVideoReady) return;
                if (frameIndex < 0 || frameIndex >= _videoService.TotalFrames) return;

                var bitmap = _videoService.LoadFrame(frameIndex);
                if (bitmap != null)
                {
                    pictureBoxVideo.Image?.Dispose();
                    pictureBoxVideo.Image = bitmap;
                }

                // Rebind selected box when frame changes
                if (selectedBox != null && selectedBox.FrameIndex != frameIndex)
                {
                    // USAB-08 / 스냅백 버그 수정:
                    // 드래그/리사이즈 중 프레임이 변경될 때 현재 위치를 유지해야 한다.
                    // 기존 코드는 새 프레임의 박스(Waypoint 생성 시 전파된 옛 위치)로
                    // selectedBox를 교체했기 때문에 스냅백이 발생했다.
                    Rectangle? carriedRect = (isDragging || isResizing) ? selectedBox.Rectangle : (Rectangle?)null;
                    string selLabel = selectedBox.Label;
                    int selId = GetBoxId(selectedBox);

                    var rebound = boundingBoxes.FirstOrDefault(b =>
                        b.FrameIndex == frameIndex && b.Label == selLabel && GetBoxId(b) == selId && !b.IsDeleted);
                    if (rebound != null)
                    {
                        // 드래그 중이면 새 프레임 박스에 현재 위치를 덮어써서 스냅백 방지
                        if (carriedRect.HasValue)
                            rebound.Rectangle = carriedRect.Value;
                        selectedBox = rebound;
                        HighlightSelectedBoxInSidebar();
                    }
                    else if (isDragging && carriedRect.HasValue)
                    {
                        // 새 프레임에 박스가 없는 경우 — 웨이포인트 범위 내라면 추적 박스 생성
                        var trackWp = waypointMarkers.FirstOrDefault(w =>
                            w.Label == selLabel && w.ObjectId == selId &&
                            frameIndex >= w.EntryFrame && frameIndex <= w.ExitFrame);
                        if (trackWp != null)
                        {
                            var newTrackBox = new BoundingBox
                            {
                                FrameIndex = frameIndex,
                                Rectangle = carriedRect.Value,
                                Label = selLabel,
                                PersonId = selLabel == "person" ? selId : 0,
                                VehicleId = selLabel == "vehicle" ? selId : 0,
                                EventId = selLabel == "event" ? selId : 0,
                                Action = "waypoint"
                            };
                            boundingBoxes.Add(newTrackBox);
                            InvalidateBoxCache();
                            selectedBox = newTrackBox;
                            HighlightSelectedBoxInSidebar();
                        }
                        else
                        {
                            selectedBox = null;
                            ClearSidebarHighlights();
                        }
                    }
                    else
                    {
                        selectedBox = null;
                        ClearSidebarHighlights();
                    }
                }

                UpdateTimeLabels();

                if (ShouldUpdateBboxList(frameIndex))
                {
                    UpdateBboxListDisplay();
                }

                pictureBoxVideo.Invalidate();
            }
            catch (OpenCvSharp.OpenCVException ocvEx)
            {
                Log.Error(ocvEx, "[프레임 로드 오류 - OpenCV] Frame {Index}", frameIndex);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[프레임 로드 오류] {Message}", ex.Message);
            }
        }

        private bool ShouldUpdateBboxList(int frameIndex)
        {
            // Update on waypoint entry frames or periodically
            return waypointMarkers.Any(w => w.EntryFrame == frameIndex || w.ExitFrame == frameIndex)
                || frameIndex % 30 == 0;
        }

        private void UpdateTimeLabels()
        {
            if (!_videoService.IsVideoLoaded) return;

            int currentFrameIndex = _videoService.CurrentFrameIndex;
            int totalFrames = _videoService.TotalFrames;
            double fps = _videoService.Fps;

            double currentSeconds = currentFrameIndex / fps;
            double totalSeconds = totalFrames / fps;
            TimeSpan currentTime = TimeSpan.FromSeconds(currentSeconds);
            TimeSpan totalTime = TimeSpan.FromSeconds(totalSeconds);

            string subtitleText = _videoService.IsSubtitleVisible ? _videoService.GetCurrentSubtitle() : "";

            if (_videoService.IsSubtitleVisible)
            {
                string timestamp = _videoService.ExtractTimestampFromSubtitle(subtitleText);
                if (!string.IsNullOrEmpty(timestamp))
                {
                    labelSubtitleTimestamp.Text = timestamp;
                    labelSubtitleTimestamp.Visible = true;
                }
                else
                {
                    labelSubtitleTimestamp.Visible = false;
                }
            }
            else
            {
                labelSubtitleTimestamp.Visible = false;
            }

            string speedInfo = $"{playbackSpeed:0.##}x";
            if (!string.IsNullOrEmpty(subtitleText))
            {
                labelTimeInfo.Text = $"{currentTime:hh\\:mm\\:ss} / {totalTime:hh\\:mm\\:ss} {speedInfo}\n자막: {subtitleText}";
            }
            else
            {
                labelTimeInfo.Text = $"{currentTime:hh\\:mm\\:ss} / {totalTime:hh\\:mm\\:ss} {speedInfo}";
            }

            timelineProgress = totalFrames > 0 ? (float)currentFrameIndex / totalFrames : 0;
            panelTimeline.Invalidate();
        }

        #endregion

        #region JSON Loading

        private async Task LoadLabelingData(string videoFilePath)
        {
            try
            {
                var result = await _jsonService.LoadLabelingDataAsync(videoFilePath, _videoService.Fps, _videoService.FrameWidth, _videoService.FrameHeight);

                if (!string.IsNullOrEmpty(result.LoadedFilePath))
                    currentJsonFile = result.LoadedFilePath;
                else
                    currentJsonFile = "";

                // 새 영상 로드 시 이전 라벨링 상태를 항상 초기화
                boundingBoxes.Clear();
                waypointMarkers.Clear();
                selectedBox = null;
                selectedWaypoint = null;
                categoryMap = new Dictionary<int, CategoryData>();
                frameTimestampMap = new Dictionary<int, string>();
                nextAnnotationId = 1;
                labelCurrentJsonFile.Text = "";

                // FUNC-09: 이전 작업 상태 완전 초기화
                // FUNC-11 (DF-2-05): SetMode 헬퍼 호출로 currentMode + 좌측 버튼 시각 + Cursor 를 동시 동기화
                undoStack.Clear();
                redoStack.Clear();
                entryFrameIndex = null;
                exitFrameIndex = null;
                SetMode(DrawMode.Select);
                currentAssignedId = 1;
                isDrawing = false;
                isDragging = false;
                isResizing = false;

                if (result.Success)
                {
                    boundingBoxes.AddRange(result.BoundingBoxes);
                    waypointMarkers.AddRange(result.WaypointMarkers);

                    categoryMap = result.CategoryMap;
                    frameTimestampMap = result.FrameTimestampMap;
                    nextAnnotationId = result.NextAnnotationId;
                }

                InvalidateBoxCache();
                UpdateBoxCount();
                UpdateWaypointListView();
                UpdateBboxListDisplay();
                pictureBoxVideo.Invalidate();

                if (!string.IsNullOrEmpty(currentJsonFile))
                {
                    labelCurrentJsonFile.Text = Path.GetFileName(currentJsonFile);
                }
            }
            catch (IOException ioEx)
            {
                Log.Error(ioEx, "[JSON 로드 오류 - I/O] {Message}", ioEx.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JSON 로드 오류] {Message}", ex.Message);
            }
        }

        #endregion

        #region JSON Export/Delete

        private async void btnExportJson_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_videoService.CurrentVideoFile))
                {
                    MessageBox.Show("먼저 비디오 파일을 로드해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // DF-1-04 (D-08): Waypoint/BBOX 없는 상태에서의 저장 시도 처리
                // — 기존 "저장할 라벨링 데이터가 없습니다." 단일 메시지를 대체하여 기존 JSON 파일이 있으면
                //   삭제 프롬프트를 표시하고, 없으면 알림 메시지만 표시한다.
                if (boundingBoxes.Count == 0 && waypointMarkers.Count == 0)
                {
                    string? existingJson = _jsonService.ResolveJsonPath(_videoService.CurrentVideoFile);
                    bool fileExists = !string.IsNullOrEmpty(existingJson) && File.Exists(existingJson);

                    if (fileExists)
                    {
                        var decision = MessageBox.Show(
                            "Waypoint가 없어 해당 JSON 파일은 삭제됩니다. 삭제하시겠습니까?",
                            "JSON 파일 삭제",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (decision == DialogResult.Yes)
                        {
                            try
                            {
                                _jsonService.DeleteJsonForVideo(_videoService.CurrentVideoFile);
                                _isDirty = false;
                                _autoSavedJsonPath = null;   // DF-1-05: 롤백 대상 해소
                                currentJsonFile = "";
                                if (labelCurrentJsonFile != null) labelCurrentJsonFile.Text = "";
                                MessageBox.Show("JSON 파일이 삭제되었습니다.", "알림",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception delEx)
                            {
                                MessageBox.Show($"JSON 파일 삭제 실패:\n{delEx.Message}", "오류",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        return;
                    }
                    else
                    {
                        MessageBox.Show("저장할 라벨링 데이터가 없어 JSON 파일은 생성되지 않습니다.",
                            "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                // FUNC-12 (DF-2-06): BBOX 1개만 존재하는 객체가 Waypoint 없이 저장되는 경우
                // — JsonService 가 Entry==Exit 인 Waypoint 를 생성하여 사용자 안내 메시지
                //   ("Exit 는 Entry 이후 프레임에서 설정") 와 모순되는 흐름을 막는다.
                //   해당 객체에 명시적 Waypoint 가 있으면 통과 (사용자 의도 존중).
                var unfinishedTracks = boundingBoxes
                    .Where(b => !b.IsDeleted)
                    .GroupBy(b => new { b.Label, ObjectId = GetBoxId(b) })
                    .Where(g =>
                    {
                        var sameObjectBoxes = g.ToList();
                        if (sameObjectBoxes.Count != 1) return false;
                        var only = sameObjectBoxes[0];
                        bool hasMatchingWaypoint = waypointMarkers.Any(w =>
                            w.Label == only.Label &&
                            w.ObjectId == GetBoxId(only) &&
                            only.FrameIndex >= w.EntryFrame &&
                            only.FrameIndex <= w.ExitFrame);
                        return !hasMatchingWaypoint;
                    })
                    .Select(g => new { g.Key.Label, g.Key.ObjectId })
                    .ToList();

                if (unfinishedTracks.Count > 0)
                {
                    string offending = string.Join(", ",
                        unfinishedTracks.Select(t => $"[{t.Label}] [{t.ObjectId:D2}]"));
                    MessageBox.Show(
                        "Exit 는 Entry 이후 프레임에서 설정해주세요. " +
                        "하나의 BBOX 만 존재하는 객체가 있어 Waypoint 를 생성할 수 없습니다.\n\n" +
                        $"해당 객체: {offending}\n\n" +
                        "해결 방법: 해당 객체의 다른 프레임에서 BBOX 를 추가하거나, " +
                        "Entry/Exit 를 명시적으로 설정한 후 다시 저장해주세요.",
                        "JSON 저장 차단",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Form loadingForm = new Form
                {
                    Width = 300, Height = 120, Text = "JSON 저장",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false, TopMost = true
                };
                Label loadingLabel = new Label
                {
                    Text = "저장 중...", AutoSize = true,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    Location = new System.Drawing.Point(100, 30)
                };
                loadingForm.Controls.Add(loadingLabel);
                loadingForm.Show();
                loadingForm.Refresh();

                try
                {
                    InvalidateBoxCache();
                    UpdateBoxCount();

                    await Task.Run(() => SaveCurrentLabelingData());

                    loadingForm.Close();

                    if (!string.IsNullOrEmpty(currentJsonFile) && File.Exists(currentJsonFile))
                    {
                        await LoadLabelingData(_videoService.CurrentVideoFile);
                    }

                    string savedFileName = !string.IsNullOrEmpty(currentJsonFile) ? Path.GetFileName(currentJsonFile) : "labels.json";
                    string labelsDir = Path.GetDirectoryName(currentJsonFile) ?? Path.Combine(Path.GetDirectoryName(_videoService.CurrentVideoFile), "labels");

                    LogService.AuditJsonSave(currentJsonFile ?? savedFileName);

                    MessageBox.Show(
                        $"JSON 파일이 저장되었습니다.\n\n파일: {savedFileName}\n위치: {labelsDir}\n박스 개수: {boundingBoxes.Count}개",
                        "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException invEx)
                {
                    loadingForm.Close();
                    MessageBox.Show($"JSON 저장 실패:\n\n{invEx.Message}",
                        "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (IOException ioEx)
                {
                    loadingForm.Close();
                    MessageBox.Show($"파일 저장 중 오류가 발생했습니다.\n\n" +
                        $"원인: {ioEx.Message}\n\n" +
                        $"해결 방법: 저장 경로의 디스크 공간과 쓰기 권한을 확인하세요.",
                        "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    loadingForm.Close();
                    MessageBox.Show($"JSON 저장 중 예기치 않은 오류가 발생했습니다:\n{ex.Message}",
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception outerEx)
            {
                Log.Error(outerEx, "[JSON 저장 버튼 오류] {Message}", outerEx.Message);
                MessageBox.Show($"JSON 저장 중 외부 오류 발생:\n{outerEx.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentLabelingData()
        {
            string videoDir = Path.GetDirectoryName(_videoService.CurrentVideoFile);
            string saveDir = Path.Combine(videoDir, "labels");
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            string baseFileName = Path.GetFileNameWithoutExtension(_videoService.CurrentVideoFile);
            string savePath = Path.Combine(saveDir, baseFileName + "_labels.json");

            // 경로 트래버설 방지 검증
            if (!PathValidator.IsPathSafe(savePath, videoDir))
            {
                Log.Warning("[보안] 저장 경로 트래버설 감지: {Path}", savePath);
                MessageBox.Show("파일 경로가 올바르지 않습니다.", "보안 경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _jsonService.ExportToJsonExtended(
                savePath,
                _videoService.CurrentVideoFile,
                _videoService.Fps,
                _videoService.FrameWidth,
                _videoService.FrameHeight,
                boundingBoxes,
                waypointMarkers,
                _videoService);

            currentJsonFile = savePath;
            _isDirty = false;
            // DF-1-05 (D-09): 저장 성공 경로 기록 — 영상 전환/앱 종료 시 '아니요' 선택하면 롤백 삭제 대상
            _autoSavedJsonPath = savePath;
        }

        private void btnDeleteJson_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_videoService.CurrentVideoFile))
            {
                MessageBox.Show("먼저 비디오 파일을 로드해주세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("JSON 파일을 삭제하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            bool deleted = _jsonService.DeleteJsonFileForVideo(_videoService.CurrentVideoFile, currentJsonFile);
            if (deleted)
            {
                boundingBoxes.Clear();
                waypointMarkers.Clear();
                selectedBox = null;
                selectedWaypoint = null;
                currentJsonFile = "";
                InvalidateBoxCache();
                UpdateBoxCount();
                UpdateWaypointListView();
                UpdateBboxListDisplay();
                pictureBoxVideo.Invalidate();
                labelCurrentJsonFile.Text = "";

                MessageBox.Show("JSON 파일이 삭제되었습니다.", "삭제 완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("삭제할 JSON 파일이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        #region Video Playback

        private void btnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_videoService.IsVideoLoaded)
                {
                    MessageBox.Show("비디오 파일이 로드되지 않았습니다.\n먼저 파일을 선택해주세요.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                isPlaying = !isPlaying;

                if (isPlaying)
                {
                    // Close attribute windows on play
                    btnPlay.Text = "⏸";
                    lastFrameTime = DateTime.Now.Ticks / 10000;
                    msPerFrame = 1000.0 / _videoService.Fps;

                    if (msPerFrame <= 0)
                    {
                        isPlaying = false;
                        btnPlay.Text = "▶";
                        return;
                    }

                    timerPlayback.Interval = 33;
                    timerPlayback.Start();
                }
                else
                {
                    btnPlay.Text = "▶";
                    timerPlayback.Stop();
                }
            }
            catch (InvalidOperationException invEx)
            {
                Log.Error(invEx, "[재생 오류 - 상태] {Message}", invEx.Message);
                isPlaying = false; btnPlay.Text = "▶"; timerPlayback?.Stop();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[재생 버튼 오류] {Message}", ex.Message);
                isPlaying = false;
                btnPlay.Text = "▶";
                timerPlayback?.Stop();
            }
        }

        private void timerPlayback_Tick(object sender, EventArgs e)
        {
            // RELI-06: 영상 로드 중에는 타이머 틱 무시 (타이머 Stop과 중복 방어)
            if (_isVideoLoading) return;
            // RELI-06 (05.5-02): 첫 프레임 페인트 전 cold-decoder 기간 틱 무시
            if (!_isVideoReady) return;

            long currentTime = DateTime.Now.Ticks / 10000;
            long elapsedMs = currentTime - lastFrameTime;

            int framesToMove = (int)(elapsedMs * playbackSpeed / msPerFrame);

            if (framesToMove > 0)
            {
                int nextFrame = Math.Min(_videoService.TotalFrames - 1, _videoService.CurrentFrameIndex + framesToMove);
                LoadFrame(nextFrame);
                lastFrameTime = currentTime;

                if (nextFrame >= _videoService.TotalFrames - 1)
                {
                    isPlaying = false;
                    btnPlay.Text = "▶";
                    timerPlayback.Stop();
                    playbackSpeed = 1.0;
                }
            }
        }

        private void btnRewind_Click(object sender, EventArgs e)
        {
            // RELI-06 (05.5-02): cold-decoder 기간 Rewind 무시
            if (!_isVideoReady) return;
            int framesToMove = (int)(_videoService.Fps * 5);
            int newFrame = Math.Max(0, _videoService.CurrentFrameIndex - framesToMove);
            LoadFrame(newFrame);
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            // RELI-06 (05.5-02): cold-decoder 기간 Forward 무시
            if (!_isVideoReady) return;
            int framesToMove = (int)(_videoService.Fps * 5);
            int newFrame = Math.Min(_videoService.TotalFrames - 1, _videoService.CurrentFrameIndex + framesToMove);
            LoadFrame(newFrame);
        }

        #endregion

        #region Entry/Exit Markers

        private void btnEntry_Click(object sender, EventArgs e)
        {
            // USAB-09: Waypoint 선택 상태이면 해당 EntryFrame으로 이동, 아니면 기존 지정 동작
            if (selectedWaypoint != null)
            {
                // DF-1-13: 영상 미로드 상태에서 LoadFrame 진입 방지
                if (!_videoService.IsVideoLoaded)
                {
                    MessageBox.Show("영상을 먼저 로드해 주십시오.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                LoadFrame(selectedWaypoint.EntryFrame);
                return;
            }

            // DF-1-13: 영상 미로드 상태에서 Entry 클릭 시 TimeSpan.FromSeconds(NaN) 크래시 차단
            if (!_videoService.IsVideoLoaded)
            {
                MessageBox.Show("영상을 먼저 로드해 주십시오.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SetEntryMarker();
        }

        private void SetEntryMarker()
        {
            // DF-1-13: 호출자 누락 대비 방어선 (단축키 경로 안전망)
            if (!_videoService.IsVideoLoaded) return;

            entryFrameIndex = _videoService.CurrentFrameIndex;
            TimeSpan entryTime = TimeSpan.FromSeconds(_videoService.CurrentFrameIndex / _videoService.Fps);
            btnEntry.Text = $"Entry: {entryTime:hh\\:mm\\:ss}";
            panelTimeline.Invalidate();
        }

        private async void btnExit_Click(object sender, EventArgs e)
        {
            // USAB-09: Waypoint 선택 상태이면 해당 ExitFrame으로 이동, 아니면 기존 지정 동작
            if (selectedWaypoint != null)
            {
                // DF-1-13: 영상 미로드 상태에서 LoadFrame 진입 방지
                if (!_videoService.IsVideoLoaded)
                {
                    MessageBox.Show("영상을 먼저 로드해 주십시오.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                LoadFrame(selectedWaypoint.ExitFrame);
                return;
            }

            // DF-1-13: 영상 미로드 상태에서 Exit 클릭 시 fps==0 연산 차단
            if (!_videoService.IsVideoLoaded)
            {
                MessageBox.Show("영상을 먼저 로드해 주십시오.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                await SetExitMarkerAndCreateWaypoint();
            }
            catch (InvalidOperationException invEx)
            {
                Log.Error(invEx, "[Exit 설정 오류] {Message}", invEx.Message);
                MessageBox.Show($"Exit 마커 설정 실패:\n\n{invEx.Message}\n\n" +
                    $"해결 방법: Entry 마커를 먼저 설정한 후 다시 시도하세요.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Exit 버튼 오류] {Message}", ex.Message);
                MessageBox.Show($"Exit 설정 중 오류 발생:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SetExitMarkerAndCreateWaypoint()
        {
            // DF-1-13: 호출자 누락 대비 방어선 (단축키 경로 안전망)
            if (!_videoService.IsVideoLoaded) return;

            int currentFrameIndex = _videoService.CurrentFrameIndex;
            double fps = _videoService.Fps;

            // If no entry set but selected box has existing waypoint, update exit
            if (!entryFrameIndex.HasValue && selectedBox != null)
            {
                var existingWaypoint = FindWaypointForBox(selectedBox);
                if (existingWaypoint != null)
                {
                    if (currentFrameIndex <= existingWaypoint.EntryFrame)
                    {
                        MessageBox.Show("Exit 프레임은 Entry 프레임보다 뒤에 있어야 합니다.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (currentFrameIndex > existingWaypoint.ExitFrame)
                    {
                        MessageBox.Show("ExitFrame을 기존 ExitFrame보다 뒤로 연장할 수 없습니다.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    bool exitFrameShortened = currentFrameIndex < existingWaypoint.ExitFrame;
                    if (exitFrameShortened)
                    {
                        int boxId = GetBoxId(selectedBox);
                        var boxesToDelete = boundingBoxes
                            .Where(b => b.Label == selectedBox.Label && GetBoxId(b) == boxId &&
                                       b.FrameIndex > currentFrameIndex && b.FrameIndex <= existingWaypoint.ExitFrame && !b.IsDeleted)
                            .ToList();

                        foreach (var box in boxesToDelete)
                        {
                            AddUndoAction(new UndoAction { Type = UndoActionType.RemoveBox, Box = CloneBoundingBox(box) });
                            boundingBoxes.Remove(box);
                        }
                        InvalidateBoxCache();
                        UpdateBoxCount();
                    }

                    existingWaypoint.ExitFrame = currentFrameIndex;
                    TimeSpan exitTimeSpan = TimeSpan.FromSeconds(currentFrameIndex / fps);
                    existingWaypoint.ExitTime = exitTimeSpan.ToString(@"hh\:mm\:ss");

                    if (selectedBox.Label == "event")
                    {
                        var entryEventBox = boundingBoxes
                            .Where(b => b.Label == "event" && b.EventId == selectedBox.EventId && b.FrameIndex >= existingWaypoint.EntryFrame && b.FrameIndex <= existingWaypoint.ExitFrame)
                            .OrderBy(b => b.FrameIndex).FirstOrDefault();
                        if (entryEventBox != null)
                            PropagateEventBoxWithinRange(entryEventBox, existingWaypoint.ExitFrame);
                    }

                    SaveCurrentLabelingData();
                    UpdateWaypointListView();
                    btnExit.Text = "Exit";
                    panelTimeline.Invalidate();
                    return;
                }
            }

            if (!entryFrameIndex.HasValue)
            {
                MessageBox.Show("먼저 Entry 프레임을 지정해야 합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (currentFrameIndex <= entryFrameIndex.Value)
            {
                MessageBox.Show("Exit 프레임은 Entry 프레임보다 뒤에 있어야 합니다.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var entryPersonBoxes = GetBboxesForFrame(entryFrameIndex.Value).Where(b => b.Label == "person").ToList();
            var entryVehicleBoxes = GetBboxesForFrame(entryFrameIndex.Value).Where(b => b.Label == "vehicle").ToList();
            var eventBoxesInRange = boundingBoxes
                .Where(b => b.Label == "event" && b.FrameIndex >= entryFrameIndex.Value && b.FrameIndex <= currentFrameIndex).ToList();

            if (entryPersonBoxes.Count == 0 && entryVehicleBoxes.Count == 0 && eventBoxesInRange.Count == 0)
            {
                MessageBox.Show("Entry 프레임에 Person, Vehicle 또는 Event 박스가 없습니다.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // FUNC-06: Entry-Exit 프레임 간 객체 ID 불일치 감지 및 경고
            var exitPersonBoxes = GetBboxesForFrame(currentFrameIndex).Where(b => b.Label == "person" && !b.IsDeleted).ToList();
            var exitVehicleBoxes = GetBboxesForFrame(currentFrameIndex).Where(b => b.Label == "vehicle" && !b.IsDeleted).ToList();
            // NEW-02 (D-03): Event 클래스도 Person/Vehicle 과 동일하게 Entry-Exit ID 불일치 검증
            var exitEventBoxes = GetBboxesForFrame(currentFrameIndex).Where(b => b.Label == "event" && !b.IsDeleted).ToList();
            var entryEventBoxes = GetBboxesForFrame(entryFrameIndex.Value).Where(b => b.Label == "event" && !b.IsDeleted).ToList();
            var mismatchMessages = new List<string>();
            foreach (var entryBox in entryPersonBoxes)
            {
                if (exitPersonBoxes.Count > 0 && !exitPersonBoxes.Any(b => b.PersonId == entryBox.PersonId))
                    mismatchMessages.Add($"Person ID {entryBox.PersonId:D2}: Entry 프레임({entryFrameIndex.Value})과 Exit 프레임({currentFrameIndex}) 간 ID가 일치하지 않습니다.");
            }
            foreach (var entryBox in entryVehicleBoxes)
            {
                if (exitVehicleBoxes.Count > 0 && !exitVehicleBoxes.Any(b => b.VehicleId == entryBox.VehicleId))
                    mismatchMessages.Add($"Vehicle ID {entryBox.VehicleId:D2}: Entry 프레임({entryFrameIndex.Value})과 Exit 프레임({currentFrameIndex}) 간 ID가 일치하지 않습니다.");
            }
            foreach (var entryBox in entryEventBoxes)
            {
                if (exitEventBoxes.Count > 0 && !exitEventBoxes.Any(b => b.EventId == entryBox.EventId))
                    mismatchMessages.Add($"Event ID {entryBox.EventId:D2}: Entry 프레임({entryFrameIndex.Value})과 Exit 프레임({currentFrameIndex}) 간 ID가 일치하지 않습니다.");
            }
            if (mismatchMessages.Count > 0)
            {
                string mismatchMsg = "Entry-Exit 구간 내 객체 ID가 일치하지 않습니다:\n\n" +
                    string.Join("\n", mismatchMessages) + "\n\n" +
                    "해결 방법: Ctrl+N 또는 단축키로 Exit 프레임의 객체 ID를 Entry 프레임과 일치시킨 후 다시 시도하세요.";
                MessageBox.Show(mismatchMsg, "ID 불일치 경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            exitFrameIndex = currentFrameIndex;
            TimeSpan exitTime = TimeSpan.FromSeconds(currentFrameIndex / fps);
            TimeSpan entryTime = TimeSpan.FromSeconds(entryFrameIndex.Value / fps);

            int currentEntryFrame = entryFrameIndex.Value;
            int currentExitFrame = exitFrameIndex.Value;

            // Create Person waypoints
            foreach (var personBox in entryPersonBoxes)
            {
                int personId = personBox.PersonId;
                var overlapping = waypointMarkers.FirstOrDefault(w =>
                    w.Label == "person" && w.ObjectId == personId &&
                    ((currentEntryFrame >= w.EntryFrame && currentEntryFrame <= w.ExitFrame) ||
                     (currentExitFrame >= w.EntryFrame && currentExitFrame <= w.ExitFrame) ||
                     (currentEntryFrame <= w.EntryFrame && currentExitFrame >= w.ExitFrame)));

                if (overlapping != null) continue;

                waypointMarkers.Add(new WaypointMarker
                {
                    EntryFrame = entryFrameIndex.Value, ExitFrame = exitFrameIndex.Value,
                    MarkerColor = Color.FromArgb(255, 107, 107),
                    EntryTime = entryTime.ToString(@"hh\:mm\:ss"), ExitTime = exitTime.ToString(@"hh\:mm\:ss"),
                    ObjectId = personId, Label = "person"
                });
                // DF-1-17 (D-17a): Waypoint 생성 감사 이벤트
                LogService.AuditWaypointCreate("person", personId, entryFrameIndex.Value, exitFrameIndex.Value);
            }

            // Create Vehicle waypoints
            foreach (var vehicleBox in entryVehicleBoxes)
            {
                int vehicleId = vehicleBox.VehicleId;
                var overlapping = waypointMarkers.FirstOrDefault(w =>
                    w.Label == "vehicle" && w.ObjectId == vehicleId &&
                    ((currentEntryFrame >= w.EntryFrame && currentEntryFrame <= w.ExitFrame) ||
                     (currentExitFrame >= w.EntryFrame && currentExitFrame <= w.ExitFrame) ||
                     (currentEntryFrame <= w.EntryFrame && currentExitFrame >= w.ExitFrame)));

                if (overlapping != null) continue;

                waypointMarkers.Add(new WaypointMarker
                {
                    EntryFrame = entryFrameIndex.Value, ExitFrame = exitFrameIndex.Value,
                    MarkerColor = Color.FromArgb(107, 158, 255),
                    EntryTime = entryTime.ToString(@"hh\:mm\:ss"), ExitTime = exitTime.ToString(@"hh\:mm\:ss"),
                    ObjectId = vehicleId, Label = "vehicle"
                });
                // DF-1-17 (D-17a): Waypoint 생성 감사 이벤트
                LogService.AuditWaypointCreate("vehicle", vehicleId, entryFrameIndex.Value, exitFrameIndex.Value);
            }

            // Create Event waypoints
            var eventGroups = eventBoxesInRange.GroupBy(b => b.EventId).ToList();
            foreach (var eventGroup in eventGroups)
            {
                int eventId = eventGroup.Key;
                int minFrameIndex = eventGroup.Min(b => b.FrameIndex);
                var overlapping = waypointMarkers.FirstOrDefault(w =>
                    w.Label == "event" && w.ObjectId == eventId &&
                    ((currentEntryFrame >= w.EntryFrame && currentEntryFrame <= w.ExitFrame) ||
                     (currentExitFrame >= w.EntryFrame && currentExitFrame <= w.ExitFrame) ||
                     (currentEntryFrame <= w.EntryFrame && currentExitFrame >= w.ExitFrame)));

                if (overlapping != null) continue;

                var entryEventBox = eventGroup.FirstOrDefault(b => b.FrameIndex == minFrameIndex);
                if (entryEventBox != null)
                {
                    waypointMarkers.Add(new WaypointMarker
                    {
                        EntryFrame = minFrameIndex, ExitFrame = exitFrameIndex.Value,
                        MarkerColor = Color.FromArgb(107, 255, 107),
                        EntryTime = TimeSpan.FromSeconds(minFrameIndex / fps).ToString(@"hh\:mm\:ss"),
                        ExitTime = exitTime.ToString(@"hh\:mm\:ss"),
                        ObjectId = eventId, Label = "event", InteractingObject = ""
                    });
                    // DF-1-17 (D-17a): Waypoint 생성 감사 이벤트
                    LogService.AuditWaypointCreate("event", eventId, minFrameIndex, exitFrameIndex.Value);
                    PropagateEventBoxWithinRange(entryEventBox, exitFrameIndex.Value);
                }
            }

            InvalidateBoxCache();
            UpdateBoxCount();
            UpdateBboxListDisplay();
            pictureBoxVideo.Invalidate();

            SaveCurrentLabelingData();
            UpdateWaypointListView();

            entryFrameIndex = null;
            exitFrameIndex = null;
            btnEntry.Text = "Entry";
            btnExit.Text = "Exit";
            panelTimeline.Invalidate();
        }

        private void btnToggleSubtitle_Click(object sender, EventArgs e)
        {
            _videoService.IsSubtitleVisible = !_videoService.IsSubtitleVisible;
            btnToggleSubtitle.Text = _videoService.IsSubtitleVisible ? "자막 닫기" : "자막 열기";
            btnToggleSubtitle.BackColor = _videoService.IsSubtitleVisible
                ? Color.FromArgb(239, 68, 68) : Color.FromArgb(100, 116, 139);
            if (labelSubtitleTimestamp != null)
                labelSubtitleTimestamp.Visible = _videoService.IsSubtitleVisible;
            UpdateTimeLabels();
        }

        #endregion

        #region Waypoint ListView

        private void UpdateWaypointListView()
        {
            // Person
            listViewPersonWaypoints.Items.Clear();
            foreach (var wp in waypointMarkers.Where(w => w.Label == "person").OrderBy(w => w.EntryFrame))
            {
                string objectName = $"person_{wp.ObjectId:D2}";
                var item = new ListViewItem(new[] { wp.EntryTime ?? "", wp.ExitTime ?? "", objectName });
                item.Tag = wp;
                listViewPersonWaypoints.Items.Add(item);
            }

            // Vehicle
            listViewVehicleWaypoints.Items.Clear();
            foreach (var wp in waypointMarkers.Where(w => w.Label == "vehicle").OrderBy(w => w.EntryFrame))
            {
                string[] vehicleNames = { "car", "motorcycle", "e_scooter", "bicycle" };
                string objectName = wp.ObjectId >= 1 && wp.ObjectId <= vehicleNames.Length ? vehicleNames[wp.ObjectId - 1] : $"vehicle_{wp.ObjectId}";
                var item = new ListViewItem(new[] { wp.EntryTime ?? "", wp.ExitTime ?? "", objectName });
                item.Tag = wp;
                listViewVehicleWaypoints.Items.Add(item);
            }

            // Event
            listViewEventWaypoints.Items.Clear();
            foreach (var wp in waypointMarkers.Where(w => w.Label == "event").OrderBy(w => w.EntryFrame))
            {
                string[] eventNames = { "event_hazard", "event_accident", "event_damage", "event_fire", "event_intrusion", "event_leak", "event_failure", "event_lost_object", "event_fall", "event_abnormal_behavior" };
                string eventName = wp.ObjectId >= 1 && wp.ObjectId <= eventNames.Length ? eventNames[wp.ObjectId - 1] : $"event_{wp.ObjectId}";
                string timeRange = $"{wp.EntryTime}~{wp.ExitTime}";
                var item = new ListViewItem(new[] { eventName, timeRange, wp.InteractingObject ?? "" });
                item.Tag = wp;
                listViewEventWaypoints.Items.Add(item);
            }
        }

        private void listViewPersonWaypoints_Click(object sender, EventArgs e)
        {
            // RELI-06 (05.5-02): cold-decoder 기간 Waypoint 리스트 클릭 무시
            if (!_isVideoReady) return;
            if (suppressWaypointClickOnce) { suppressWaypointClickOnce = false; return; }
            if (listViewPersonWaypoints.SelectedItems.Count > 0)
            {
                var waypoint = listViewPersonWaypoints.SelectedItems[0].Tag as WaypointMarker;
                if (waypoint != null)
                {
                    selectedWaypoint = waypoint;
                    panelTimeline.Invalidate();
                    LoadFrame(waypoint.EntryFrame);
                }
            }
            else { selectedWaypoint = null; panelTimeline.Invalidate(); }
        }

        private void listViewVehicleWaypoints_Click(object sender, EventArgs e)
        {
            // RELI-06 (05.5-02): cold-decoder 기간 Waypoint 리스트 클릭 무시
            if (!_isVideoReady) return;
            if (suppressWaypointClickOnce) { suppressWaypointClickOnce = false; return; }
            if (listViewVehicleWaypoints.SelectedItems.Count > 0)
            {
                var waypoint = listViewVehicleWaypoints.SelectedItems[0].Tag as WaypointMarker;
                if (waypoint != null)
                {
                    selectedWaypoint = waypoint;
                    panelTimeline.Invalidate();
                    LoadFrame(waypoint.EntryFrame);
                }
            }
            else { selectedWaypoint = null; panelTimeline.Invalidate(); }
        }

        private void listViewEventWaypoints_Click(object sender, EventArgs e)
        {
            // RELI-06 (05.5-02): cold-decoder 기간 Waypoint 리스트 클릭 무시
            if (!_isVideoReady) return;
            if (suppressWaypointClickOnce) { suppressWaypointClickOnce = false; return; }
            if (listViewEventWaypoints.SelectedItems.Count > 0)
            {
                var waypoint = listViewEventWaypoints.SelectedItems[0].Tag as WaypointMarker;
                if (waypoint != null)
                {
                    selectedWaypoint = waypoint;
                    panelTimeline.Invalidate();
                    LoadFrame(waypoint.EntryFrame);
                }
            }
            else { selectedWaypoint = null; panelTimeline.Invalidate(); }
        }

        private void listViewWaypoints_MouseDown(object sender, MouseEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null) return;
            var hit = listView.HitTest(e.Location);
            if (hit.Item == null) return;

            if (hit.Item.Tag is WaypointMarker waypoint)
            {
                selectedWaypoint = waypoint;
                panelTimeline.Invalidate();

                int targetFrame = waypoint.EntryFrame;
                bool shouldSelectBox = false;

                if (hit.SubItem != null)
                {
                    int subIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
                    if (listView == listViewPersonWaypoints || listView == listViewVehicleWaypoints)
                    {
                        if (subIndex == 1) targetFrame = waypoint.ExitFrame;
                        else if (subIndex == 2) shouldSelectBox = true;
                    }
                    else if (listView == listViewEventWaypoints)
                    {
                        if (subIndex == 2) shouldSelectBox = true;
                    }
                }

                if (!shouldSelectBox) LoadFrame(targetFrame);
                if (shouldSelectBox) SelectBoxForWaypoint(waypoint);
                suppressWaypointClickOnce = true;
            }
        }

        private void listViewEventWaypoints_MouseUp(object sender, MouseEventArgs e)
        {
            var hit = listViewEventWaypoints.HitTest(e.Location);
            if (hit.Item == null || hit.SubItem == null) return;
            int subIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (subIndex != 2) return;
            if (eventListEditBox != null && eventListEditBox.Tag != null) return;

            var waypoint = hit.Item.Tag as WaypointMarker;
            if (waypoint == null) return;

            if (eventListEditBox == null || eventListEditBox.IsDisposed)
            {
                eventListEditBox = new TextBox();
                eventListEditBox.Leave += (s, ev) => CommitEventListEdit();
                eventListEditBox.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter) { CommitEventListEdit(); ev.Handled = true; }
                    else if (ev.KeyCode == Keys.Escape) { CancelEventListEdit(); ev.Handled = true; }
                };
            }

            eventListEditBox.Tag = hit.Item;
            eventListEditBox.Bounds = hit.SubItem.Bounds;
            eventListEditBox.Text = waypoint.InteractingObject ?? string.Empty;
            listViewEventWaypoints.Controls.Add(eventListEditBox);
            eventListEditBox.Focus();
            eventListEditBox.SelectAll();
        }

        private void CommitEventListEdit()
        {
            if (eventListEditBox == null || eventListEditBox.Tag == null) return;
            var item = eventListEditBox.Tag as ListViewItem;
            if (item == null) { CancelEventListEdit(); return; }
            var waypoint = item.Tag as WaypointMarker;
            if (waypoint == null) { CancelEventListEdit(); return; }

            waypoint.InteractingObject = eventListEditBox.Text ?? string.Empty;
            if (item.SubItems.Count >= 3) item.SubItems[2].Text = waypoint.InteractingObject;
            listViewEventWaypoints.Controls.Remove(eventListEditBox);
            eventListEditBox.Tag = null;
            SaveCurrentLabelingData();
        }

        private void CancelEventListEdit()
        {
            if (eventListEditBox == null) return;
            listViewEventWaypoints.Controls.Remove(eventListEditBox);
            eventListEditBox.Tag = null;
        }

        private void SelectBoxForWaypoint(WaypointMarker waypoint)
        {
            int currentFrameIndex = _videoService.CurrentFrameIndex;
            BoundingBox targetBox = null;

            if (waypoint.Label == "person")
                targetBox = boundingBoxes.FirstOrDefault(b => b.FrameIndex == currentFrameIndex && b.Label == "person" && b.PersonId == waypoint.ObjectId && !b.IsDeleted);
            else if (waypoint.Label == "vehicle")
                targetBox = boundingBoxes.FirstOrDefault(b => b.FrameIndex == currentFrameIndex && b.Label == "vehicle" && b.VehicleId == waypoint.ObjectId && !b.IsDeleted);
            else if (waypoint.Label == "event")
                targetBox = boundingBoxes.FirstOrDefault(b => b.FrameIndex == currentFrameIndex && b.Label == "event" && b.EventId == waypoint.ObjectId && !b.IsDeleted);

            if (targetBox != null)
            {
                selectedBox = targetBox;
                UpdateObjectInfo(selectedBox);
                UpdateBboxListDisplay();
                HighlightSelectedBoxInSidebar();
                pictureBoxVideo.Invalidate();
            }
        }

        private void btnDeleteSelectedWaypoint_Click(object sender, EventArgs e)
        {
            // DF-1-18 (D-16): 다중 선택 일괄 삭제 — P/V/E 각 ListView 의 SelectedItems 전체 순회
            var toDelete = new List<(WaypointMarker wp, string type)>();

            foreach (ListViewItem item in listViewPersonWaypoints.SelectedItems)
                if (item.Tag is WaypointMarker wp) toDelete.Add((wp, "Person"));
            foreach (ListViewItem item in listViewVehicleWaypoints.SelectedItems)
                if (item.Tag is WaypointMarker wp) toDelete.Add((wp, "Vehicle"));
            foreach (ListViewItem item in listViewEventWaypoints.SelectedItems)
                if (item.Tag is WaypointMarker wp) toDelete.Add((wp, "Event"));

            if (toDelete.Count == 0)
            {
                MessageBox.Show("삭제할 Waypoint를 선택해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string prompt = toDelete.Count == 1
                ? $"선택한 {toDelete[0].type} Waypoint를 삭제하시겠습니까?"
                : $"선택된 {toDelete.Count}개의 Waypoint를 삭제하시겠습니까?";
            string title = toDelete.Count == 1
                ? $"{toDelete[0].type} Waypoint 삭제"
                : "Waypoint 삭제";

            var result = MessageBox.Show(prompt, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            foreach (var (waypoint, type) in toDelete)
            {
                var boxesToDelete = boundingBoxes.Where(b =>
                    b.Label == waypoint.Label &&
                    GetBoxId(b) == waypoint.ObjectId &&
                    b.FrameIndex >= waypoint.EntryFrame &&
                    b.FrameIndex <= waypoint.ExitFrame).ToList();

                // DF-1-19: Waypoint 삭제 atomic Undo — 박스 + Waypoint 단일 composite 액션으로 묶어 Ctrl+Z 한 번에 전체 복원.
                AddUndoAction(new UndoAction
                {
                    Type = UndoActionType.RemoveWaypointWithBoxes,
                    AffectedBoxes = boxesToDelete.Select(CloneBoundingBox).ToList(),
                    AffectedWaypoint = waypoint,
                });

                foreach (var box in boxesToDelete) boundingBoxes.Remove(box);

                if (selectedBox != null && boxesToDelete.Contains(selectedBox)) selectedBox = null;
                if (selectedWaypoint == waypoint) selectedWaypoint = null;
                waypointMarkers.Remove(waypoint);
                // DF-1-17 (D-17a): Waypoint 삭제 감사 이벤트 (사용자 주도 — 일괄 삭제 경로)
                LogService.AuditWaypointDelete(waypoint.Label, waypoint.ObjectId, waypoint.EntryFrame, waypoint.ExitFrame);
            }

            UpdateWaypointListView();
            InvalidateBoxCache();
            UpdateBoxCount();
            UpdateBboxListDisplay();
            panelTimeline.Invalidate();
            pictureBoxVideo.Invalidate();
        }

        #endregion

        #region Drawing/Painting

        // FUNC-11 (DF-2-05): 모드 전환 시 currentMode + 좌측 버튼 BackColor + Cursor 를
        // 한 곳에서 원자적으로 갱신하여 시각 동기화 결함 방지.
        // (ARGB: 활성 = (59, 130, 246) — 파란색, 비활성 = (62, 62, 66) — 다크 톤)
        private static readonly Color ModeButtonActiveColor   = Color.FromArgb(59, 130, 246);
        private static readonly Color ModeButtonInactiveColor = Color.FromArgb(62, 62, 66);

        private void SetMode(DrawMode mode)
        {
            currentMode = mode;

            if (mode == DrawMode.Select)
            {
                btnSelectAll.BackColor = ModeButtonActiveColor;
                btnEdit.BackColor      = ModeButtonInactiveColor;
                pictureBoxVideo.Cursor = Cursors.Hand;
            }
            else // DrawMode.Draw
            {
                btnEdit.BackColor      = ModeButtonActiveColor;
                btnSelectAll.BackColor = ModeButtonInactiveColor;
                pictureBoxVideo.Cursor = Cursors.Cross;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            SetMode(DrawMode.Select);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            SetMode(DrawMode.Draw);
        }

        private void pictureBoxVideo_Paint(object sender, PaintEventArgs e)
        {
            if (pictureBoxVideo.Image == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int currentFrameIndex = _videoService.CurrentFrameIndex;

            if (lastCachedFrameForPaint != currentFrameIndex)
            {
                cachedCurrentFrameBoxes = GetBboxesForFrame(currentFrameIndex);
                lastCachedFrameForPaint = currentFrameIndex;
            }

            // Draw all boxes except selected
            foreach (var box in cachedCurrentFrameBoxes)
            {
                if (box == selectedBox) continue;
                DrawBoundingBox(g, box, false);
            }

            // Draw selected box on top
            if (selectedBox != null && selectedBox.FrameIndex == currentFrameIndex)
            {
                DrawBoundingBox(g, selectedBox, true);
            }

            // Draw in-progress drawing box
            if (isDrawing && drawingBox != null)
            {
                var viewRect = CoordinateHelper.ImageToView(
                    new RectangleF(drawingBox.Rectangle.X, drawingBox.Rectangle.Y, drawingBox.Rectangle.Width, drawingBox.Rectangle.Height), pictureBoxVideo);
                Color boxColor = GetColorForLabel(drawingBox.Label);
                using (Pen pen = new Pen(boxColor, 3) { DashStyle = DashStyle.Dash })
                    g.DrawRectangle(pen, viewRect.X, viewRect.Y, viewRect.Width, viewRect.Height);
            }

            // RELI-06 (05.5-02 gap closure): first-paint handshake —
            // 첫 프레임이 실제로 화면에 페인트된 직후 한 번만 ready 전이 + 지연된 자동 재생 소비.
            if (!_isVideoReady && _videoService.IsVideoLoaded && pictureBoxVideo.Image != null)
            {
                _isVideoReady = true;
                if (_pendingAutoPlay)
                {
                    _pendingAutoPlay = false;
                    if (!isPlaying)
                    {
                        btnPlay_Click(null, EventArgs.Empty);
                    }
                }
            }
        }

        private void DrawBoundingBox(Graphics g, BoundingBox box, bool isSelected)
        {
            var viewRect = CoordinateHelper.ImageToView(
                new RectangleF(box.Rectangle.X, box.Rectangle.Y, box.Rectangle.Width, box.Rectangle.Height), pictureBoxVideo);

            Color boxColor = GetColorForLabel(box.Label);

            if (box.IsDeleted)
            {
                Color fadedColor = Color.FromArgb(150, boxColor.R, boxColor.G, boxColor.B);
                using (Pen pen = new Pen(fadedColor, 2))
                    g.DrawRectangle(pen, viewRect.X, viewRect.Y, viewRect.Width, viewRect.Height);
                return;
            }

            float penWidth = isSelected ? 5 : 3;
            using (Pen pen = new Pen(boxColor, penWidth))
                g.DrawRectangle(pen, viewRect.X, viewRect.Y, viewRect.Width, viewRect.Height);

            string labelText = GetBoxLabelText(box);
            SizeF textSize = g.MeasureString(labelText, labelFont);
            RectangleF labelBg = new RectangleF(viewRect.X, viewRect.Y - textSize.Height - 4, textSize.Width + 8, textSize.Height + 4);

            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(200, boxColor)))
                g.FillRectangle(bgBrush, labelBg);
            using (SolidBrush textBrush = new SolidBrush(Color.White))
                g.DrawString(labelText, labelFont, textBrush, viewRect.X + 4, viewRect.Y - textSize.Height - 2);

            if (isSelected)
                DrawResizeHandles(g, viewRect);
        }

        private void DrawResizeHandles(Graphics g, RectangleF rect)
        {
            PointF[] corners = {
                new PointF(rect.X, rect.Y),
                new PointF(rect.X + rect.Width, rect.Y),
                new PointF(rect.X, rect.Y + rect.Height),
                new PointF(rect.X + rect.Width, rect.Y + rect.Height)
            };

            foreach (var center in corners)
            {
                float halfSize = HANDLE_SIZE / 2f;
                RectangleF handleRect = new RectangleF(center.X - halfSize, center.Y - halfSize, HANDLE_SIZE, HANDLE_SIZE);
                using (SolidBrush brush = new SolidBrush(Color.White))
                    g.FillRectangle(brush, handleRect);
                using (Pen pen = new Pen(Color.Black, 2))
                    g.DrawRectangle(pen, handleRect.X, handleRect.Y, handleRect.Width, handleRect.Height);
            }
        }

        private void pictureBoxVideo_MouseDown(object sender, MouseEventArgs e)
        {
            int currentFrameIndex = _videoService.CurrentFrameIndex;

            // Right-click: Person attribute editing
            if (e.Button == MouseButtons.Right)
            {
            }

            if (currentMode == DrawMode.Draw)
            {
                isDrawing = true;
                drawStartPoint = e.Location;
                var imagePoint = CoordinateHelper.ViewToImage(new PointF(e.X, e.Y), pictureBoxVideo);

                // NEW-01 (D-06): ID 승계 적용 — Waypoint 범위면 Entry 박스 ID, 아니면 currentAssignedId / selectedBox 우선
                int resolvedId = ResolveIdForNewBox(currentSelectedLabel, currentFrameIndex);

                drawingBox = new BoundingBox
                {
                    FrameIndex = currentFrameIndex,
                    Rectangle = new Rectangle((int)imagePoint.X, (int)imagePoint.Y, 0, 0),
                    Label = currentSelectedLabel,
                    PersonId = currentSelectedLabel == "person" ? resolvedId : 0,
                    VehicleId = currentSelectedLabel == "vehicle" ? resolvedId : 0,
                    EventId = currentSelectedLabel == "event" ? resolvedId : 0,
                    Action = "waypoint"
                };
            }
            else if (currentMode == DrawMode.Select)
            {
                // Check resize handles first
                if (selectedBox != null)
                {
                    var viewRect = CoordinateHelper.ImageToView(
                        new RectangleF(selectedBox.Rectangle.X, selectedBox.Rectangle.Y, selectedBox.Rectangle.Width, selectedBox.Rectangle.Height), pictureBoxVideo);
                    ResizeHandle handle = GetResizeHandleAtPoint(e.Location, viewRect);

                    if (handle != ResizeHandle.None)
                    {
                        isResizing = true;
                        currentResizeHandle = handle;
                        resizeStartPoint = e.Location;
                        originalResizeRect = selectedBox.Rectangle;
                        return;
                    }

                    // Inside selected box: drag
                    if (viewRect.Contains(e.Location))
                    {
                        if (HasAnotherHitCandidateAt(e.Location, selectedBox))
                        {
                            isWaitingForDoubleClick = true;
                            lastClickPoint = e.Location;
                            dragOffset = new System.Drawing.Point(e.X - (int)viewRect.X, e.Y - (int)viewRect.Y);
                            // DF-1-07 (05.6-04 Task 5-B): double-click 대기 경로에서도 drag 시작점 rect 스냅샷 (타이머 경과 후 drag 전환 시 사용)
                            originalDragRect = selectedBox.Rectangle;
                            doubleClickTimer?.Dispose();
                            doubleClickTimer = new System.Threading.Timer((state) =>
                            {
                                if (isWaitingForDoubleClick && !isDragging)
                                {
                                    if (!IsDisposed && IsHandleCreated)
                                    {
                                        this.Invoke((Action)(() =>
                                        {
                                            if (isWaitingForDoubleClick && selectedBox != null)
                                            {
                                                isDragging = true;
                                                isWaitingForDoubleClick = false;
                                                pictureBoxVideo.Invalidate();
                                            }
                                        }));
                                    }
                                }
                            }, null, 500, Timeout.Infinite);
                            return;
                        }
                        else
                        {
                            isDragging = true;
                            dragOffset = new System.Drawing.Point(e.X - (int)viewRect.X, e.Y - (int)viewRect.Y);
                            // DF-1-07 (05.6-04 Task 5-B): drag 시작 시점 rect 스냅샷 (MouseUp 에서 undo 푸시 판정용)
                            originalDragRect = selectedBox.Rectangle;
                            UpdateObjectInfo(selectedBox);
                            HighlightSelectedBoxInSidebar();
                            pictureBoxVideo.Invalidate();
                            return;
                        }
                    }
                }

                // Select a box
                selectedBox = GetBoundingBoxAt(e.Location);
                if (selectedBox != null)
                {
                    isDragging = true;
                    var viewRect = CoordinateHelper.ImageToView(
                        new RectangleF(selectedBox.Rectangle.X, selectedBox.Rectangle.Y, selectedBox.Rectangle.Width, selectedBox.Rectangle.Height), pictureBoxVideo);
                    dragOffset = new System.Drawing.Point(e.X - (int)viewRect.X, e.Y - (int)viewRect.Y);
                    // DF-1-07 (05.6-04 Task 5-B): drag 시작 시점 rect 스냅샷 (MouseUp 에서 undo 푸시 판정용)
                    originalDragRect = selectedBox.Rectangle;
                    UpdateObjectInfo(selectedBox);
                    UpdateBboxListDisplay();
                    HighlightSelectedBoxInSidebar();
                }
                else
                {
                    ClearSidebarHighlights();
                    UpdateObjectInfo(null);
                    UpdateBboxListDisplay();
                }
                pictureBoxVideo.Invalidate();
            }
        }

        private void pictureBoxVideo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (currentMode == DrawMode.Select && e.Button == MouseButtons.Left)
            {
                isWaitingForDoubleClick = false;
                doubleClickTimer?.Dispose();
                doubleClickTimer = null;
                if (isDragging) isDragging = false;

                var clickedBox = GetBoundingBoxAt(e.Location);
                if (clickedBox != null && HasAnotherHitCandidateAt(e.Location, clickedBox))
                {
                    var ordered = GetOrderedCandidatesAt(e.Location);
                    if (ordered.Count > 1)
                    {
                        int idx = ordered.IndexOf(clickedBox);
                        BoundingBox next = ordered[(idx + 1) % ordered.Count];
                        if (next != clickedBox)
                        {
                            selectedBox = next;
                            UpdateObjectInfo(selectedBox);
                            UpdateBboxListDisplay();
                            HighlightSelectedBoxInSidebar();
                            pictureBoxVideo.Invalidate();
                        }
                    }
                }
            }
        }

        private void pictureBoxVideo_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && drawingBox != null)
            {
                var startImagePoint = CoordinateHelper.ViewToImage(new PointF(drawStartPoint.X, drawStartPoint.Y), pictureBoxVideo);
                var currentImagePoint = CoordinateHelper.ViewToImage(new PointF(e.X, e.Y), pictureBoxVideo);

                int x = (int)Math.Min(startImagePoint.X, currentImagePoint.X);
                int y = (int)Math.Min(startImagePoint.Y, currentImagePoint.Y);
                int width = (int)Math.Abs(currentImagePoint.X - startImagePoint.X);
                int height = (int)Math.Abs(currentImagePoint.Y - startImagePoint.Y);
                drawingBox.Rectangle = new Rectangle(x, y, width, height);
                pictureBoxVideo.Invalidate();
            }
            else if (isResizing && selectedBox != null)
            {
                PerformResize(e.Location);
                pictureBoxVideo.Invalidate();
            }
            else if (isWaitingForDoubleClick && selectedBox != null)
            {
                int moveDistance = (int)Math.Sqrt(Math.Pow(e.X - lastClickPoint.X, 2) + Math.Pow(e.Y - lastClickPoint.Y, 2));
                if (moveDistance > 5)
                {
                    isDragging = true;
                    isWaitingForDoubleClick = false;
                    doubleClickTimer?.Dispose();
                    doubleClickTimer = null;
                }
            }
            else if (isDragging && selectedBox != null)
            {
                var viewPos = new PointF(e.X - dragOffset.X, e.Y - dragOffset.Y);
                var imagePos = CoordinateHelper.ViewToImage(viewPos, pictureBoxVideo);
                selectedBox.Rectangle = new Rectangle((int)imagePos.X, (int)imagePos.Y, selectedBox.Rectangle.Width, selectedBox.Rectangle.Height);
                // FUNC-03: 이미지 범위 클램핑
                if (pictureBoxVideo.Image != null)
                    selectedBox.Rectangle = CoordinateHelper.ClampToImage(selectedBox.Rectangle, pictureBoxVideo.Image.Width, pictureBoxVideo.Image.Height);
                pictureBoxVideo.Invalidate();
            }
            else if (currentMode == DrawMode.Select && selectedBox != null)
            {
                var viewRect = CoordinateHelper.ImageToView(
                    new RectangleF(selectedBox.Rectangle.X, selectedBox.Rectangle.Y, selectedBox.Rectangle.Width, selectedBox.Rectangle.Height), pictureBoxVideo);
                ResizeHandle handle = GetResizeHandleAtPoint(e.Location, viewRect);
                UpdateCursorForHandle(handle);
            }
            else
            {
                pictureBoxVideo.Cursor = currentMode == DrawMode.Select ? Cursors.Hand :
                    currentMode == DrawMode.Draw ? Cursors.Cross : Cursors.Default;
            }
        }

        private void pictureBoxVideo_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDrawing && drawingBox != null)
            {
                if (drawingBox.Rectangle.Width > 10 && drawingBox.Rectangle.Height > 10)
                {
                    // FUNC-03: 이미지 범위 클램핑
                    if (pictureBoxVideo.Image != null)
                        drawingBox.Rectangle = CoordinateHelper.ClampToImage(drawingBox.Rectangle, pictureBoxVideo.Image.Width, pictureBoxVideo.Image.Height);
                    boundingBoxes.Add(drawingBox);
                    InvalidateBoxCache();
                    AddUndoAction(new UndoAction { Type = UndoActionType.AddBox, Box = drawingBox });
                    // DF-1-17 (D-17a): 사용자 주도 BBOX 생성 감사 이벤트 (MouseUp 경로 — 주 진입점)
                    // 프로그래밍적 전파/추적 연장(line 502, 2480, 2504, 2537) 및 Undo/Redo 경로는 제외 — 신호 품질 유지
                    LogService.AuditBBoxCreate(drawingBox.Label, GetBoxId(drawingBox), drawingBox.FrameIndex);
                    selectedBox = drawingBox;
                    UpdateObjectInfo(selectedBox);
                    UpdateBoxCount();
                    UpdateBboxListDisplay();
                }
                drawingBox = null;
                isDrawing = false;
                pictureBoxVideo.Invalidate();
            }
            else if (isResizing)
            {
                isResizing = false;
                currentResizeHandle = ResizeHandle.None;
                if (selectedBox == null) return;  // RELI-04 null guard
                // DF-1-07 (05.6-04 Task 5-B): 기존 코드는 Box.Rectangle 에 originalResizeRect 를 넣고 OriginalRectangle/Label/ObjectId 는
                // 세팅하지 않아서 Undo() 가 boxToModify.Rectangle = (0,0,0,0), Label = null, Id = 0 으로 복원하는 심각한 버그였음.
                // 수정: Box 는 변경 후 상태(라이브 박스 탐색용), Original* 필드에 되돌릴 값을 채운다.
                if (selectedBox.Rectangle != originalResizeRect)
                {
                    PushModifyBoxUndo(selectedBox, originalResizeRect, selectedBox.Label, GetBoxId(selectedBox));
                }
                InvalidateBoxCache();
                // NEW-08 fix (2026-04-24): Waypoint cross-frame Rectangle 전파 제거.
                // 기존 Propagate*FromCurrentFrame 호출은 resize 시 현재 프레임 이후의 모든 박스
                // Rectangle을 덮어써서 사용자가 "뒤 프레임 좌표가 수정 프레임으로 고정됨"으로 관찰.
                // 사용자 결정: "기존 waypoint에 저장된 bbox 좌표값들을 그대로 유지해야지."
                // 드래그/리사이즈는 현재 프레임의 선택 박스만 수정, 다른 프레임은 불변.
                pictureBoxVideo.Cursor = Cursors.Default;
                pictureBoxVideo.Invalidate();
            }
            else if (isDragging || isWaitingForDoubleClick)
            {
                if (isWaitingForDoubleClick && !isDragging) isDragging = true;
                if (isDragging)
                {
                    isDragging = false;
                    if (selectedBox != null)
                    {
                        // DF-1-07 (05.6-04 Task 5-B): drag 로 rect 가 실제 변경된 경우에만 ModifyBox Undo 엔트리 푸시.
                        // 단순 클릭 (drag 거리 0) 에서는 엔트리 미생성 — 불필요한 undo stack 축적 방지.
                        if (selectedBox.Rectangle != originalDragRect)
                        {
                            PushModifyBoxUndo(selectedBox, originalDragRect, selectedBox.Label, GetBoxId(selectedBox));
                        }

                        // NEW-08 fix (2026-04-24): 드래그로 변경된 Rectangle을 이후 프레임들에
                        // 전파하던 Propagate*FromCurrentFrame 호출 제거. 각 프레임의 박스는 독립 관리.
                    }
                }
                doubleClickTimer?.Dispose();
                doubleClickTimer = null;
                isWaitingForDoubleClick = false;
            }
        }

        private void pictureBoxVideo_Resize(object sender, EventArgs e)
        {
            if (labelSubtitleTimestamp != null && pictureBoxVideo != null)
            {
                labelSubtitleTimestamp.Location = new System.Drawing.Point(50, pictureBoxVideo.Height - labelSubtitleTimestamp.Height - 30);
            }
            // RELI-06: 로딩 라벨 중앙 유지
            CenterLoadingLabel();
        }

        private void panelVideoControls_Resize(object sender, EventArgs e)
        {
            if (panelVideoControls != null && groupBoxObjectInfo != null)
            {
                groupBoxObjectInfo.Location = new System.Drawing.Point(panelVideoControls.Width - groupBoxObjectInfo.Width - 16, 16);
            }
            // 타임라인 너비를 컨테이너 크기에 맞게 동적으로 조정
            if (panelVideoControls != null && panelTimeline != null && groupBoxObjectInfo != null)
            {
                int newWidth = Math.Max(100, groupBoxObjectInfo.Left - panelTimeline.Left - 8);
                if (panelTimeline.Width != newWidth)
                    panelTimeline.Width = newWidth;
            }
        }

        #endregion

        #region Hit Testing & Selection

        private BoundingBox GetBoundingBoxAt(System.Drawing.Point location)
        {
            var imageLocation = CoordinateHelper.ViewToImage(new PointF(location.X, location.Y), pictureBoxVideo);
            int currentFrameIndex = _videoService.CurrentFrameIndex;
            var currentFrameBoxes = GetBboxesForFrame(currentFrameIndex).Where(b => !b.IsDeleted).ToList();

            var candidates = new List<(BoundingBox box, bool inActiveWaypoint, int labelPri, double dist, int area, int zIndex)>();
            foreach (var box in currentFrameBoxes)
            {
                var r = box.Rectangle; r.Inflate(HIT_MARGIN, HIT_MARGIN);
                if (!r.Contains((int)imageLocation.X, (int)imageLocation.Y)) continue;

                bool inActiveWaypoint = selectedWaypoint != null &&
                    box.Label == selectedWaypoint.Label && GetBoxId(box) == selectedWaypoint.ObjectId &&
                    currentFrameIndex >= selectedWaypoint.EntryFrame && currentFrameIndex <= selectedWaypoint.ExitFrame;

                int labelPri = GetLabelPriority(box.Label);
                double cx = box.Rectangle.X + box.Rectangle.Width / 2.0;
                double cy = box.Rectangle.Y + box.Rectangle.Height / 2.0;
                double dist = Math.Sqrt((cx - imageLocation.X) * (cx - imageLocation.X) + (cy - imageLocation.Y) * (cy - imageLocation.Y));
                int area = Math.Max(1, box.Rectangle.Width * box.Rectangle.Height);
                int zIndex = boundingBoxes.IndexOf(box);
                candidates.Add((box, inActiveWaypoint, labelPri, dist, area, zIndex));
            }

            if (candidates.Count == 0) return null;

            var ordered = candidates
                .OrderByDescending(c => c.inActiveWaypoint).ThenByDescending(c => c.labelPri)
                .ThenBy(c => c.dist).ThenBy(c => c.area).ThenByDescending(c => c.zIndex).ToList();

            lastHitCandidates = ordered.Select(c => c.box).ToList();
            lastHitIndex = 0;
            lastClickViewPoint = location;
            return lastHitCandidates[0];
        }

        private bool HasAnotherHitCandidateAt(System.Drawing.Point viewLocation, BoundingBox exclude)
        {
            var imageLocation = CoordinateHelper.ViewToImage(new PointF(viewLocation.X, viewLocation.Y), pictureBoxVideo);
            int currentFrameIndex = _videoService.CurrentFrameIndex;
            foreach (var box in GetBboxesForFrame(currentFrameIndex).Where(b => !b.IsDeleted))
            {
                if (box == exclude) continue;
                var r = box.Rectangle; r.Inflate(HIT_MARGIN, HIT_MARGIN);
                if (r.Contains((int)imageLocation.X, (int)imageLocation.Y)) return true;
            }
            return false;
        }

        private List<BoundingBox> GetOrderedCandidatesAt(System.Drawing.Point viewLocation)
        {
            var imageLocation = CoordinateHelper.ViewToImage(new PointF(viewLocation.X, viewLocation.Y), pictureBoxVideo);
            int currentFrameIndex = _videoService.CurrentFrameIndex;
            var candidates = new List<(BoundingBox box, int labelPri, double dist, int area, int zIndex)>();

            foreach (var box in GetBboxesForFrame(currentFrameIndex).Where(b => !b.IsDeleted))
            {
                var r = box.Rectangle; r.Inflate(HIT_MARGIN, HIT_MARGIN);
                if (!r.Contains((int)imageLocation.X, (int)imageLocation.Y)) continue;
                int labelPri = GetLabelPriority(box.Label);
                double cx = box.Rectangle.X + box.Rectangle.Width / 2.0;
                double cy = box.Rectangle.Y + box.Rectangle.Height / 2.0;
                double dist = Math.Sqrt((cx - imageLocation.X) * (cx - imageLocation.X) + (cy - imageLocation.Y) * (cy - imageLocation.Y));
                int area = Math.Max(1, box.Rectangle.Width * box.Rectangle.Height);
                int zIndex = boundingBoxes.IndexOf(box);
                candidates.Add((box, labelPri, dist, area, zIndex));
            }

            return candidates.OrderByDescending(c => c.labelPri).ThenBy(c => c.dist).ThenBy(c => c.area).ThenByDescending(c => c.zIndex).Select(c => c.box).ToList();
        }

        private int GetLabelPriority(string label)
        {
            return (label ?? "").ToLower() switch { "person" => 3, "vehicle" => 2, "event" => 1, _ => 0 };
        }

        private void CycleSelection(bool reverse)
        {
            if (lastHitCandidates == null || lastHitCandidates.Count == 0) return;
            lastHitIndex = reverse ? (lastHitIndex - 1 + lastHitCandidates.Count) % lastHitCandidates.Count : (lastHitIndex + 1) % lastHitCandidates.Count;
            selectedBox = lastHitCandidates[lastHitIndex];
            HighlightSelectedBoxInSidebar();
            UpdateObjectInfo(selectedBox);
            UpdateBboxListDisplay();
            pictureBoxVideo.Invalidate();
        }

        #endregion

        #region Resize Helpers

        private ResizeHandle GetResizeHandleAtPoint(System.Drawing.Point viewPoint, RectangleF viewRect)
        {
            float tolerance = HANDLE_SIZE / 2f + 2;
            if (Distance(viewPoint, new PointF(viewRect.X, viewRect.Y)) <= tolerance) return ResizeHandle.TopLeft;
            if (Distance(viewPoint, new PointF(viewRect.X + viewRect.Width, viewRect.Y)) <= tolerance) return ResizeHandle.TopRight;
            if (Distance(viewPoint, new PointF(viewRect.X, viewRect.Y + viewRect.Height)) <= tolerance) return ResizeHandle.BottomLeft;
            if (Distance(viewPoint, new PointF(viewRect.X + viewRect.Width, viewRect.Y + viewRect.Height)) <= tolerance) return ResizeHandle.BottomRight;
            return ResizeHandle.None;
        }

        private float Distance(PointF p1, PointF p2) => (float)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));

        private void PerformResize(System.Drawing.Point currentViewPoint)
        {
            if (selectedBox == null || currentResizeHandle == ResizeHandle.None) return;

            int deltaX = currentViewPoint.X - resizeStartPoint.X;
            int deltaY = currentViewPoint.Y - resizeStartPoint.Y;
            var imageDelta = ViewToImageDistance(new PointF(deltaX, deltaY));

            Rectangle newRect = originalResizeRect;

            switch (currentResizeHandle)
            {
                case ResizeHandle.TopLeft:
                    int newLeft = originalResizeRect.X + (int)imageDelta.X;
                    int newTop = originalResizeRect.Y + (int)imageDelta.Y;
                    int newW = originalResizeRect.Right - newLeft;
                    int newH = originalResizeRect.Bottom - newTop;
                    newRect = new Rectangle(newW >= MIN_BBOX_SIZE ? newLeft : originalResizeRect.Right - MIN_BBOX_SIZE,
                        newH >= MIN_BBOX_SIZE ? newTop : originalResizeRect.Bottom - MIN_BBOX_SIZE,
                        Math.Max(newW, MIN_BBOX_SIZE), Math.Max(newH, MIN_BBOX_SIZE));
                    break;
                case ResizeHandle.TopRight:
                    newTop = originalResizeRect.Y + (int)imageDelta.Y;
                    newW = originalResizeRect.Width + (int)imageDelta.X;
                    newH = originalResizeRect.Bottom - newTop;
                    newRect = new Rectangle(originalResizeRect.X,
                        newH >= MIN_BBOX_SIZE ? newTop : originalResizeRect.Bottom - MIN_BBOX_SIZE,
                        Math.Max(newW, MIN_BBOX_SIZE), Math.Max(newH, MIN_BBOX_SIZE));
                    break;
                case ResizeHandle.BottomLeft:
                    newLeft = originalResizeRect.X + (int)imageDelta.X;
                    newW = originalResizeRect.Right - newLeft;
                    newH = originalResizeRect.Height + (int)imageDelta.Y;
                    newRect = new Rectangle(newW >= MIN_BBOX_SIZE ? newLeft : originalResizeRect.Right - MIN_BBOX_SIZE,
                        originalResizeRect.Y, Math.Max(newW, MIN_BBOX_SIZE), Math.Max(newH, MIN_BBOX_SIZE));
                    break;
                case ResizeHandle.BottomRight:
                    newW = originalResizeRect.Width + (int)imageDelta.X;
                    newH = originalResizeRect.Height + (int)imageDelta.Y;
                    newRect = new Rectangle(originalResizeRect.X, originalResizeRect.Y, Math.Max(newW, MIN_BBOX_SIZE), Math.Max(newH, MIN_BBOX_SIZE));
                    break;
            }

            // FUNC-03: 이미지 범위 클램핑
            if (pictureBoxVideo.Image != null)
                newRect = CoordinateHelper.ClampToImage(newRect, pictureBoxVideo.Image.Width, pictureBoxVideo.Image.Height);
            selectedBox.Rectangle = newRect;
        }

        private PointF ViewToImageDistance(PointF viewDelta)
        {
            if (pictureBoxVideo.Image == null) return viewDelta;
            var displayRect = CoordinateHelper.GetImageDisplayRectangle(pictureBoxVideo);
            float scaleX = pictureBoxVideo.Image.Width / displayRect.Width;
            float scaleY = pictureBoxVideo.Image.Height / displayRect.Height;
            return new PointF(viewDelta.X * scaleX, viewDelta.Y * scaleY);
        }

        private void UpdateCursorForHandle(ResizeHandle handle)
        {
            pictureBoxVideo.Cursor = handle switch
            {
                ResizeHandle.TopLeft => Cursors.SizeNWSE,
                ResizeHandle.TopRight => Cursors.SizeNESW,
                ResizeHandle.BottomLeft => Cursors.SizeNESW,
                ResizeHandle.BottomRight => Cursors.SizeNWSE,
                _ => currentMode == DrawMode.Select ? Cursors.Hand : Cursors.Cross
            };
        }

        #endregion

        #region Timeline

        // 타임라인 레이아웃 상수
        private const int TIMELINE_HEADER_HEIGHT = 8;   // 플레이헤드 삼각형 영역
        private const int TIMELINE_ROW_HEIGHT = 16;     // 각 레이블 행 높이
        private const int TIMELINE_ROW_PADDING = 1;     // 행 내부 상하 패딩
        private const int TIMELINE_LABEL_WIDTH = 16;    // "P","V","E" 레이블 영역
        private const int TIMELINE_SEGMENT_RADIUS = 3;  // 둥근 사각형 반지름

        private static readonly Font timelineFont = new Font("Segoe UI", 7F, FontStyle.Bold);
        private static readonly Font timelineLabelFont = new Font("Segoe UI", 7F, FontStyle.Bold);

        /// <summary>
        /// 레이블에 해당하는 행 Y좌표 반환
        /// </summary>
        private int GetTimelineRowY(string label)
        {
            return label switch
            {
                "person" => TIMELINE_HEADER_HEIGHT,
                "vehicle" => TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT,
                "event" => TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT * 2,
                _ => TIMELINE_HEADER_HEIGHT
            };
        }

        /// <summary>
        /// 레이블에 해당하는 세그먼트 색상 반환
        /// </summary>
        private Color GetTimelineSegmentColor(string label)
        {
            return label switch
            {
                "person" => Color.FromArgb(200, 255, 107, 107),
                "vehicle" => Color.FromArgb(200, 107, 158, 255),
                "event" => Color.FromArgb(200, 107, 255, 107),
                _ => Color.FromArgb(200, 180, 180, 180)
            };
        }

        /// <summary>
        /// 둥근 사각형 GraphicsPath 생성
        /// </summary>
        private GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            if (rect.Width < radius * 2) radius = rect.Width / 2;
            if (rect.Height < radius * 2) radius = rect.Height / 2;
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void panelTimeline_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int width = panelTimeline.Width;
            int height = panelTimeline.Height;
            int totalFrames = _videoService.TotalFrames;

            // 1. 배경 채우기
            using (var bgBrush = new SolidBrush(Color.FromArgb(45, 45, 48)))
                g.FillRectangle(bgBrush, 0, 0, width, height);

            // 2. 행 구분선
            using (var linePen = new Pen(Color.FromArgb(63, 63, 70), 1))
            {
                int y1 = TIMELINE_HEADER_HEIGHT;
                g.DrawLine(linePen, 0, y1, width, y1);
                int y2 = y1 + TIMELINE_ROW_HEIGHT;
                g.DrawLine(linePen, 0, y2, width, y2);
                int y3 = y2 + TIMELINE_ROW_HEIGHT;
                g.DrawLine(linePen, 0, y3, width, y3);
                int y4 = y3 + TIMELINE_ROW_HEIGHT;
                g.DrawLine(linePen, 0, y4, width, y4);
            }

            // 3. 행 레이블 ("P", "V", "E")
            using (var labelBrush = new SolidBrush(Color.FromArgb(120, 255, 107, 107)))
                g.DrawString("P", timelineLabelFont, labelBrush, 2, TIMELINE_HEADER_HEIGHT + 1);
            using (var labelBrush = new SolidBrush(Color.FromArgb(120, 107, 158, 255)))
                g.DrawString("V", timelineLabelFont, labelBrush, 2, TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT + 1);
            using (var labelBrush = new SolidBrush(Color.FromArgb(120, 107, 255, 107)))
                g.DrawString("E", timelineLabelFont, labelBrush, 2, TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT * 2 + 1);

            if (totalFrames > 0)
            {
                int trackLeft = TIMELINE_LABEL_WIDTH;
                int trackWidth = width - trackLeft;

                // 4. 웨이포인트 세그먼트 (둥근 사각형)
                foreach (var waypoint in waypointMarkers)
                {
                    int rowY = GetTimelineRowY(waypoint.Label);
                    Color segColor = GetTimelineSegmentColor(waypoint.Label);

                    int startX = trackLeft + (int)(trackWidth * ((float)waypoint.EntryFrame / totalFrames));
                    int endX = trackLeft + (int)(trackWidth * ((float)waypoint.ExitFrame / totalFrames));
                    int segWidth = Math.Max(endX - startX, 4); // 최소 4px

                    var segRect = new RectangleF(startX, rowY + TIMELINE_ROW_PADDING, segWidth, TIMELINE_ROW_HEIGHT - TIMELINE_ROW_PADDING * 2);

                    // 세그먼트 채우기
                    using (var path = CreateRoundedRect(segRect, TIMELINE_SEGMENT_RADIUS))
                    using (var brush = new SolidBrush(segColor))
                    {
                        g.FillPath(brush, path);
                    }

                    // 선택된 웨이포인트 강조
                    bool isSelected = selectedWaypoint != null &&
                        waypoint.Label == selectedWaypoint.Label &&
                        waypoint.ObjectId == selectedWaypoint.ObjectId &&
                        waypoint.EntryFrame == selectedWaypoint.EntryFrame;

                    if (isSelected)
                    {
                        using (var path = CreateRoundedRect(segRect, TIMELINE_SEGMENT_RADIUS))
                        using (var pen = new Pen(Color.White, 2))
                        {
                            g.DrawPath(pen, path);
                        }
                    }

                    // 세그먼트 내 ID 텍스트 (폭이 충분할 때)
                    if (segWidth > 25)
                    {
                        string prefix = waypoint.Label switch
                        {
                            "person" => "P",
                            "vehicle" => "V",
                            "event" => "E",
                            _ => "?"
                        };
                        string idText = $"{prefix}{waypoint.ObjectId:D2}";
                        using (var textBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                        {
                            var textRect = new RectangleF(startX + 2, rowY + TIMELINE_ROW_PADDING, segWidth - 4, TIMELINE_ROW_HEIGHT - TIMELINE_ROW_PADDING * 2);
                            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                            g.DrawString(idText, timelineFont, textBrush, textRect, sf);
                        }
                    }
                }

                // 5. 플레이헤드 (삼각형 + 세로선)
                int currentX = trackLeft + (int)(trackWidth * timelineProgress);

                // 세로선
                using (var pen = new Pen(Color.FromArgb(220, 255, 255, 255), 1.5f))
                    g.DrawLine(pen, currentX, TIMELINE_HEADER_HEIGHT, currentX, height);

                // 삼각형 머리 (Accent 색상)
                var triangle = new System.Drawing.Point[]
                {
                    new System.Drawing.Point(currentX, TIMELINE_HEADER_HEIGHT),
                    new System.Drawing.Point(currentX - 5, 0),
                    new System.Drawing.Point(currentX + 5, 0)
                };
                using (var brush = new SolidBrush(Color.FromArgb(0, 120, 212)))
                    g.FillPolygon(brush, triangle);
                using (var pen = new Pen(Color.FromArgb(30, 140, 230), 1))
                    g.DrawPolygon(pen, triangle);
            }

            // 6. Entry 설정중 표시
            if (entryFrameIndex.HasValue && !exitFrameIndex.HasValue && totalFrames > 0)
            {
                int trackLeft = TIMELINE_LABEL_WIDTH;
                int trackWidth = width - trackLeft;
                int entryX = trackLeft + (int)(trackWidth * ((float)entryFrameIndex.Value / totalFrames));

                // 빨간 세로선
                using (var pen = new Pen(Color.FromArgb(255, 68, 68), 2))
                    g.DrawLine(pen, entryX, 0, entryX, height);

                // 빨간 삼각형
                var tri = new System.Drawing.Point[]
                {
                    new System.Drawing.Point(entryX, TIMELINE_HEADER_HEIGHT),
                    new System.Drawing.Point(entryX - 5, 0),
                    new System.Drawing.Point(entryX + 5, 0)
                };
                using (var brush = new SolidBrush(Color.FromArgb(255, 68, 68)))
                    g.FillPolygon(brush, tri);
            }
        }

        /// <summary>
        /// 클릭 위치에서 웨이포인트 세그먼트 감지 + 선택
        /// </summary>
        private bool TryNavigateToMarker(int mouseX, int mouseY)
        {
            if (_videoService.TotalFrames == 0) return false;

            int width = panelTimeline.Width;
            int totalFrames = _videoService.TotalFrames;
            int trackLeft = TIMELINE_LABEL_WIDTH;
            int trackWidth = width - trackLeft;

            // 클릭 Y좌표로 어떤 행인지 판별
            string targetLabel = null;
            if (mouseY >= TIMELINE_HEADER_HEIGHT && mouseY < TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT)
                targetLabel = "person";
            else if (mouseY >= TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT && mouseY < TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT * 2)
                targetLabel = "vehicle";
            else if (mouseY >= TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT * 2 && mouseY < TIMELINE_HEADER_HEIGHT + TIMELINE_ROW_HEIGHT * 3)
                targetLabel = "event";

            if (targetLabel == null) return false;

            // 해당 행의 세그먼트 중 X좌표에 해당하는 웨이포인트 찾기
            foreach (var waypoint in waypointMarkers)
            {
                if (waypoint.Label != targetLabel) continue;

                int startX = trackLeft + (int)(trackWidth * ((float)waypoint.EntryFrame / totalFrames));
                int endX = trackLeft + (int)(trackWidth * ((float)waypoint.ExitFrame / totalFrames));

                if (mouseX >= startX - 2 && mouseX <= endX + 2)
                {
                    // 해당 웨이포인트의 Entry 프레임으로 이동 + 선택
                    selectedWaypoint = waypoint;
                    LoadFrame(waypoint.EntryFrame);
                    panelTimeline.Invalidate();

                    // 우측 ListView에서도 해당 웨이포인트 선택
                    SelectWaypointInListView(waypoint);
                    return true;
                }
            }

            return false;
        }

        private void panelTimeline_MouseDown(object sender, MouseEventArgs e)
        {
            // RELI-06: 영상 로드 완료 전 타임라인 입력 무시 (조용히 return, D-08)
            if (!_videoService.IsVideoLoaded) return;
            if (_videoService.TotalFrames == 0) return;
            // RELI-06 (05.5-02): cold-decoder 기간 타임라인 클릭 무시
            if (!_isVideoReady) return;

            // 세그먼트 클릭 감지 먼저
            if (TryNavigateToMarker(e.X, e.Y))
                return;

            // 세그먼트가 아니면 드래그 시작
            isTimelineDragging = true;
            UpdateFrameFromTimeline(e.X);
        }

        private void panelTimeline_MouseMove(object sender, MouseEventArgs e)
        {
            // RELI-06: 영상 로드 완료 전 드래그 이동 무시
            if (!_videoService.IsVideoLoaded) return;
            // RELI-06 (05.5-02): cold-decoder 기간 드래그 이동 무시
            if (!_isVideoReady) return;
            if (isTimelineDragging && _videoService.TotalFrames > 0)
                UpdateFrameFromTimeline(e.X);
        }

        private void panelTimeline_MouseUp(object sender, MouseEventArgs e) => isTimelineDragging = false;

        private void UpdateFrameFromTimeline(int mouseX)
        {
            if (_videoService.TotalFrames == 0) return;
            int trackLeft = TIMELINE_LABEL_WIDTH;
            int trackWidth = panelTimeline.Width - trackLeft;
            float clickPosition = Math.Max(0, Math.Min(1, (float)(mouseX - trackLeft) / trackWidth));
            int targetFrame = Math.Max(0, Math.Min(_videoService.TotalFrames - 1, (int)(clickPosition * _videoService.TotalFrames)));
            LoadFrame(targetFrame);
        }

        #endregion

        #region Undo/Redo

        private void AddUndoAction(UndoAction action)
        {
            _isDirty = true;
            undoStack.Push(action);
            // PERF-03: Enforce MAX_UNDO_STACK limit - removes oldest (bottom) entry
            if (undoStack.Count > MAX_UNDO_STACK)
            {
                var tempList = undoStack.ToList();
                tempList.RemoveAt(tempList.Count - 1);
                undoStack = new Stack<UndoAction>(tempList.AsEnumerable().Reverse());
            }
            redoStack.Clear();
        }

        /// <summary>
        /// DF-1-07 (05.6-04 Task 5-B): ModifyBox 용 Undo 엔트리 표준 생성 헬퍼.
        /// Undo() 핸들러 계약:
        ///   - action.Box          = 변경 후 상태 (GetBoxId + Label + FrameIndex 로 라이브 박스 탐색)
        ///   - OriginalRectangle/Label/ObjectId = 되돌릴 원본 값 (세 필드 모두 반드시 채워야 함)
        /// 호출처: W/A/S/D 키 이동, 마우스 drag 이동 MouseUp, 마우스 resize MouseUp.
        /// </summary>
        private void PushModifyBoxUndo(BoundingBox currentState, Rectangle originalRect, string originalLabel, int originalObjectId)
        {
            AddUndoAction(new UndoAction
            {
                Type = UndoActionType.ModifyBox,
                Box = CloneBoundingBox(currentState),
                OriginalRectangle = originalRect,
                OriginalLabel = originalLabel,
                OriginalObjectId = originalObjectId,
            });
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;
            var action = undoStack.Pop();
            switch (action.Type)
            {
                case UndoActionType.AddBox:
                    // DF-1-07 (05.6-04 Task 5-D): 2 단계 매칭 — 참조 동일성 fast-path + Rectangle 포함 값 매칭 fallback.
                    // 5-C 의 (FrameIndex+Label+Id) 만 사용한 값 매칭은 같은 클래스·같은 기본 Id(예: Person Id=1) 박스가
                    // 3 개 공존할 때 모두 match 되어 FirstOrDefault 가 틀린 박스(A)를 제거하는 회귀 발생.
                    // Rectangle 을 포함시키되, clone round-trip 된 경우에도 AddBox action.Box 는 live 참조이므로 fast-path 로 선 히트.
                    {
                        var toRemove = boundingBoxes.FirstOrDefault(b => ReferenceEquals(b, action.Box))
                            ?? boundingBoxes.FirstOrDefault(b =>
                                b.FrameIndex == action.Box.FrameIndex &&
                                b.Label == action.Box.Label &&
                                GetBoxId(b) == GetBoxId(action.Box) &&
                                b.Rectangle == action.Box.Rectangle);
                        if (toRemove != null)
                        {
                            boundingBoxes.Remove(toRemove);
                            if (selectedBox == toRemove) selectedBox = null;
                        }
                        InvalidateBoxCache();
                    }
                    break;
                case UndoActionType.RemoveBox: boundingBoxes.Add(action.Box); InvalidateBoxCache(); break;
                case UndoActionType.ModifyBox:
                    var boxToModify = boundingBoxes.FirstOrDefault(b => b.FrameIndex == action.Box.FrameIndex && GetBoxId(b) == GetBoxId(action.Box) && b.Label == action.Box.Label);
                    if (boxToModify != null) { boxToModify.Rectangle = action.OriginalRectangle; boxToModify.Label = action.OriginalLabel; SetBoxId(boxToModify, action.OriginalLabel, action.OriginalObjectId); InvalidateBoxCache(); }
                    break;
                case UndoActionType.RemoveWaypointWithBoxes:
                    if (action.AffectedBoxes != null)
                        foreach (var b in action.AffectedBoxes) boundingBoxes.Add(b);
                    if (action.AffectedWaypoint != null && !waypointMarkers.Contains(action.AffectedWaypoint))
                        waypointMarkers.Add(action.AffectedWaypoint);
                    InvalidateBoxCache();
                    UpdateWaypointListView();
                    panelTimeline.Invalidate();
                    break;
            }
            redoStack.Push(action);
            UpdateBoxCount(); UpdateBboxListDisplay(); pictureBoxVideo.Invalidate();
        }

        private void Redo()
        {
            if (redoStack.Count == 0) return;
            var action = redoStack.Pop();
            switch (action.Type)
            {
                case UndoActionType.AddBox: boundingBoxes.Add(action.Box); InvalidateBoxCache(); break;
                case UndoActionType.RemoveBox:
                    // DF-1-07 (05.6-04 Task 5-D): Undo AddBox 와 대칭 — 참조 동일성 fast-path + Rectangle 포함 값 매칭 fallback.
                    {
                        var toRemove = boundingBoxes.FirstOrDefault(b => ReferenceEquals(b, action.Box))
                            ?? boundingBoxes.FirstOrDefault(b =>
                                b.FrameIndex == action.Box.FrameIndex &&
                                b.Label == action.Box.Label &&
                                GetBoxId(b) == GetBoxId(action.Box) &&
                                b.Rectangle == action.Box.Rectangle);
                        if (toRemove != null)
                        {
                            boundingBoxes.Remove(toRemove);
                            if (selectedBox == toRemove) selectedBox = null;
                        }
                        InvalidateBoxCache();
                    }
                    break;
                case UndoActionType.ModifyBox:
                    var boxToModify = boundingBoxes.FirstOrDefault(b => b.FrameIndex == action.Box.FrameIndex && GetBoxId(b) == action.OriginalObjectId && b.Label == action.OriginalLabel);
                    if (boxToModify != null) { boxToModify.Rectangle = action.Box.Rectangle; boxToModify.Label = action.Box.Label; SetBoxId(boxToModify, action.Box.Label, GetBoxId(action.Box)); InvalidateBoxCache(); }
                    break;
                case UndoActionType.RemoveWaypointWithBoxes:
                    if (action.AffectedBoxes != null)
                        foreach (var b in action.AffectedBoxes)
                        {
                            var live = boundingBoxes.FirstOrDefault(x => ReferenceEquals(x, b));
                            if (live != null) boundingBoxes.Remove(live);
                        }
                    if (action.AffectedWaypoint != null) waypointMarkers.Remove(action.AffectedWaypoint);
                    InvalidateBoxCache();
                    UpdateWaypointListView();
                    panelTimeline.Invalidate();
                    break;
            }
            undoStack.Push(action);
            UpdateBoxCount(); UpdateBboxListDisplay(); pictureBoxVideo.Invalidate();
        }

        #endregion

        #region Event Box Propagation

        private void PropagateEventBoxWithinRange(BoundingBox box, int endFrame)
        {
            if (box.Label != "event") return;
            for (int frame = box.FrameIndex + 1; frame <= endFrame; frame++)
            {
                if (!boundingBoxes.Any(b => b.FrameIndex == frame && b.Label == "event" && b.EventId == box.EventId))
                {
                    boundingBoxes.Add(new BoundingBox
                    {
                        FrameIndex = frame, Rectangle = box.Rectangle, Label = "event",
                        EventId = box.EventId, Action = "waypoint"
                    });
                }
            }
            InvalidateBoxCache(); UpdateBoxCount();
        }

        private void PropagateEventBoxFromCurrentFrame(BoundingBox box)
        {
            if (box.Label != "event") return;
            var waypoint = waypointMarkers.FirstOrDefault(w => box.FrameIndex >= w.EntryFrame && box.FrameIndex <= w.ExitFrame && w.Label == "event" && w.ObjectId == box.EventId);
            if (waypoint == null) return;

            var boxesToUpdate = boundingBoxes.Where(b => b.FrameIndex > box.FrameIndex && b.FrameIndex <= waypoint.ExitFrame && b.Label == "event" && b.EventId == box.EventId).ToList();
            foreach (var target in boxesToUpdate) target.Rectangle = box.Rectangle;

            if (boxesToUpdate.Count == 0)
            {
                for (int frame = box.FrameIndex + 1; frame <= waypoint.ExitFrame; frame++)
                {
                    if (!boundingBoxes.Any(b => b.FrameIndex == frame && b.Label == "event" && b.EventId == box.EventId))
                        boundingBoxes.Add(new BoundingBox { FrameIndex = frame, Rectangle = box.Rectangle, Label = "event", EventId = box.EventId, Action = "waypoint" });
                }
            }
            InvalidateBoxCache(); UpdateBoxCount(); UpdateBboxListDisplay();
        }

        /// <summary>
        /// USAB-08: Person/Vehicle 박스의 현재 위치를 웨이포인트 범위 내 이후 프레임에 전파합니다.
        /// Event 클래스의 PropagateEventBoxFromCurrentFrame과 동일한 동작.
        /// - 이후 프레임에 같은 label+ID 박스가 있으면 위치 덮어쓰기
        /// - 없으면 새 박스 생성 (수동 추적 시 프레임마다 박스 생성)
        /// </summary>
        private void PropagatePersonVehicleBoxFromCurrentFrame(BoundingBox box)
        {
            if (box.Label != "person" && box.Label != "vehicle") return;
            int objId = GetBoxId(box);
            var waypoint = waypointMarkers.FirstOrDefault(w =>
                box.FrameIndex >= w.EntryFrame && box.FrameIndex <= w.ExitFrame &&
                w.Label == box.Label && w.ObjectId == objId);
            if (waypoint == null) return;

            var boxesToUpdate = boundingBoxes.Where(b =>
                b.FrameIndex > box.FrameIndex && b.FrameIndex <= waypoint.ExitFrame &&
                b.Label == box.Label && GetBoxId(b) == objId && !b.IsDeleted).ToList();
            foreach (var target in boxesToUpdate) target.Rectangle = box.Rectangle;

            if (boxesToUpdate.Count == 0)
            {
                for (int frame = box.FrameIndex + 1; frame <= waypoint.ExitFrame; frame++)
                {
                    bool exists = boundingBoxes.Any(b =>
                        b.FrameIndex == frame && b.Label == box.Label && GetBoxId(b) == objId);
                    if (!exists)
                        boundingBoxes.Add(new BoundingBox
                        {
                            FrameIndex = frame, Rectangle = box.Rectangle, Label = box.Label,
                            PersonId = box.PersonId, VehicleId = box.VehicleId, EventId = box.EventId,
                            Action = "waypoint"
                        });
                }
            }
            InvalidateBoxCache(); UpdateBoxCount(); UpdateBboxListDisplay();
        }

        #endregion

        #region Keyboard Shortcuts

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool isVideoLoaded = _videoService.IsVideoLoaded;
            // RELI-06 (05.5-02): 화살표 seek 폭주 방지 — isVideoReady 전엔 seek 키 무시.
            if (isVideoLoaded && _isVideoReady)
            {
                if (keyData == (Keys.Shift | Keys.Left)) { LoadFrame(Math.Max(0, _videoService.CurrentFrameIndex - (int)(_videoService.Fps * 2))); return true; }
                if (keyData == (Keys.Shift | Keys.Right)) { LoadFrame(Math.Min(_videoService.TotalFrames - 1, _videoService.CurrentFrameIndex + (int)(_videoService.Fps * 2))); return true; }
                if (keyData == Keys.Left) { LoadFrame(Math.Max(0, _videoService.CurrentFrameIndex - (int)(_videoService.Fps * 5))); return true; }
                if (keyData == Keys.Right) { LoadFrame(Math.Min(_videoService.TotalFrames - 1, _videoService.CurrentFrameIndex + (int)(_videoService.Fps * 5))); return true; }
            }

            // DF-1-06 (D-12): Tab / Shift+Tab 를 WinForms 기본 버튼 네비게이션보다 먼저 가로챈다.
            //   - MainForm_KeyDown 의 Tab 분기는 ProcessDialogKey 가 Tab 을 먼저 소비해 도달 불가였다.
            //   - ProcessCmdKey 는 PreProcessMessage → ProcessDialogKey 이전에 호출되므로 여기서 true 를 반환하면
            //     포커스가 버튼/컨트롤 간 이동하는 기본 동작이 차단된다.
            //   - 영상 미로드 또는 BBOX 0 건 상황에서도 항상 true 를 반환하여 버튼 focus 이동을 막는다.
            if (keyData == Keys.Tab || keyData == (Keys.Shift | Keys.Tab))
            {
                if (isVideoLoaded && boundingBoxes.Count > 0)
                {
                    CycleSelection(reverse: (keyData & Keys.Shift) == Keys.Shift);
                }
                return true;  // 항상 true — WinForms 버튼 traversal 방지
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>NEW-04: ID 지정용 숫자 키(Ctrl+D0~D9, Ctrl+NumPad0~9)인지 판정</summary>
        private static bool IsIdAssignmentKey(Keys keyCode)
        {
            return (keyCode >= Keys.D0 && keyCode <= Keys.D9)
                || (keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            Control focusedControl = this.ActiveControl;
            if (focusedControl is TextBox || focusedControl is ComboBox)
            {
                // NEW-04: Ctrl+숫자(D0~D9, NumPad0~9) / Ctrl+N / Alt+숫자 는 ID 지정 단축키이므로
                // TextBox/ComboBox 포커스와 무관하게 통과시킨다 (QA [4] 결함).
                bool isIdShortcut =
                    (e.Control && (IsIdAssignmentKey(e.KeyCode) || e.KeyCode == Keys.N))
                    || (e.Alt && IsIdAssignmentKey(e.KeyCode));
                if (!isIdShortcut && e.KeyCode != Keys.Enter && e.KeyCode != Keys.Escape) return;
            }

            // F1/F2/F3: label selection
            if (!e.Control && !e.Shift && !e.Alt)
            {
                if (e.KeyCode == Keys.F1) { btnLabelPerson_Click(sender, e); e.Handled = true; return; }
                if (e.KeyCode == Keys.F2) { btnLabelVehicle_Click(sender, e); e.Handled = true; return; }
                if (e.KeyCode == Keys.F3) { btnLabelEvent_Click(sender, e); e.Handled = true; return; }
            }

            // Ctrl+1~0: ID assignment for person
            if (e.Control && !e.Shift && !e.Alt && currentSelectedLabel == "person")
            {
                int? assignedId = GetIdFromKey(e.KeyCode, 0);
                if (assignedId.HasValue)
                {
                    if (selectedBox != null && selectedBox.Label == "person")
                    {
                        ChangeBoxIdOnly(selectedBox, assignedId.Value);
                    }
                    else currentAssignedId = assignedId.Value;
                    e.Handled = true; return;
                }
            }

            // Alt+1~0: ID 11-20 for person
            if (!e.Control && !e.Shift && e.Alt && currentSelectedLabel == "person")
            {
                int? assignedId = GetIdFromKey(e.KeyCode, 10);
                if (assignedId.HasValue)
                {
                    if (selectedBox != null && selectedBox.Label == "person") ChangeBoxIdOnly(selectedBox, assignedId.Value);
                    else currentAssignedId = assignedId.Value;
                    e.Handled = true; return;
                }
            }

            // Ctrl+1~4: vehicle 클래스 선택 (car/motorcycle/e_scooter/bicycle) - selectedBox 우선
            if (e.Control && !e.Shift && !e.Alt && selectedBox != null && selectedBox.Label == "vehicle")
            {
                int? classId = GetIdFromKey(e.KeyCode, 0);
                if (classId.HasValue && classId.Value >= 1 && classId.Value <= 4)
                {
                    ChangeBoxIdOnly(selectedBox, classId.Value);
                    e.Handled = true; return;
                }
            }
            else if (e.Control && !e.Shift && !e.Alt && currentSelectedLabel == "vehicle" && selectedBox == null)
            {
                int? classId = GetIdFromKey(e.KeyCode, 0);
                if (classId.HasValue && classId.Value >= 1 && classId.Value <= 4)
                {
                    currentAssignedId = classId.Value;
                    e.Handled = true; return;
                }
            }

            // Ctrl+1~0: event 클래스 선택 (event_hazard..event_abnormal_behavior) - selectedBox 우선
            if (e.Control && !e.Shift && !e.Alt && selectedBox != null && selectedBox.Label == "event")
            {
                int? classId = GetIdFromKey(e.KeyCode, 0);
                if (classId.HasValue && classId.Value >= 1 && classId.Value <= 10)
                {
                    ChangeBoxIdOnly(selectedBox, classId.Value);
                    e.Handled = true; return;
                }
            }
            else if (e.Control && !e.Shift && !e.Alt && currentSelectedLabel == "event" && selectedBox == null)
            {
                int? classId = GetIdFromKey(e.KeyCode, 0);
                if (classId.HasValue && classId.Value >= 1 && classId.Value <= 10)
                {
                    currentAssignedId = classId.Value;
                    e.Handled = true; return;
                }
            }

            // NEW-03 (D-01): Ctrl+N — Exit 프레임 BBOX ID 를 Entry 프레임과 자동 매칭
            // MainForm.cs (기존) 의 오류 메시지에서 "Ctrl+N 또는 단축키로…" 문구로 안내되던 기능의 실구현.
            if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.N)
            {
                if (!_videoService.IsVideoLoaded) { e.Handled = true; return; }

                int currentFrame = _videoService.CurrentFrameIndex;

                // Entry 프레임 결정: (1) entryFrameIndex 필드 > (2) 현 프레임 포함 Waypoint 의 EntryFrame
                int? entryFrame = null;
                if (entryFrameIndex.HasValue) entryFrame = entryFrameIndex.Value;
                else
                {
                    var containing = waypointMarkers.FirstOrDefault(w =>
                        currentFrame >= w.EntryFrame && currentFrame <= w.ExitFrame);
                    if (containing != null) entryFrame = containing.EntryFrame;
                }

                if (!entryFrame.HasValue)
                {
                    MessageBox.Show(
                        "Entry 프레임이 지정되지 않아 Ctrl+N 을 수행할 수 없습니다.",
                        "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    e.Handled = true; return;
                }

                var entryBoxes = GetBboxesForFrame(entryFrame.Value).Where(b => !b.IsDeleted).ToList();
                var exitBoxes = GetBboxesForFrame(currentFrame).Where(b => !b.IsDeleted).ToList();

                int matched = 0;
                if (selectedBox != null && exitBoxes.Contains(selectedBox))
                {
                    // 선택된 박스만 매칭 — 같은 Label 첫 번째 Entry 박스 ID 사용
                    var entryMatch = entryBoxes.FirstOrDefault(b => b.Label == selectedBox.Label);
                    if (entryMatch != null && GetBoxId(entryMatch) != GetBoxId(selectedBox))
                    {
                        ChangeBoxIdOnly(selectedBox, GetBoxId(entryMatch));
                        matched++;
                    }
                }
                else
                {
                    // 현재 프레임의 모든 Exit 박스를 Label 기준 1:1 매칭 (Entry 동일 Label 첫 박스)
                    foreach (var exitBox in exitBoxes)
                    {
                        var entryMatch = entryBoxes.FirstOrDefault(b => b.Label == exitBox.Label);
                        if (entryMatch != null && GetBoxId(entryMatch) != GetBoxId(exitBox))
                        {
                            ChangeBoxIdOnly(exitBox, GetBoxId(entryMatch));
                            matched++;
                        }
                    }
                }

                if (matched == 0)
                    MessageBox.Show("일치시킬 ID 차이가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    pictureBoxVideo.Invalidate();

                e.Handled = true; return;
            }

            if (!_videoService.IsVideoLoaded) return;  // DF-1-13: 아래 Keys.E/X 등 영상 의존 브랜치 보호

            // DF-1-06 (D-12): Tab 처리는 ProcessCmdKey 에서 ProcessDialogKey 전에 가로채므로 여기서는 dead code.
            //   WinForms 기본 버튼 네비게이션이 Tab 을 먼저 먹기 때문에 KeyDown 까지 도달하지 않는다.
            //   (구 구현: `if (e.KeyCode == Keys.Tab) { CycleSelection(e.Shift); e.Handled = true; return; }`)
            if (e.KeyCode == Keys.Space) { btnPlay_Click(sender, e); e.Handled = true; }
            else if (e.KeyCode == Keys.C && !e.Control) { btnToggleSubtitle_Click(sender, e); e.Handled = true; }
            else if (e.Shift && e.KeyCode == Keys.OemPeriod)
            {
                if (playbackSpeed < 1.0) playbackSpeed = 1.0; else if (playbackSpeed < 4.0) playbackSpeed = 4.0; else if (playbackSpeed < 8.0) playbackSpeed = 8.0; else if (playbackSpeed < 16.0) playbackSpeed = 16.0;
                if (isPlaying) lastFrameTime = DateTime.Now.Ticks / 10000;
                UpdateTimeLabels(); e.Handled = true;
            }
            else if (e.Shift && e.KeyCode == Keys.Oemcomma)
            {
                if (playbackSpeed > 8.0) playbackSpeed = 8.0; else if (playbackSpeed > 4.0) playbackSpeed = 4.0; else if (playbackSpeed > 1.0) playbackSpeed = 1.0; else playbackSpeed = 0.5;
                if (isPlaying) lastFrameTime = DateTime.Now.Ticks / 10000;
                UpdateTimeLabels(); e.Handled = true;
            }
            else if (selectedBox != null && !e.Control && (e.KeyCode == Keys.W || e.KeyCode == Keys.A || e.KeyCode == Keys.S || e.KeyCode == Keys.D))
            {
                int moveAmount = e.Shift ? 10 : 2;
                // DF-1-07 (05.6-04 Task 5-B): 변경 전 rect 스냅샷 — 각 키 입력을 독립적인 Undo 단위로 푸시
                Rectangle originalRect = selectedBox.Rectangle;
                Rectangle rect = selectedBox.Rectangle;
                switch (e.KeyCode) { case Keys.W: rect.Y -= moveAmount; break; case Keys.A: rect.X -= moveAmount; break; case Keys.S: rect.Y += moveAmount; break; case Keys.D: rect.X += moveAmount; break; }
                // FUNC-03: 이미지 범위 클램핑
                if (pictureBoxVideo.Image != null)
                    rect = CoordinateHelper.ClampToImage(rect, pictureBoxVideo.Image.Width, pictureBoxVideo.Image.Height);
                // 실제 rect 가 변경된 경우에만 Undo 푸시 (클램핑으로 변화 없을 때 stack 오염 방지)
                if (rect != originalRect)
                {
                    selectedBox.Rectangle = rect;
                    PushModifyBoxUndo(selectedBox, originalRect, selectedBox.Label, GetBoxId(selectedBox));
                }
                pictureBoxVideo.Invalidate(); e.Handled = true;
            }
            else if ((e.KeyCode == Keys.Delete || e.KeyCode == Keys.G) && selectedBox != null)
            {
                var deletedSnapshot = CloneBoundingBox(selectedBox);  // DF-1-03: Waypoint 매칭용 스냅샷
                AddUndoAction(new UndoAction { Type = UndoActionType.RemoveBox, Box = deletedSnapshot });
                boundingBoxes.Remove(selectedBox);
                // DF-1-17 (D-17a): 사용자 주도 BBOX 삭제 감사 이벤트
                LogService.AuditBBoxDelete(deletedSnapshot.Label, GetBoxId(deletedSnapshot), deletedSnapshot.FrameIndex);
                selectedBox = null;
                InvalidateBoxCache(); UpdateBoxCount(); UpdateBboxListDisplay(); pictureBoxVideo.Invalidate();
                // DF-1-03 (D-07): Waypoint 구간 내 마지막 박스였는지 확인 후 동반 삭제 프롬프트
                PromptWaypointDeletionIfEmpty(deletedSnapshot);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && selectedBox == null)
            {
                if (listViewPersonWaypoints.SelectedItems.Count > 0 || listViewVehicleWaypoints.SelectedItems.Count > 0 || listViewEventWaypoints.SelectedItems.Count > 0)
                { btnDeleteSelectedWaypoint_Click(sender, e); e.Handled = true; }
            }
            else if (e.Control && e.KeyCode == Keys.Z) { if (e.Shift) Redo(); else Undo(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.Y) { Redo(); e.Handled = true; }
            else if (e.KeyCode == Keys.E && !e.Control && !e.Alt) { SetEntryMarker(); e.Handled = true; }
            else if (e.KeyCode == Keys.X && !e.Control && !e.Alt) { _ = SetExitMarkerAndCreateWaypoint(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.S) { btnExportJson_Click(sender, e); e.Handled = true; }
            else if (e.KeyCode == Keys.D1 && !e.Control && !e.Alt) { btnSelectAll_Click(sender, e); e.Handled = true; }
            else if (e.KeyCode == Keys.D2 && !e.Control && !e.Alt) { btnEdit_Click(sender, e); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape)
            {
                if (entryFrameIndex.HasValue) { entryFrameIndex = null; btnEntry.Text = "Entry"; panelTimeline.Invalidate(); }
                selectedBox = null; ClearSidebarHighlights(); pictureBoxVideo.Invalidate(); e.Handled = true;
            }
            else if (e.KeyCode == Keys.Oemcomma && !e.Shift)
            {
                if (_videoService.CurrentFrameIndex > 0) LoadFrame(_videoService.CurrentFrameIndex - 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.OemPeriod && !e.Shift)
            {
                if (_videoService.CurrentFrameIndex < _videoService.TotalFrames - 1) LoadFrame(_videoService.CurrentFrameIndex + 1);
                e.Handled = true;
            }
        }

        private int? GetIdFromKey(Keys keyCode, int offset)
        {
            if (keyCode == Keys.D1 || keyCode == Keys.NumPad1) return 1 + offset;
            if (keyCode == Keys.D2 || keyCode == Keys.NumPad2) return 2 + offset;
            if (keyCode == Keys.D3 || keyCode == Keys.NumPad3) return 3 + offset;
            if (keyCode == Keys.D4 || keyCode == Keys.NumPad4) return 4 + offset;
            if (keyCode == Keys.D5 || keyCode == Keys.NumPad5) return 5 + offset;
            if (keyCode == Keys.D6 || keyCode == Keys.NumPad6) return 6 + offset;
            if (keyCode == Keys.D7 || keyCode == Keys.NumPad7) return 7 + offset;
            if (keyCode == Keys.D8 || keyCode == Keys.NumPad8) return 8 + offset;
            if (keyCode == Keys.D9 || keyCode == Keys.NumPad9) return 9 + offset;
            if (keyCode == Keys.D0 || keyCode == Keys.NumPad0) return 10 + offset;
            return null;
        }

        /// <summary>
        /// NEW-01 (D-06): 새 BBOX 생성 시 사용할 ID 결정.
        /// 우선순위 (1) 현 프레임이 포함된 Waypoint 의 Entry 프레임에 같은 Label 박스가 있으면 그 ID,
        /// (2) entryFrameIndex 필드가 있고 현 프레임이 Entry 이후면 Entry 동일 Label 박스 ID,
        /// (3) 현 프레임의 같은 Label selectedBox ID,
        /// (4) currentAssignedId (사용자 Ctrl+숫자 지정값),
        /// (5) 기본값 1.
        /// 목적: Exit 프레임에서 새 BBOX 를 그릴 때 Entry 프레임 ID 를 자동 승계 → Entry-Exit 일관성 유지.
        /// </summary>
        private int ResolveIdForNewBox(string label, int currentFrameIndex)
        {
            // (1) 현 프레임을 포함하는 Waypoint 의 Entry 박스 ID 승계
            var containingWaypoint = waypointMarkers.FirstOrDefault(w =>
                w.Label == label
                && currentFrameIndex >= w.EntryFrame
                && currentFrameIndex <= w.ExitFrame);
            if (containingWaypoint != null)
            {
                var entryBox = GetBboxesForFrame(containingWaypoint.EntryFrame)
                    .FirstOrDefault(b => b.Label == label && !b.IsDeleted);
                if (entryBox != null) return GetBoxId(entryBox);
            }

            // (2) entryFrameIndex 가 지정되어 있고 현 프레임이 Entry 이후면 Entry 동일 Label ID 승계
            if (entryFrameIndex.HasValue && currentFrameIndex >= entryFrameIndex.Value)
            {
                var entryBox = GetBboxesForFrame(entryFrameIndex.Value)
                    .FirstOrDefault(b => b.Label == label && !b.IsDeleted);
                if (entryBox != null) return GetBoxId(entryBox);
            }

            // (3) 현 프레임의 같은 Label selectedBox ID 승계
            if (selectedBox != null && selectedBox.Label == label && selectedBox.FrameIndex == currentFrameIndex)
                return GetBoxId(selectedBox);

            // (4) 사용자 Ctrl+숫자 지정값
            if (currentAssignedId >= 1) return currentAssignedId;

            // (5) 기본값
            return 1;
        }

        /// <summary>
        /// D-02 (NEW-05/07): 선택된 개별 BBOX 하나만 ID 를 변경한다.
        /// 같은 Waypoint 내 같은 Label+oldId 의 다른 박스로 전파되지 않으며 Waypoint.ObjectId 도 자동 변경되지 않는다
        /// (Waypoint 단위 일괄 변경은 사용자가 Waypoint 전체를 삭제 후 재생성해야 한다 — 의도적 보수성).
        /// Undo 스택에 등록되어 Ctrl+Z 로 복원 가능.
        ///
        /// D-21 (NEW-08 진단): Ctrl+{digit} 으로 ID 변경 시 Rectangle 이 "처음 설정한 박스 좌표" 로
        /// 고정된다는 사용자 보고(2026-04-24 SC-02 UAT). 코드 감사 결과 ChangeBoxIdOnly/SetBoxId/
        /// CloneBoundingBox 어느 경로에도 Rectangle 재설정이 없음을 확인. 재현 불가로 진단을 위해
        /// Rectangle 의 entry/exit 값과 identity 를 Debug.WriteLine 으로 로깅 (릴리스 빌드에서는 비활성).
        /// 원인 확정 시 D-21 에 실측 값 기입 후 로그 제거.
        /// </summary>
        private void ChangeBoxIdOnly(BoundingBox box, int newId)
        {
            if (box == null) return;
            int oldId = GetBoxId(box);
            if (oldId == newId) return;

            // 변경 전 상태 스냅샷 (Undo 복원용)
            var origRect = box.Rectangle;
            var origLabel = box.Label;

            // D-21 (NEW-08 진단): Rectangle entry 스냅샷 — 예상 동작은 origRect == 호출 시점의 Rect.
            System.Diagnostics.Debug.WriteLine(
                $"[NEW-08] ChangeBoxIdOnly entry: " +
                $"hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(box)} " +
                $"label={origLabel} oldId={oldId} newId={newId} " +
                $"frame={box.FrameIndex} rect={origRect}");

            SetBoxId(box, box.Label, newId);

            // Undo 등록 — action.Box 는 변경 후 상태여야 Undo 핸들러가 GetBoxId(action.Box) == newId 로 boxToModify 조회 성공함
            // Original* 필드는 복원 대상 값을 담는다 (MainForm.cs:2241-2243 Undo 핸들러 계약)
            AddUndoAction(new UndoAction
            {
                Type = UndoActionType.ModifyBox,
                Box = CloneBoundingBox(box),
                OriginalRectangle = origRect,
                OriginalLabel = origLabel,
                OriginalObjectId = oldId,
            });

            // NEW-05 수정: Waypoint.ObjectId 는 자동 변경하지 않음 (선택된 박스만 변경)
            UpdateObjectInfo(box);
            UpdateBboxListDisplay();
            pictureBoxVideo.Invalidate();

            // D-21 (NEW-08 진단): Rectangle exit 스냅샷 — 예상 동작은 origRect == 현재 box.Rectangle.
            // 만약 rect 가 바뀌었다면 ChangeBoxIdOnly 내부 또는 UpdateBboxListDisplay 경로에서 Rectangle 재설정 발생.
            System.Diagnostics.Debug.WriteLine(
                $"[NEW-08] ChangeBoxIdOnly exit:  " +
                $"hash={System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(box)} " +
                $"rect={box.Rectangle} " +
                (box.Rectangle == origRect ? "(Rectangle 보존됨)" : "(!!! Rectangle 변경됨)"));
        }

        #endregion

        #region Label Buttons

        private void btnLabelPerson_Click(object sender, EventArgs e)
        {
            currentSelectedLabel = "person";
            currentAssignedId = 1;
            btnLabelPerson.FlatAppearance.BorderSize = 2;
            btnLabelVehicle.FlatAppearance.BorderSize = 1;
            btnLabelEvent.FlatAppearance.BorderSize = 1;
            if (selectedBox != null && selectedBox.Label != "person")
                ChangeBoxLabel(selectedBox, "person");
            UpdateBboxListDisplay();
        }

        private void btnLabelVehicle_Click(object sender, EventArgs e)
        {
            currentSelectedLabel = "vehicle";
            currentAssignedId = 1;
            btnLabelPerson.FlatAppearance.BorderSize = 1;
            btnLabelVehicle.FlatAppearance.BorderSize = 2;
            btnLabelEvent.FlatAppearance.BorderSize = 1;
            if (selectedBox != null && selectedBox.Label != "vehicle")
                ChangeBoxLabel(selectedBox, "vehicle");
            UpdateBboxListDisplay();
        }

        private void btnLabelEvent_Click(object sender, EventArgs e)
        {
            currentSelectedLabel = "event";
            currentAssignedId = 1;
            btnLabelPerson.FlatAppearance.BorderSize = 1;
            btnLabelVehicle.FlatAppearance.BorderSize = 1;
            btnLabelEvent.FlatAppearance.BorderSize = 2;
            if (selectedBox != null && selectedBox.Label != "event")
                ChangeBoxLabel(selectedBox, "event");
            UpdateBboxListDisplay();
        }

        // 선택된 박스의 레이블 타입 변경 (person/vehicle/event)
        private void ChangeBoxLabel(BoundingBox box, string newLabel)
        {
            box.Label = newLabel;
            box.PersonId = newLabel == "person" ? currentAssignedId : 0;
            box.VehicleId = newLabel == "vehicle" ? 1 : 0;
            box.EventId = newLabel == "event" ? 1 : 0;
            InvalidateBoxCache();
            UpdateObjectInfo(box);
            pictureBoxVideo.Invalidate();
        }

        private void TogglePersonPanel(object sender, EventArgs e)
        {
            panelPersonList.Visible = !panelPersonList.Visible;
            labelPersonList.Text = panelPersonList.Visible ? "v person" : "> person";
            RecalcLabelsPanelLayout();
        }

        private void ToggleVehiclePanel(object sender, EventArgs e)
        {
            panelVehicleList.Visible = !panelVehicleList.Visible;
            labelVehicleList.Text = panelVehicleList.Visible ? "v vehicle" : "> vehicle";
            RecalcLabelsPanelLayout();
        }

        private void ToggleEventPanel(object sender, EventArgs e)
        {
            panelEventList.Visible = !panelEventList.Visible;
            labelEventList.Text = panelEventList.Visible ? "v event" : "> event";
            RecalcLabelsPanelLayout();
        }

        private void RecalcLabelsPanelLayout()
        {
            int y = 70;
            labelPersonList.Location = new System.Drawing.Point(8, y); y += 30;
            if (panelPersonList.Visible) { panelPersonList.Location = new System.Drawing.Point(8, y); y += panelPersonList.Height + 5; }

            labelVehicleList.Location = new System.Drawing.Point(8, y); y += 30;
            if (panelVehicleList.Visible) { panelVehicleList.Location = new System.Drawing.Point(8, y); y += panelVehicleList.Height + 5; }

            labelEventList.Location = new System.Drawing.Point(8, y); y += 30;
            if (panelEventList.Visible) { panelEventList.Location = new System.Drawing.Point(8, y); y += panelEventList.Height + 5; }

            btnDeleteLabel.Location = new System.Drawing.Point(8, y); y += 40;
            btnExportJsonInLabels.Location = new System.Drawing.Point(8, y); y += 40;
            groupBoxLabels.Height = y + 10;
        }

        private void btnDeleteLabel_Click(object sender, EventArgs e)
        {
            if (selectedBox == null) { MessageBox.Show("삭제할 박스를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var deletedSnapshot = CloneBoundingBox(selectedBox);  // DF-1-03: Waypoint 매칭용 스냅샷
            AddUndoAction(new UndoAction { Type = UndoActionType.RemoveBox, Box = deletedSnapshot });
            boundingBoxes.Remove(selectedBox);
            // DF-1-17 (D-17a): 사용자 주도 BBOX 삭제 감사 이벤트 (사이드바 경로)
            LogService.AuditBBoxDelete(deletedSnapshot.Label, GetBoxId(deletedSnapshot), deletedSnapshot.FrameIndex);
            selectedBox = null;
            InvalidateBoxCache(); UpdateBoxCount(); UpdateBboxListDisplay(); pictureBoxVideo.Invalidate();
            // DF-1-03 (D-07): 사이드바 삭제도 Delete 키와 동일한 Waypoint 동반 삭제 프롬프트
            PromptWaypointDeletionIfEmpty(deletedSnapshot);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// DF-1-03 (D-07): BBOX 삭제 후 해당 Waypoint 구간이 비어있는지 체크하고,
        /// 비어있으면 동반 삭제 프롬프트를 표시한다.
        /// 사용자 주도의 개별 삭제 경로(Delete/G 키, 사이드바 삭제 버튼)에서만 호출한다.
        /// Undo/Redo 경로, Waypoint 자체 삭제 경로, 프로그램 정리 경로에서는 호출하지 않는다.
        /// </summary>
        /// <param name="deletedBox">방금 삭제된 박스 (Waypoint 매칭 기준 — CloneBoundingBox 스냅샷 권장)</param>
        private void PromptWaypointDeletionIfEmpty(BoundingBox deletedBox)
        {
            if (deletedBox == null) return;

            // 삭제된 박스가 속했던 Waypoint 탐색 (같은 Label + ObjectId + Frame 범위)
            int deletedId = GetBoxId(deletedBox);
            var waypoint = waypointMarkers.FirstOrDefault(w =>
                w.Label == deletedBox.Label
                && w.ObjectId == deletedId
                && deletedBox.FrameIndex >= w.EntryFrame
                && deletedBox.FrameIndex <= w.ExitFrame);

            if (waypoint == null) return;

            // Waypoint 구간 내 같은 Label+ObjectId 박스가 아직 남아있는지
            bool hasRemaining = boundingBoxes.Any(b =>
                !b.IsDeleted
                && b.Label == waypoint.Label
                && GetBoxId(b) == waypoint.ObjectId
                && b.FrameIndex >= waypoint.EntryFrame
                && b.FrameIndex <= waypoint.ExitFrame);

            if (hasRemaining) return;

            var result = MessageBox.Show(
                "Waypoint 구간 BBOX가 전부 삭제됩니다. 해당 Waypoint도 함께 삭제하시겠습니까?",
                "Waypoint 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // DF-1-19: 직전에 push 된 RemoveBox 액션을 회수하여 composite 으로 교체.
                // 단일 Ctrl+Z 로 박스 + Waypoint 동시 복원, 더블-restore 방지.
                var capturedBoxes = new List<BoundingBox>();
                if (undoStack.Count > 0)
                {
                    var top = undoStack.Peek();
                    if (top.Type == UndoActionType.RemoveBox && top.Box != null
                        && top.Box.FrameIndex == deletedBox.FrameIndex
                        && top.Box.Label == deletedBox.Label
                        && GetBoxId(top.Box) == GetBoxId(deletedBox))
                    {
                        var popped = undoStack.Pop();
                        capturedBoxes.Add(popped.Box);
                        // redoStack 정합성 — pop 한 액션이 다시 redo 되는 경로 차단
                        redoStack.Clear();
                    }
                }
                AddUndoAction(new UndoAction
                {
                    Type = UndoActionType.RemoveWaypointWithBoxes,
                    AffectedBoxes = capturedBoxes,
                    AffectedWaypoint = waypoint,
                });

                waypointMarkers.Remove(waypoint);
                if (selectedWaypoint == waypoint) selectedWaypoint = null;
                // DF-1-17 (D-17a): Waypoint 삭제 감사 이벤트 (BBOX 동반 삭제 프롬프트 Yes 경로)
                LogService.AuditWaypointDelete(waypoint.Label, waypoint.ObjectId, waypoint.EntryFrame, waypoint.ExitFrame);
                UpdateWaypointListView();
                panelTimeline.Invalidate();
            }
            // No 선택 시 Waypoint 유지 (빈 Waypoint — 사용자가 나중에 BBOX 를 다시 추가할 수 있음)
        }

        private int GetBoxId(BoundingBox box)
        {
            if (box.Label == "person") return box.PersonId;
            if (box.Label == "vehicle") return box.VehicleId;
            if (box.Label == "event") return box.EventId;
            return 0;
        }

        private void SetBoxId(BoundingBox box, string label, int id)
        {
            box.PersonId = label == "person" ? id : 0;
            box.VehicleId = label == "vehicle" ? id : 0;
            box.EventId = label == "event" ? id : 0;
        }

        private string GetCategoryName(string label, int boxId)
        {
            if (label == "person") return $"person_{boxId:D2}";
            if (label == "vehicle") return boxId switch { 1 => "car", 2 => "motorcycle", 3 => "e_scooter", 4 => "bicycle", _ => "car" };
            if (label == "event") return boxId switch { 1 => "event_hazard", 2 => "event_accident", 3 => "event_damage", 4 => "event_fire", 5 => "event_intrusion", 6 => "event_leak", 7 => "event_failure", 8 => "event_lost_object", 9 => "event_fall", 10 => "event_abnormal_behavior", _ => "event_hazard" };
            return $"{label}_{boxId:D2}";
        }

        private Color GetColorForLabel(string label)
        {
            return (label ?? "").ToLower() switch
            {
                "person" => DarkTheme.PersonColor,
                "vehicle" => DarkTheme.VehicleColor,
                "event" => DarkTheme.EventColor,
                _ => Color.Gray
            };
        }

        private string GetBoxLabelText(BoundingBox box)
        {
            int boxId = GetBoxId(box);
            return GetCategoryName(box.Label, boxId);
        }

        private BoundingBox CloneBoundingBox(BoundingBox box)
        {
            return new BoundingBox
            {
                FrameIndex = box.FrameIndex,
                Rectangle = new Rectangle(box.Rectangle.Location, box.Rectangle.Size),
                Label = box.Label, PersonId = box.PersonId, VehicleId = box.VehicleId,
                EventId = box.EventId, Action = box.Action,
                VehicleName = box.VehicleName, EventName = box.EventName
            };
        }

        private void UpdateBoxCount()
        {
            int count = boundingBoxes.Count(b => !b.IsDeleted);
            labelBoxCount.Text = $"박스: {count}";
        }

        private void InvalidateBoxCache()
        {
            lastCachedFrameForPaint = -1;
            _bboxByFrame = null;
        }

        private void RebuildBboxIndex()
        {
            _bboxByFrame = new Dictionary<int, List<BoundingBox>>();
            foreach (var box in boundingBoxes)
            {
                if (!_bboxByFrame.TryGetValue(box.FrameIndex, out var list))
                {
                    list = new List<BoundingBox>();
                    _bboxByFrame[box.FrameIndex] = list;
                }
                list.Add(box);
            }
        }

        private List<BoundingBox> GetBboxesForFrame(int frameIndex)
        {
            if (_bboxByFrame == null)
                RebuildBboxIndex();
            return _bboxByFrame.TryGetValue(frameIndex, out var list) ? list : new List<BoundingBox>();
        }

        private WaypointMarker FindWaypointForBox(BoundingBox box)
        {
            int boxId = GetBoxId(box);
            return waypointMarkers.FirstOrDefault(w =>
                w.Label == box.Label && w.ObjectId == boxId &&
                box.FrameIndex >= w.EntryFrame && box.FrameIndex <= w.ExitFrame);
        }

        private void SelectWaypointInListView(WaypointMarker waypoint)
        {
            ListView targetListView = null;
            if (waypoint.Label == "person") targetListView = listViewPersonWaypoints;
            else if (waypoint.Label == "vehicle") targetListView = listViewVehicleWaypoints;
            else if (waypoint.Label == "event") targetListView = listViewEventWaypoints;

            if (targetListView == null) return;

            foreach (ListViewItem item in targetListView.Items)
            {
                if (item.Tag is WaypointMarker marker &&
                    marker.Label == waypoint.Label &&
                    marker.ObjectId == waypoint.ObjectId &&
                    marker.EntryFrame == waypoint.EntryFrame)
                {
                    suppressWaypointClickOnce = true;
                    targetListView.SelectedItems.Clear();
                    item.Selected = true;
                    item.EnsureVisible();
                    targetListView.Focus();
                    return;
                }
            }
        }

        private void UpdateObjectInfo(BoundingBox box)
        {
            if (box == null)
            {
                labelObjectLabel.Text = "Label: -";
                return;
            }
            int boxId = GetBoxId(box);
            labelObjectLabel.Text = $"Label: {GetCategoryName(box.Label, boxId)}\nFrame: {box.FrameIndex}\nSize: {box.Rectangle.Width}x{box.Rectangle.Height}";
        }

        private void UpdateBboxListDisplay()
        {
            // Simplified: update sidebar list panels
            int currentFrameIndex = _videoService.IsVideoLoaded ? _videoService.CurrentFrameIndex : 0;
            var frameBoxes = GetBboxesForFrame(currentFrameIndex).Where(b => !b.IsDeleted).ToList();

            UpdateBboxPanel(panelPersonList, frameBoxes.Where(b => b.Label == "person").ToList());
            UpdateBboxPanel(panelVehicleList, frameBoxes.Where(b => b.Label == "vehicle").ToList());
            UpdateBboxPanel(panelEventList, frameBoxes.Where(b => b.Label == "event").ToList());
        }

        private void UpdateBboxPanel(Panel panel, List<BoundingBox> boxes)
        {
            panel.Controls.Clear();
            int y = 2;
            foreach (var box in boxes)
            {
                BoundingBox capturedBox = box;
                int boxId = GetBoxId(capturedBox);
                string text = GetCategoryName(capturedBox.Label, boxId);
                var itemPanel = new Panel
                {
                    Location = new System.Drawing.Point(2, y),
                    Size = new System.Drawing.Size(panel.Width - 20, 25),
                    BackColor = DarkTheme.Panel,
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = capturedBox,
                    Cursor = Cursors.Hand
                };

                if (capturedBox.Label == "vehicle" || capturedBox.Label == "event")
                {
                    // vehicle/event: ComboBox로 클래스 직접 변경 가능
                    string[] options = capturedBox.Label == "vehicle"
                        ? new[] { "car", "motorcycle", "e_scooter", "bicycle" }
                        : new[] { "event_hazard", "event_accident", "event_damage", "event_fire", "event_intrusion", "event_leak", "event_failure", "event_lost_object", "event_fall", "event_abnormal_behavior" };
                    var combo = new ComboBox
                    {
                        Dock = DockStyle.Fill,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        BackColor = DarkTheme.Panel,
                        ForeColor = DarkTheme.TextPrimary,
                        Font = new Font("Segoe UI", 8F),
                        FlatStyle = FlatStyle.Flat,
                        TabStop = false
                    };
                    foreach (var opt in options) combo.Items.Add(opt);
                    int selIdx = Array.IndexOf(options, text);
                    combo.SelectedIndex = selIdx >= 0 ? selIdx : 0;
                    // 이벤트는 초기값 설정 후에 등록해야 spurious 발화 방지
                    combo.Click += (s, ev) =>
                    {
                        selectedBox = capturedBox;
                        UpdateObjectInfo(capturedBox);
                        HighlightSelectedBoxInSidebar();
                        pictureBoxVideo.Invalidate();
                    };
                    combo.SelectedIndexChanged += (s, ev) =>
                    {
                        int newClassId = combo.SelectedIndex + 1;
                        if (newClassId != GetBoxId(capturedBox))
                        {
                            selectedBox = capturedBox;
                            ChangeBoxIdOnly(capturedBox, newClassId);
                        }
                    };
                    itemPanel.Controls.Add(combo);
                }
                else
                {
                    // NEW-06 (D-05): Person 분기도 Vehicle/Event 처럼 ID 변경 UI 제공
                    // Person ID 는 1~20 (Ctrl+1~0 + Alt+1~0) → NumericUpDown(Min=1, Max=20)
                    int currentId = GetBoxId(capturedBox);

                    var label = new Label
                    {
                        Text = text, AutoSize = false, Dock = DockStyle.Fill,
                        ForeColor = DarkTheme.TextPrimary,
                        Font = new Font("Segoe UI", 8F),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(5, 0, 0, 0)
                    };

                    var numericId = new NumericUpDown
                    {
                        Minimum = 1,
                        Maximum = 20,
                        Value = Math.Max(1, Math.Min(20, currentId)),
                        Width = 50,
                        Dock = DockStyle.Right,
                        BackColor = DarkTheme.Panel,
                        ForeColor = DarkTheme.TextPrimary,
                        BorderStyle = BorderStyle.None,
                        TabStop = false,
                        Font = new Font("Segoe UI", 8F)
                    };
                    numericId.ValueChanged += (s, ev) =>
                    {
                        int newId = (int)numericId.Value;
                        if (newId != GetBoxId(capturedBox))
                        {
                            selectedBox = capturedBox;
                            ChangeBoxIdOnly(capturedBox, newId);
                        }
                    };

                    label.Click += (s, ev) => { selectedBox = capturedBox; UpdateObjectInfo(capturedBox); HighlightSelectedBoxInSidebar(); pictureBoxVideo.Invalidate(); };
                    itemPanel.Click += (s, ev) => { selectedBox = capturedBox; UpdateObjectInfo(capturedBox); HighlightSelectedBoxInSidebar(); pictureBoxVideo.Invalidate(); };

                    // WinForms 도킹: DockStyle.Right 를 먼저 추가해야 Fill 이 나머지 영역만 점유
                    itemPanel.Controls.Add(numericId);
                    itemPanel.Controls.Add(label);
                }

                panel.Controls.Add(itemPanel);
                y += 27;
            }
        }

        private void HighlightSelectedBoxInSidebar()
        {
            if (selectedBox == null) return;
            ClearSidebarHighlights();
            Panel targetPanel = selectedBox.Label switch { "person" => panelPersonList, "vehicle" => panelVehicleList, "event" => panelEventList, _ => null };
            if (targetPanel == null) return;

            foreach (Control ctrl in targetPanel.Controls)
            {
                if (ctrl is Panel itemPanel && itemPanel.Tag is BoundingBox box && box == selectedBox)
                {
                    itemPanel.BackColor = Color.FromArgb(0, 120, 212);
                }
            }
        }

        private void ClearSidebarHighlights()
        {
            ClearPanelHighlights(panelPersonList);
            ClearPanelHighlights(panelVehicleList);
            ClearPanelHighlights(panelEventList);
        }

        private void ClearPanelHighlights(Panel panel)
        {
            foreach (Control ctrl in panel.Controls)
            {
                if (ctrl is Panel itemPanel) itemPanel.BackColor = DarkTheme.Panel;
            }
        }

        #endregion

        #region Window Drag & Resize

        private void SetupWindowDragHandlers()
        {
            panelHeader.MouseDown += PanelHeader_MouseDown;
            panelHeader.MouseMove += PanelHeader_MouseMove;
            panelHeader.MouseUp += PanelHeader_MouseUp;
            panelHeader.DoubleClick += PanelHeader_DoubleClick;

            labelTitle.MouseDown += PanelHeader_MouseDown;
            labelTitle.MouseMove += PanelHeader_MouseMove;
            labelTitle.MouseUp += PanelHeader_MouseUp;
            labelTitle.DoubleClick += PanelHeader_DoubleClick;
        }

        private void PanelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized)
            {
                isMovingWindow = true;
                windowMoveStartPoint = e.Location;
            }
        }

        private void PanelHeader_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMovingWindow)
            {
                var currentScreenPoint = Control.MousePosition;
                this.Location = new System.Drawing.Point(currentScreenPoint.X - windowMoveStartPoint.X, currentScreenPoint.Y - windowMoveStartPoint.Y);
            }
        }

        private void PanelHeader_MouseUp(object sender, MouseEventArgs e) => isMovingWindow = false;

        private void PanelHeader_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

            if (m.Msg == WM_NCHITTEST && this.WindowState != FormWindowState.Maximized)
            {
                base.WndProc(ref m);
                var pos = this.PointToClient(new System.Drawing.Point(m.LParam.ToInt32()));
                int w = this.ClientSize.Width, h = this.ClientSize.Height;

                if (pos.X <= RESIZE_BORDER_WIDTH && pos.Y <= RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTTOPLEFT;
                else if (pos.X >= w - RESIZE_BORDER_WIDTH && pos.Y <= RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTTOPRIGHT;
                else if (pos.X <= RESIZE_BORDER_WIDTH && pos.Y >= h - RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (pos.X >= w - RESIZE_BORDER_WIDTH && pos.Y >= h - RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (pos.X <= RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTLEFT;
                else if (pos.X >= w - RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTRIGHT;
                else if (pos.Y <= RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTTOP;
                else if (pos.Y >= h - RESIZE_BORDER_WIDTH) m.Result = (IntPtr)HTBOTTOM;
            }
            else base.WndProc(ref m);
        }

        #endregion

        #region Form Closing

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDirty && _videoService != null && _videoService.IsVideoLoaded)
            {
                var result = MessageBox.Show(
                    "저장하지 않은 변경사항이 있습니다. 저장하시겠습니까?",
                    "저장 확인",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                else if (result == DialogResult.Yes)
                {
                    SaveCurrentLabelingData();
                }
                else // DialogResult.No — DF-1-05 (D-09): 앱 종료 시에도 자동 저장 JSON 롤백 삭제 (비디오 전환과 동일 UX)
                {
                    if (!string.IsNullOrEmpty(_autoSavedJsonPath) && File.Exists(_autoSavedJsonPath))
                    {
                        try
                        {
                            File.Delete(_autoSavedJsonPath);
                            Log.Information("[AUDIT] 앱 종료 시 미저장 JSON 롤백 삭제: {Path}", _autoSavedJsonPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "[JSON 롤백 삭제 오류] {Path}", _autoSavedJsonPath);
                        }
                    }
                    _autoSavedJsonPath = null;
                }
            }
            base.OnFormClosing(e);
            doubleClickTimer?.Dispose();
            doubleClickTimer = null;
            _videoLoadCts?.Cancel();
            _videoLoadCts?.Dispose();
            labelFont?.Dispose();
            _videoService?.Dispose();
        }

        #endregion
    }
}
