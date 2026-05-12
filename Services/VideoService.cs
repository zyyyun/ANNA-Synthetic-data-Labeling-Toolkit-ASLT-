using System.Diagnostics;
using Serilog;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using FFMpegCore;
using FFMpegCore.Enums;
using ASLTv1.Models;
using ASLTv1.Helpers;

namespace ASLTv1.Services
{
    /// <summary>
    /// Manages video capture lifecycle, frame loading, playback, and SRT subtitle extraction.
    /// </summary>
    public class VideoService : IDisposable
    {
        #region Fields

        private VideoCapture? videoCapture;
        private Mat? currentFrame;
        private string currentVideoFile = "";
        private int currentFrameIndex;
        private int totalFrames;
        private double fps;
        private bool isPlaying;
        private double playbackSpeed = 1.0;

        // DF-1-21: 일부 AVI 코덱은 Set(PosFrames, ...) seek 가 작동하지 않거나 후속 Read 가 empty 반환.
        // VideoCapture 가 새로 열린 직후엔 자연 위치 0 이므로 seek 없이 Read 가능 — 첫 frame 호출에서 seek 생략.
        private bool _isFreshlyOpened;

        // Playback timing
        private long lastFrameTime;
        private double msPerFrame;

        // SRT subtitle
        private List<SubtitleEntry> subtitleEntries = new();
        private bool isSubtitleVisible;
        private string currentSrtFile = "";
        private bool isFFmpegAvailable;

        private bool disposed;

        #endregion

        #region Properties

        public int CurrentFrameIndex => currentFrameIndex;
        public int TotalFrames => totalFrames;
        public double Fps => fps;
        public string CurrentVideoFile => currentVideoFile;
        public bool IsPlaying => isPlaying;
        public Mat? CurrentFrame => currentFrame;

        public List<SubtitleEntry> SubtitleEntries => subtitleEntries;

        public bool IsSubtitleVisible
        {
            get => isSubtitleVisible;
            set => isSubtitleVisible = value;
        }

        public string CurrentSrtFile => currentSrtFile;

        public double PlaybackSpeed
        {
            get => playbackSpeed;
            set => playbackSpeed = value;
        }

        public bool IsVideoLoaded => videoCapture != null && videoCapture.IsOpened();

        public bool IsFFmpegAvailable => isFFmpegAvailable;

        public int FrameWidth => videoCapture != null && videoCapture.IsOpened()
            ? (int)videoCapture.Get(VideoCaptureProperties.FrameWidth) : 0;

        public int FrameHeight => videoCapture != null && videoCapture.IsOpened()
            ? (int)videoCapture.Get(VideoCaptureProperties.FrameHeight) : 0;

        #endregion

        #region Events

        /// <summary>Raised when a new frame has been loaded.</summary>
        public event EventHandler<int>? FrameChanged;

        /// <summary>Raised when playback starts or stops. Arg = isPlaying.</summary>
        public event EventHandler<bool>? PlayStateChanged;

        /// <summary>Raised after a video file has been successfully opened.</summary>
        public event EventHandler<string>? VideoLoaded;

        #endregion

        #region Video Loading

