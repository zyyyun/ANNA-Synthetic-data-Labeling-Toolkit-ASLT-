using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ASLTv1.Helpers;
using ASLTv1.Models;

namespace ASLTv1.Services
{
    /// <summary>
    /// Handles loading and exporting labeling data in COCO-like JSON format.
    /// JSON 라벨링 데이터 로드/내보내기 서비스.
    /// </summary>
    public class JsonService
    {
        #region Category ID Mapping

        private static readonly Dictionary<string, int> CategoryNameToIdMap = new()
        {
            // Person categories (1~20)
            {"person_01", 1}, {"person_02", 2}, {"person_03", 3}, {"person_04", 4},
            {"person_05", 5}, {"person_06", 6}, {"person_07", 7}, {"person_08", 8},
            {"person_09", 9}, {"person_10", 10}, {"person_11", 11}, {"person_12", 12},
            {"person_13", 13}, {"person_14", 14}, {"person_15", 15}, {"person_16", 16},
            {"person_17", 17}, {"person_18", 18}, {"person_19", 19}, {"person_20", 20},

            // Vehicle categories (21~24)
            {"car", 21}, {"motorcycle", 22}, {"e_scooter", 23}, {"bicycle", 24},

            // Event categories (25~34)
            {"event_hazard", 25}, {"event_accident", 26}, {"event_damage", 27}, {"event_fire", 28},
            {"event_intrusion", 29}, {"event_leak", 30}, {"event_failure", 31}, {"event_lost_object", 32},
            {"event_fall", 33}, {"event_abnormal_behavior", 34}
        };

        private static readonly Color[] MarkerColors = new Color[]
        {
            Color.FromArgb(59, 130, 246),
            Color.FromArgb(16, 185, 129),
            Color.FromArgb(139, 92, 246),
            Color.FromArgb(245, 158, 11),
            Color.FromArgb(236, 72, 153),
        };

        #endregion

        #region Helper Methods

        public static int GetBoxId(BoundingBox box)
        {
            if (box.Label == "person") return box.PersonId;
            if (box.Label == "vehicle") return box.VehicleId;
            if (box.Label == "event") return box.EventId;
            return 0;
        }

        private static int GetCategoryId(string label, int boxId)
        {
            string categoryName = GetCategoryName(label, boxId);

            if (CategoryNameToIdMap.ContainsKey(categoryName))
                return CategoryNameToIdMap[categoryName];

            if (label == "person") return Math.Clamp(boxId, 1, 20);
            if (label == "vehicle") return Math.Clamp(21 + (boxId - 1), 21, 24);
            if (label == "event") return Math.Clamp(25 + (boxId - 1), 25, 34);

            return boxId;
        }

        private static string GetCategoryName(string label, int boxId)
        {
            if (label == "person")
            {
                return $"person_{boxId:D2}";
            }
            else if (label == "vehicle")
            {
                return boxId switch
                {
                    1 => "car",
                    2 => "motorcycle",
                    3 => "e_scooter",
                    4 => "bicycle",
                    _ => "car"
                };
            }
            else if (label == "event")
            {
                return boxId switch
                {
                    1 => "event_hazard",
                    2 => "event_accident",
                    3 => "event_damage",
                    4 => "event_fire",
                    5 => "event_intrusion",
                    6 => "event_leak",
                    7 => "event_failure",
                    8 => "event_lost_object",
                    9 => "event_fall",
                    10 => "event_abnormal_behavior",
                    _ => "event_hazard"
                };
            }

            return $"{label}_{boxId:D2}";
        }

        #endregion

        #region Resolve JSON Path

        /// <summary>
        /// Resolves the _labels.json file path for a given video file.
        /// Returns null if no JSON file exists.
        /// </summary>
        public string? ResolveJsonPath(string videoFilePath)
        {
            string? videoDir = Path.GetDirectoryName(videoFilePath);
            if (string.IsNullOrEmpty(videoDir) || !Directory.Exists(videoDir))
                return null;

            string saveDir = Path.Combine(videoDir, "labels");
            if (!Directory.Exists(saveDir))
                return null;

            string baseFileName = Path.GetFileNameWithoutExtension(videoFilePath);
            string normalPath = Path.Combine(saveDir, baseFileName + "_labels.json");

            if (File.Exists(normalPath))
            {
                // 경로 트래버설 방지 검증
                if (!PathValidator.IsPathSafe(normalPath, videoDir))
                {
                    Log.Warning("[보안] 경로 트래버설 감지: {Path}", normalPath);
                    return null;
                }
                return normalPath;
            }

            return null;
        }

        #endregion

        #region Load JSON

        /// <summary>
        /// Result of loading labeling data from a JSON file.
        /// </summary>
        public class LoadResult
        {
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public string? LoadedFilePath { get; set; }
            public List<BoundingBox> BoundingBoxes { get; set; } = new();
            public List<WaypointMarker> WaypointMarkers { get; set; } = new();
            public Dictionary<int, CategoryData> CategoryMap { get; set; } = new();
            public Dictionary<int, string> FrameTimestampMap { get; set; } = new();
            public int NextAnnotationId { get; set; } = 1;
        }

        /// <summary>
        /// Loads labeling data from the _labels.json file associated with a video.
        /// </summary>
        /// <param name="videoFilePath">Path to the video file.</param>
        /// <param name="fps">Video FPS (for formatting waypoint times).</param>
        /// <param name="progressCallback">Optional callback for progress updates (message).</param>
        public async Task<LoadResult> LoadLabelingDataAsync(
            string videoFilePath,
            double fps,
            int frameWidth = 0,
            int frameHeight = 0,
            Action<string>? progressCallback = null)
        {
            var result = new LoadResult();
            string loadPath = "";

            try
            {
                string? videoDir = Path.GetDirectoryName(videoFilePath);
                if (string.IsNullOrEmpty(videoDir) || !Directory.Exists(videoDir))
                {
                    result.LoadedFilePath = "";
                    return result;
                }

                string saveDir = Path.Combine(videoDir, "labels");
                if (!Directory.Exists(saveDir))
                {
                    result.LoadedFilePath = "";
                    return result;
                }

                string baseFileName = Path.GetFileNameWithoutExtension(videoFilePath);
                string normalPath = Path.Combine(saveDir, baseFileName + "_labels.json");

                if (!File.Exists(normalPath))
                {
                    result.LoadedFilePath = "";
                    return result;
                }

                loadPath = normalPath;
                result.LoadedFilePath = loadPath;

                // File size check
                FileInfo fileInfo = new FileInfo(loadPath);
                long fileSizeMB = fileInfo.Length / (1024 * 1024);

                // Create backup
                string backupPath = loadPath + ".backup";
                try
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Copy(loadPath, backupPath);
                }
                catch (IOException backupIoEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[백업 생성 실패 - I/O] {backupIoEx.Message}");
                    Log.Warning("[백업 생성 실패] I/O 오류: {Message}", backupIoEx.Message);
                }
                catch (UnauthorizedAccessException backupAuthEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[백업 생성 실패 - 권한] {backupAuthEx.Message}");
                    Log.Warning("[백업 생성 실패] 접근 권한 오류: {Message}", backupAuthEx.Message);
                }
                catch (Exception backupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[백업 생성 실패] {backupEx.Message}");
                    Log.Warning("[백업 생성 실패] {Message}", backupEx.Message);
                }

                // Read and parse JSON
                progressCallback?.Invoke("JSON 파일 읽는 중...");

                LabelingDataExtended? labelingData = null;

                await Task.Run(() =>
                {
                    using var fileStream = new FileStream(loadPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192);
                    using var streamReader = new StreamReader(fileStream, System.Text.Encoding.UTF8, true, 8192);

                    string json = streamReader.ReadToEnd();

                    progressCallback?.Invoke("JSON 파싱 중...");

                    var settings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.None
                    };
                    labelingData = JsonConvert.DeserializeObject<LabelingDataExtended>(json, settings);
                });

                if (labelingData?.Annotations == null)
                    return result;

                // ImageId -> FrameNumber mapping
                var imageIdToFrameNumber = new Dictionary<int, int>();
                if (labelingData.Images != null)
                {
                    foreach (var image in labelingData.Images)
                    {
                        imageIdToFrameNumber[image.Id] = image.FrameNumber;
                        if (!string.IsNullOrEmpty(image.Timestamp))
                        {
                            result.FrameTimestampMap[image.FrameNumber] = image.Timestamp;
                        }
                    }
                }

                if (labelingData.Categories != null)
                {
                    foreach (var category in labelingData.Categories)
                    {
                        result.CategoryMap[category.Id] = category;
                    }
                }

                // Process annotations
                progressCallback?.Invoke("데이터 처리 중...");