        /// <summary>
        /// Opens a video file and reads initial metadata.
        /// Also attempts to load an associated SRT subtitle (external file or embedded).
        /// </summary>
        public async Task LoadVideoAsync(string filePath, CancellationToken cancellationToken = default)
        {
            // Release previous capture
            if (videoCapture != null)
            {
                videoCapture.Release();
                videoCapture.Dispose();
                videoCapture = null;
            }

            try
            {
                videoCapture = new VideoCapture(filePath);
            }
            catch (TypeInitializationException tiex)
            {
                string errorMsg = "OpenCvSharp 네이티브 DLL 초기화 실패:\n\n" +
                                $"{tiex.Message}\n\n" +
                                "가능한 원인:\n" +
                                "1. Visual C++ 재배포 가능 패키지가 설치되지 않았습니다.\n" +
                                "   (Microsoft Visual C++ 2015-2022 Redistributable 설치 필요)\n" +
                                "2. OpenCvSharpExtern.dll 또는 관련 DLL이 누락되었습니다.\n" +
                                "3. 플랫폼 아키텍처 불일치 (x64 필요)";
                if (tiex.InnerException != null)
                    errorMsg += $"\n\n내부 예외: {tiex.InnerException.Message}";
                throw new InvalidOperationException(errorMsg, tiex);
            }
            catch (DllNotFoundException dllEx)
            {
                string errorMsg = "필수 DLL을 찾을 수 없습니다:\n\n" +
                                $"{dllEx.Message}\n\n" +
                                "OpenCvSharpExtern.dll 또는 opencv_videoio_ffmpeg4110_64.dll이\n" +
                                "실행 파일과 같은 폴더에 있는지 확인하세요.";
                throw new InvalidOperationException(errorMsg, dllEx);
            }

            if (!videoCapture.IsOpened())
            {
                // DF-1-14 (D-15): 영어 메시지 → 한국어 통일
                throw new InvalidOperationException(
                    "비디오 파일을 열 수 없습니다.\n\n" +
                    "원인:\n" +
                    "1. 파일이 손상되었거나 지원하지 않는 확장자입니다.\n" +
                    "2. 코덱을 지원하지 않습니다.\n\n" +
                    "해결 방법: MP4(H.264) 등 지원 형식의 파일인지 확인하세요.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            currentVideoFile = filePath;
            totalFrames = (int)videoCapture.Get(VideoCaptureProperties.FrameCount);
            fps = videoCapture.Get(VideoCaptureProperties.Fps);
            currentFrameIndex = 0;
            _isFreshlyOpened = true;  // DF-1-21: 다음 LoadFrame 호출에서 seek 생략 가능 표시

            // RELI-06 (05.5-02): 첫 프레임 로드는 MainForm.LoadVideoWithSubtitle 에서 수행.
            // 여기서 호출하면 동일한 cold-decoder seek 가 두 번 발생하여 ~1fps 지연의 원인이 됨.

            // DF-1-17 (D-17a): 영상 로드 감사 이벤트 — 성공 경로에서만
            LogService.AuditVideoLoad(filePath);

            VideoLoaded?.Invoke(this, filePath);

            // Attempt to load SRT subtitle
            await LoadSubtitleForVideo(filePath);

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Attempts to load an external .srt file or extract embedded subtitles from the video.
        /// </summary>
        private async Task LoadSubtitleForVideo(string filePath)
        {
            string? videoDir = Path.GetDirectoryName(filePath);
            string videoName = Path.GetFileNameWithoutExtension(filePath);

            if (videoDir == null) return;

            string externalSrtPath = Path.Combine(videoDir, $"{videoName}.srt");

            if (File.Exists(externalSrtPath))
            {
                currentSrtFile = externalSrtPath;
                await LoadSrtFileAsync(externalSrtPath);
                return;
            }

            // No external SRT file -- try extracting from the video
            await ExtractSrtFromVideoAsync(filePath);
        }

        #endregion

        #region Frame Loading

        /// <summary>
        /// Seeks to the given frame index and reads the frame into <see cref="CurrentFrame"/>.
        /// </summary>
        /// <returns>The Bitmap of the loaded frame, or null on failure.</returns>
        public Bitmap? LoadFrame(int frameIndex)
        {
            try
            {
                if (videoCapture == null || !videoCapture.IsOpened())
                    return null;

                if (frameIndex < 0 || frameIndex >= totalFrames)
                    return null;

                // DF-1-21: AVI 호환성 — seek 생략 조건
                //  (1) 새로 열린 VideoCapture 의 첫 Read (자연 위치 0)
                //  (2) sequential next frame (재생 중 timer tick — currentFrameIndex+1)
                // 일부 AVI 코덱은 Set(PosFrames) seek 후 Read 가 empty 를 반환하거나 seek 자체가 무시됨.
                // sequential read 는 모든 코덱에서 안전하게 동작.
                bool skipSeek = (_isFreshlyOpened && frameIndex == 0)
                                || (frameIndex == currentFrameIndex + 1);
                if (!skipSeek)
                {
                    videoCapture.Set(VideoCaptureProperties.PosFrames, frameIndex);
                }

                currentFrame?.Dispose();
                currentFrame = new Mat();

                // PERF-V2-JITTER-INST (D: gc2): 호출 간 Gen2 collection delta — large bitmap 압박 추적.
                int gen2Delta = 0;
                if (PerfLog.Enabled)
                {
                    int gen2Now = GC.CollectionCount(2);
                    gen2Delta = gen2Now - PerfLog.LastGen2Count;
                    PerfLog.LastGen2Count = gen2Now;
                }

                // PerfLog: 디코드 ms 측정 — 첫 Read 만 측정 (retry loop 는 의도된 fallback 으로 신호 가치 없음)
                long decodeMs = 0;
                Stopwatch? swDecode = PerfLog.Enabled ? Stopwatch.StartNew() : null;
                videoCapture.Read(currentFrame);
                if (swDecode != null)
                {
                    swDecode.Stop();
                    decodeMs = swDecode.ElapsedMilliseconds;
                }

                // 일부 코덱은 첫 Read 후에도 empty Mat — 후속 Read 호출에서 디코더 버퍼가 채워짐.
                int retryCount = 0;
                while (currentFrame.Empty() && retryCount < 3)
                {
                    videoCapture.Read(currentFrame);
                    retryCount++;
                }

                // 첫 read 가 성공/실패 여부와 무관하게 freshly-opened 표시는 해제 (seek 경로로 진입)
                _isFreshlyOpened = false;

                Bitmap? bitmap = null;
                long toBmpMs = 0;
                if (!currentFrame.Empty())
                {
                    Stopwatch? swToBmp = PerfLog.Enabled ? Stopwatch.StartNew() : null;
                    bitmap = BitmapConverter.ToBitmap(currentFrame);
                    if (swToBmp != null)
                    {
                        swToBmp.Stop();
                        toBmpMs = swToBmp.ElapsedMilliseconds;
                    }
                }

                if (PerfLog.Enabled)
                {
                    Log.Debug("[PERF] LoadFrame f={Frame} {W}x{H} decode={DecodeMs}ms toBmp={ToBmpMs}ms skipSeek={SkipSeek} gc2={Gen2Delta}",
                        frameIndex, FrameWidth, FrameHeight, decodeMs, toBmpMs, skipSeek, gen2Delta);
                }

                currentFrameIndex = frameIndex;
                FrameChanged?.Invoke(this, frameIndex);

                return bitmap;
            }
            catch (OpenCvSharp.OpenCVException ocvEx)
            {
                Log.Error(ocvEx, "[프레임 로드 오류 - OpenCV] {Message}", ocvEx.Message);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[프레임 로드 오류] {Message}", ex.Message);
                return null;
            }
        }

        #endregion

        #region Video Playback

        /// <summary>
        /// Starts playback. Returns false if video is not loaded.
        /// </summary>
        public bool StartPlayback()
        {
            if (videoCapture == null || !videoCapture.IsOpened())
                return false;

            isPlaying = true;
            lastFrameTime = DateTime.Now.Ticks / 10000;
            msPerFrame = 1000.0 / fps;

            if (msPerFrame <= 0)
            {
                isPlaying = false;
                return false;
            }

            PlayStateChanged?.Invoke(this, true);
            return true;
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        public void StopPlayback()
        {
            isPlaying = false;
            PlayStateChanged?.Invoke(this, false);
        }

        /// <summary>
        /// Toggles between play and pause.
        /// </summary>
        /// <returns>The new playing state.</returns>
        public bool TogglePlayback()
        {
            if (isPlaying)
            {
                StopPlayback();
            }
            else
            {
                StartPlayback();
            }
            return isPlaying;
        }

        /// <summary>
        /// Should be called by the UI timer tick. Calculates elapsed time and advances frames.
        /// </summary>
        /// <returns>A Bitmap of the new frame if the frame changed, or null if no frame advance was needed.</returns>
        public Bitmap? OnTimerTick()
        {
            if (!isPlaying)
                return null;

            long currentTime = DateTime.Now.Ticks / 10000;
            long elapsedMs = currentTime - lastFrameTime;

            int framesToMove = (int)(elapsedMs * playbackSpeed / msPerFrame);

            if (framesToMove > 0)
            {
                int nextFrame = Math.Min(totalFrames - 1, currentFrameIndex + framesToMove);
                var bitmap = LoadFrame(nextFrame);
                lastFrameTime = currentTime;

                if (nextFrame >= totalFrames - 1)
                {
                    isPlaying = false;
                    playbackSpeed = 1.0;
                    PlayStateChanged?.Invoke(this, false);
                }

                return bitmap;
            }

            return null;
        }

        #endregion

        #region Time Formatting

        /// <summary>
        /// Formats a frame index as hh:mm:ss using the current FPS.
        /// </summary>
        public string FormatFrameTime(int frameIndex)
        {
            if (fps <= 0) return "00:00:00";
            TimeSpan time = TimeSpan.FromSeconds(frameIndex / fps);
            return time.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// Gets the subtitle text for the current frame.
        /// </summary>
        public string GetCurrentSubtitle()
        {
            if (subtitleEntries.Count == 0 || fps <= 0)
                return "";

            double currentSeconds = currentFrameIndex / fps;
            TimeSpan currentTime = TimeSpan.FromSeconds(currentSeconds);

            var currentSubtitle = subtitleEntries.FirstOrDefault(s =>
                currentTime >= s.StartTime && currentTime <= s.EndTime);

            return currentSubtitle?.Text ?? "";
        }

        /// <summary>
        /// Gets the subtitle text for a specific frame.
        /// </summary>
        public string? GetSubtitleTimestampForFrame(int frameIndex)
        {
            if (subtitleEntries.Count == 0 || fps <= 0)
                return null;

            double frameSeconds = frameIndex / fps;
            TimeSpan frameTime = TimeSpan.FromSeconds(frameSeconds);

            var subtitle = subtitleEntries.FirstOrDefault(s =>
                frameTime >= s.StartTime && frameTime <= s.EndTime);

            if (subtitle != null)
            {
                return ExtractTimestampFromSubtitle(subtitle.Text);
            }

            return null;
        }

        /// <summary>
        /// Extracts a YYYY-MM-DD HH:mm:ss timestamp from subtitle text and returns it in ISO 8601 format.
        /// </summary>
        public string? ExtractTimestampFromSubtitle(string subtitleText)
        {
            if (string.IsNullOrEmpty(subtitleText))
                return null;

            var regex = new System.Text.RegularExpressions.Regex(@"\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}");
            var match = regex.Match(subtitleText);

            if (match.Success)
            {
                return match.Value.Replace(" ", "T");
            }

            return null;
        }

        #endregion

        #region SRT Subtitle Extraction

        /// <summary>
        /// Extracts SRT subtitles from an embedded subtitle stream using FFmpeg.
        /// </summary>
        public async Task<bool> ExtractSrtFromVideoAsync(string videoPath)
        {
            if (!isFFmpegAvailable)
            {
                Log.Warning("FFmpeg 미설치로 자막 추출을 건너뜁니다: {VideoPath}", videoPath);
                return false;
            }

            try
            {
                string? videoDir = Path.GetDirectoryName(videoPath);
                string videoName = Path.GetFileNameWithoutExtension(videoPath);
                if (videoDir == null) return false;

                string srtPath = Path.Combine(videoDir, $"{videoName}.srt");

                if (File.Exists(srtPath))
                    File.Delete(srtPath);

                var ffTask = FFMpegArguments
                    .FromFileInput(videoPath)
                    .OutputToFile(srtPath, true, options => options
                        .WithCustomArgument("-loglevel error")
                        .WithCustomArgument("-map 0:s:0")
                        .WithCustomArgument("-c:s srt")
                    )
                    .ProcessAsynchronously();

                var result = await ffTask;

                if (result && File.Exists(srtPath))
                {
                    currentSrtFile = srtPath;
                    await LoadSrtFileAsync(srtPath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Loads an external SRT file and populates <see cref="SubtitleEntries"/>.
        /// </summary>
        public async Task LoadSrtFileAsync(string srtPath)
        {
            try
            {
                subtitleEntries.Clear();
                string[] lines = await File.ReadAllLinesAsync(srtPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    if (int.TryParse(lines[i], out int index))
                    {
                        i++;

                        if (i < lines.Length && lines[i].Contains("-->"))
                        {
                            string[] timeParts = lines[i].Split(new[] { " --> " }, StringSplitOptions.None);
                            if (timeParts.Length == 2)
                            {
                                TimeSpan startTime = ParseSrtTime(timeParts[0]);
                                TimeSpan endTime = ParseSrtTime(timeParts[1]);

                                i++;

                                List<string> textLines = new();
                                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                                {
                                    textLines.Add(lines[i]);
                                    i++;
                                }

                                if (textLines.Count > 0)
                                {
                                    subtitleEntries.Add(new SubtitleEntry
                                    {
                                        Index = index,
                                        StartTime = startTime,
                                        EndTime = endTime,
                                        Text = string.Join(" ", textLines)
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (FormatException fmtEx)
            {
                Log.Warning("[자막 파싱 오류] SRT 파일 형식 오류: {Message}", fmtEx.Message);
                subtitleEntries.Clear();
            }
            catch (IOException ioEx)
            {
                Log.Warning("[자막 로드 오류] 파일 읽기 실패: {Message}", ioEx.Message);
            }
            catch (Exception ex)
            {
                Log.Warning("[자막 로드 오류] {Message}", ex.Message);
            }
        }

        private TimeSpan ParseSrtTime(string timeString)
        {
            string[] parts = timeString.Split(',');
            if (parts.Length == 2)
            {
                string[] timeParts = parts[0].Split(':');
                if (timeParts.Length == 3)
                {
                    int hours = int.Parse(timeParts[0]);
                    int minutes = int.Parse(timeParts[1]);
                    int seconds = int.Parse(timeParts[2]);
                    int milliseconds = int.Parse(parts[1]);

                    return new TimeSpan(0, hours, minutes, seconds, milliseconds);
                }
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Checks for FFmpeg availability on the system PATH or in the application folder.
        /// Call this once at application startup.
        /// </summary>
        public void SetupFFmpegPath()
        {
            try
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = "-version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    process.WaitForExit(3000);

                    if (process.ExitCode == 0)
                    {
                        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = "" });
                        isFFmpegAvailable = true;
                        return;
                    }
                }
                catch
                {
                    // PATH에서 FFmpeg를 찾지 못함
                }

                string localFFmpegPath = Path.Combine(Application.StartupPath, "ffmpeg");
                string ffmpegExe = Path.Combine(localFFmpegPath, "ffmpeg.exe");

                if (File.Exists(ffmpegExe))
                {
                    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = localFFmpegPath });
                    isFFmpegAvailable = true;
                }
                else
                {
                    isFFmpegAvailable = false;
                    Log.Warning("FFmpeg를 찾을 수 없습니다. 자막 추출 기능이 비활성화됩니다. " +
                                "PATH에 ffmpeg를 추가하거나 실행 폴더의 ffmpeg/ 하위에 ffmpeg.exe를 배치하세요.");
                }
            }
            catch (Exception ex)
            {
                isFFmpegAvailable = false;
                Log.Warning(ex, "FFmpeg를 찾을 수 없습니다. 자막 추출 기능이 비활성화됩니다.");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                StopPlayback();

                currentFrame?.Dispose();
                currentFrame = null;

                if (videoCapture != null)
                {
                    videoCapture.Release();
                    videoCapture.Dispose();
                    videoCapture = null;
                }
            }

            disposed = true;
        }

        ~VideoService()
        {
            Dispose(false);
        }

        #endregion
    }
}