                var waypointKeySet = new Dictionary<string, WaypointMarker>();

                foreach (var annotation in labelingData.Annotations)
                {
                    if (annotation.Bbox == null || annotation.Bbox.Length < 4)
                        continue;

                    int trackId = annotation.TrackId;
                    string label = "person";

                    int catId = annotation.CategoryId;
                    if (catId >= 1 && catId <= 20)
                        label = "person";
                    else if (catId >= 21 && catId <= 24)
                        label = "vehicle";
                    else if (catId >= 25 && catId <= 34)
                        label = "event";
                    else if (result.CategoryMap.ContainsKey(catId))
                    {
                        string categoryName = result.CategoryMap[catId].Name;
                        if (categoryName.Contains("car") || categoryName.Contains("motorcycle") ||
                            categoryName.Contains("scooter") || categoryName.Contains("bicycle"))
                            label = "vehicle";
                        else if (categoryName.StartsWith("event_"))
                            label = "event";
                        else if (categoryName.StartsWith("person"))
                            label = "person";
                    }

                    int actualFrameNumber = annotation.ImageId;
                    if (imageIdToFrameNumber.ContainsKey(annotation.ImageId))
                        actualFrameNumber = imageIdToFrameNumber[annotation.ImageId];

                    int personId = 0, vehicleId = 0, eventId = 0;

                    if (label == "person")
                        personId = trackId;
                    else if (label == "vehicle")
                        vehicleId = catId >= 21 && catId <= 24 ? (catId - 20) : trackId;
                    else if (label == "event")
                        eventId = catId >= 25 && catId <= 28 ? (catId - 24) : trackId;

                    var box = new BoundingBox
                    {
                        FrameIndex = actualFrameNumber,
                        Rectangle = new Rectangle(annotation.Bbox[0], annotation.Bbox[1], annotation.Bbox[2], annotation.Bbox[3]),
                        Label = label,
                        PersonId = personId,
                        VehicleId = vehicleId,
                        EventId = eventId,
                        Action = "waypoint"
                    };

                    result.BoundingBoxes.Add(box);

                    if (annotation.Id >= result.NextAnnotationId)
                        result.NextAnnotationId = annotation.Id + 1;

                    // Restore waypoints
                    if (annotation.TrackInfo?.Entry != null && annotation.TrackInfo?.Exit != null)
                    {
                        int entryFrame = annotation.TrackInfo.Entry.Frame;
                        int exitFrame = annotation.TrackInfo.Exit.Frame;

                        int objectId = 0;
                        if (box.Label == "person") objectId = box.PersonId;
                        else if (box.Label == "vehicle") objectId = box.VehicleId;
                        else if (box.Label == "event") objectId = box.EventId;

                        string waypointKey = $"{box.Label}_{objectId}_{entryFrame}_{exitFrame}";

                        Color waypointColor;
                        if (box.Label == "person")
                            waypointColor = Color.FromArgb(255, 107, 107);
                        else if (box.Label == "vehicle")
                            waypointColor = Color.FromArgb(107, 158, 255);
                        else if (box.Label == "event")
                            waypointColor = Color.FromArgb(107, 255, 107);
                        else
                            waypointColor = Color.Black;

                        var waypoint = new WaypointMarker
                        {
                            ObjectId = objectId,
                            Label = box.Label,
                            EntryFrame = entryFrame,
                            ExitFrame = exitFrame,
                            EntryTime = FormatFrameTime(entryFrame, fps),
                            ExitTime = FormatFrameTime(exitFrame, fps),
                            MarkerColor = waypointColor,
                            InteractingObject = (box.Label == "event") ? (annotation.InteractingObject ?? "") : null
                        };

                        if (!waypointKeySet.ContainsKey(waypointKey))
                        {
                            waypointKeySet[waypointKey] = waypoint;
                            result.WaypointMarkers.Add(waypoint);
                        }
                        else
                        {
                            if (box.Label == "event" && !string.IsNullOrWhiteSpace(annotation.InteractingObject))
                            {
                                var existing = waypointKeySet[waypointKey];
                                if (string.IsNullOrWhiteSpace(existing.InteractingObject))
                                    existing.InteractingObject = annotation.InteractingObject;
                            }
                        }
                    }
                }

                // Fix uncolored waypoints
                int waypointIndex = 0;
                foreach (var waypoint in result.WaypointMarkers)
                {
                    if (waypoint.MarkerColor == Color.Black)
                    {
                        waypoint.MarkerColor = MarkerColors[waypointIndex % MarkerColors.Length];
                    }
                    waypointIndex++;
                }

                // 로드된 bbox에 대해 이미지 범위 클램핑 적용
                if (frameWidth > 0 && frameHeight > 0)
                {
                    foreach (var box in result.BoundingBoxes)
                    {
                        box.Rectangle = CoordinateHelper.ClampToImage(box.Rectangle, frameWidth, frameHeight);
                    }
                }

                result.Success = true;

                // DF-1-17 (D-17a): JSON 로드 성공 감사 이벤트
                LogService.AuditJsonLoad(loadPath);
            }
            catch (OutOfMemoryException oomEx)
            {
                result.Success = false;
                result.ErrorMessage = $"메모리 부족으로 파일을 로드할 수 없습니다.\n\n" +
                    $"해결 방법: 다른 프로그램을 종료하고 다시 시도하세요.\n" +
                    $"상세: {oomEx.Message}";
            }
            catch (Newtonsoft.Json.JsonReaderException jrEx)
            {
                result.Success = false;
                result.ErrorMessage = $"JSON 파일이 손상되었습니다.\n\n" +
                    $"파일: {Path.GetFileName(loadPath)}\n" +
                    $"위치: 줄 {jrEx.LineNumber}, 열 {jrEx.LinePosition}\n\n" +
                    $"해결 방법: 백업 파일({Path.GetFileName(loadPath)}.backup)을 확인하거나 " +
                    $"새로 라벨링을 시작하세요.";
                Log.Warning("[JSON 파싱 오류] {Path}: 줄 {Line}, 열 {Col}", loadPath, jrEx.LineNumber, jrEx.LinePosition);
            }
            catch (Newtonsoft.Json.JsonSerializationException jsEx)
            {
                result.Success = false;
                result.ErrorMessage = $"JSON 데이터 형식이 올바르지 않습니다.\n\n" +
                    $"상세: {jsEx.Message}\n\n" +
                    $"해결 방법: JSON 파일의 구조가 COCO 형식과 일치하는지 확인하세요.";
                Log.Warning("[JSON 역직렬화 오류] {Path}: {Message}", loadPath, jsEx.Message);
            }
            catch (IOException ioEx)
            {
                result.Success = false;
                result.ErrorMessage = $"파일을 읽을 수 없습니다.\n\n" +
                    $"파일: {Path.GetFileName(loadPath)}\n" +
                    $"원인: {ioEx.Message}\n\n" +
                    $"해결 방법: 파일이 다른 프로그램에서 사용 중인지 확인하세요.";
                Log.Warning("[파일 I/O 오류] {Path}: {Message}", loadPath, ioEx.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"라벨링 데이터 로드 중 예기치 않은 오류가 발생했습니다.\n\n" +
                    $"상세: {ex.Message}\n\n" +
                    $"해결 방법: 프로그램을 재시작하거나 파일을 다시 선택하세요.";
                Log.Error(ex, "[JSON 로드 오류] {Path}", loadPath);
            }

            return result;
        }

        #endregion

        #region Export JSON

        /// <summary>
        /// Exports annotation data to a COCO-format JSON file.
        /// </summary>
        public void ExportToJsonExtended(
            string filePath,
            string currentVideoFile,
            double fps,
            int frameWidth,
            int frameHeight,
            List<BoundingBox> boundingBoxes,
            List<WaypointMarker> waypointMarkers,
            VideoService? videoService = null)
        {
            try
            {
                var images = new List<ImageInfo>();
                var annotations = new List<AnnotationData>();
                var categories = new Dictionary<int, CategoryData>();

                var frameGroups = boundingBoxes.Where(b => !b.IsDeleted).GroupBy(b => b.FrameIndex).OrderBy(g => g.Key);
                int imageId = 0;
                int nextAnnotationId = 1;

                foreach (var frameGroup in frameGroups)
                {
                    double frameSeconds = frameGroup.Key / fps;
                    TimeSpan frameTimeSpan = TimeSpan.FromSeconds(frameSeconds);

                    string? subtitleTimestamp = videoService?.GetSubtitleTimestampForFrame(frameGroup.Key);
                    string timestamp = subtitleTimestamp ?? frameTimeSpan.ToString(@"hh\:mm\:ss\.fff");

                    var imageInfo = new ImageInfo
                    {
                        Id = imageId,
                        Height = frameHeight,
                        Width = frameWidth,
                        FrameNumber = frameGroup.Key,
                        Timestamp = timestamp
                    };

                    images.Add(imageInfo);

                    foreach (var box in frameGroup)
                    {
                        int boxId = GetBoxId(box);
                        int categoryId = GetCategoryId(box.Label, boxId);
                        string categoryName = GetCategoryName(box.Label, boxId);

                        if (!categories.ContainsKey(categoryId))
                        {
                            categories[categoryId] = new CategoryData
                            {
                                Id = categoryId,
                                Name = categoryName,
                                Supercategory = box.Label
                            };
                        }

                        // Find matching waypoint
                        var matchingWaypoint = waypointMarkers.FirstOrDefault(w =>
                            w.Label == box.Label &&
                            w.ObjectId == boxId &&
                            box.FrameIndex >= w.EntryFrame &&
                            box.FrameIndex <= w.ExitFrame);

                        int entryFrame = box.FrameIndex;
                        int exitFrame = box.FrameIndex;

                        if (matchingWaypoint != null)
                        {
                            entryFrame = matchingWaypoint.EntryFrame;
                            exitFrame = matchingWaypoint.ExitFrame;
                        }
                        else
                        {
                            var sameObjectBoxes = boundingBoxes
                                .Where(b => b.Label == box.Label && GetBoxId(b) == boxId)
                                .ToList();

                            if (sameObjectBoxes.Any())
                            {
                                entryFrame = sameObjectBoxes.Min(b => b.FrameIndex);
                                exitFrame = sameObjectBoxes.Max(b => b.FrameIndex);
                            }
                        }

                        string? entryTimestamp = videoService?.GetSubtitleTimestampForFrame(entryFrame);
                        string? exitTimestamp = videoService?.GetSubtitleTimestampForFrame(exitFrame);

                        double entrySeconds = entryFrame / fps;
                        double exitSeconds = exitFrame / fps;
                        TimeSpan entryTimeSpan = TimeSpan.FromSeconds(entrySeconds);
                        TimeSpan exitTimeSpan = TimeSpan.FromSeconds(exitSeconds);

                        var clampedRect = CoordinateHelper.ClampToImage(box.Rectangle, frameWidth, frameHeight);

                        var annotation = new AnnotationData
                        {
                            Id = nextAnnotationId++,
                            ImageId = imageId,
                            CategoryId = categoryId,
                            Bbox = new int[] { clampedRect.X, clampedRect.Y, clampedRect.Width, clampedRect.Height },
                            Area = clampedRect.Width * clampedRect.Height,
                            Iscrowd = 0,
                            TrackId = boxId,
                            TrackInfo = new TrackInfo
                            {
                                Entry = new TrackEntry
                                {
                                    Frame = entryFrame,
                                    Timestamp = entryTimestamp ?? entryTimeSpan.ToString(@"hh\:mm\:ss\.fff")
                                },
                                Exit = new TrackEntry
                                {
                                    Frame = exitFrame,
                                    Timestamp = exitTimestamp ?? exitTimeSpan.ToString(@"hh\:mm\:ss\.fff")
                                },
                                CurrentClipCount = 1
                            }
                        };

                        // Event interacting object
                        if (box.Label == "event" && matchingWaypoint != null && !string.IsNullOrWhiteSpace(matchingWaypoint.InteractingObject))
                        {
                            annotation.InteractingObject = matchingWaypoint.InteractingObject;
                        }

                        annotations.Add(annotation);
                    }

                    imageId++;
                }

                var labelingData = new LabelingDataExtended
                {
                    Info = new VideoInfoExtended
                    {
                        Description = "Extended COCO with Tracking",
                        Version = "1.0",
                        Year = DateTime.Now.Year,
                        DateCreated = DateTime.Now.ToString("yyyy-MM-dd"),
                        VideoFile = Path.GetFileName(currentVideoFile)
                    },
                    Licenses = new List<object>(),
                    Images = images,
                    Annotations = annotations,
                    Categories = categories.Values.ToList()
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.None
                };
                string json = JsonConvert.SerializeObject(labelingData, settings);
                File.WriteAllText(filePath, json);

                // DF-1-17 (D-17a): 내보내기 성공 감사 이벤트
                LogService.AuditExport(filePath, images.Count, annotations.Count);
            }
            catch (IOException ioEx)
            {
                throw new InvalidOperationException($"JSON 파일 저장 실패: {ioEx.Message}\n\n" +
                    $"해결 방법: 저장 경로에 쓰기 권한이 있는지 확인하세요.", ioEx);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                throw new InvalidOperationException($"JSON 파일 접근 권한 없음: {uaEx.Message}\n\n" +
                    $"해결 방법: 파일이 읽기 전용이 아닌지 확인하세요.", uaEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"JSON 내보내기 오류: {ex.Message}", ex);
            }
        }

        #endregion

        #region Delete JSON

        /// <summary>
        /// DF-1-04 (D-08) / DF-1-05 (D-09): 비디오에 대응하는 COCO JSON 파일을 삭제하고
        /// 감사 이벤트를 기록한다. 경로 트래버설 방지 검증 포함.
        /// Wave 5 에서 <c>LogService.AuditJsonDelete</c> 래퍼로 전환될 수 있음.
        /// </summary>
        /// <param name="videoFilePath">비디오 파일 경로 (labels/ 서브폴더의 대응 JSON 탐색 기준)</param>
        /// <returns>파일이 실제로 삭제되었으면 true, 존재하지 않거나 경로 검증 실패면 false</returns>
        /// <exception cref="IOException">파일 삭제 I/O 오류</exception>
        /// <exception cref="UnauthorizedAccessException">삭제 권한 없음</exception>
        public bool DeleteJsonForVideo(string videoFilePath)
        {
            string? jsonPath = ResolveJsonPath(videoFilePath);
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
                return false;

            // 경로 트래버설 방지 재검증 (SECU-04 패턴 — ResolveJsonPath 이미 수행하지만 방어선 이중화)
            string? videoDir = Path.GetDirectoryName(videoFilePath);
            if (!string.IsNullOrEmpty(videoDir) && !PathValidator.IsPathSafe(jsonPath, videoDir))
            {
                Log.Warning("[보안] 삭제 경로 트래버설 감지: {Path}", jsonPath);
                return false;
            }

            try
            {
                File.Delete(jsonPath);
                // DF-1-17 (D-17a): Wave 3 의 raw Log.Information 을 LogService.AuditJsonDelete 래퍼로 교체
                LogService.AuditJsonDelete(jsonPath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[JSON 삭제 오류] {Path}", jsonPath);
                throw;
            }
        }

        /// <summary>
        /// Legacy helper — DF-1-04 이전에 도입된 삭제 API. 내부적으로 <see cref="DeleteJsonForVideo"/> 로 위임하여
        /// 감사 로그 일관성을 유지한다. <paramref name="currentJsonFile"/> 힌트가 있으면 우선적으로 해당 경로를 확인한다.
        /// </summary>
        /// <returns>True if a file was deleted, false if no file existed.</returns>
        public bool DeleteJsonFileForVideo(string videoFilePath, string? currentJsonFile = null)
        {
            string? videoDir = Path.GetDirectoryName(videoFilePath);
            if (string.IsNullOrEmpty(videoDir) || !Directory.Exists(videoDir))
                return false;

            // Hint 경로가 유효하면 그대로 감사 삭제
            if (!string.IsNullOrEmpty(currentJsonFile) && File.Exists(currentJsonFile))
            {
                if (!PathValidator.IsPathSafe(currentJsonFile, videoDir))
                {
                    Log.Warning("[보안] 삭제 경로 트래버설 감지: {Path}", currentJsonFile);
                    return false;
                }
                try
                {
                    File.Delete(currentJsonFile);
                    // DF-1-17 (D-17a): Wave 3 의 raw Log.Information 을 LogService.AuditJsonDelete 래퍼로 교체
                    LogService.AuditJsonDelete(currentJsonFile);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[JSON 삭제 오류] {Path}", currentJsonFile);
                    throw;
                }
            }

            // Hint 없으면 DeleteJsonForVideo 경로 (ResolveJsonPath 기반 탐색)
            return DeleteJsonForVideo(videoFilePath);
        }

        #endregion

        #region Private Helpers

        private static string FormatFrameTime(int frameIndex, double fps)
        {
            if (fps <= 0) return "00:00:00";
            TimeSpan time = TimeSpan.FromSeconds(frameIndex / fps);
            return time.ToString(@"hh\:mm\:ss");
        }

        #endregion
    }
}
