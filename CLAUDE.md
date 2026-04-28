<!-- GSD:project-start source:PROJECT.md -->
## Project

**ASLT - ANNA Synthetic data Labeling Toolkit**

ASLT는 영상 내 객체(사람, 차량, 이벤트)에 바운딩 박스를 그리고 COCO 형식 JSON으로 내보내는 Windows 데스크톱 라벨링 도구다. IFEZ 등 내부 연구원/엔지니어가 교통 영상 분석용 학습 데이터를 생성하는 데 사용한다.

**Core Value:** 모든 라벨링 기능이 GS인증 1등급 기준(ISO/IEC 25023 8대 품질 특성)을 충족하며 결함 없이 정확하게 동작해야 한다.

### Constraints

- **Tech stack**: C# .NET 8.0 WinForms 유지 — 기존 코드 기반 개선만
- **Certification**: ISO/IEC 25023 8대 품질 특성 모두 충족 필요
- **Security**: KISA 가이드 준수 — SHA-256 이상 단방향 암호화 + Salt
- **Defects**: Critical/High 등급 결함 0건 필수 (Medium 이하 최소화)
- **Documentation**: 제품 설명서 + 사용자 취급 설명서 필요 (코드와 동작 일치)
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# - Windows Forms desktop application, all business logic and UI
## Runtime
- .NET 8.0 (net8.0-windows)
- Windows Forms (UseWindowsForms: true)
- NuGet (implicit in .NET project)
- Project: `ASLTv1.0.csproj`
- Lockfile: `obj/project.assets.json` (auto-generated)
## Frameworks
- Windows Forms - Desktop UI framework
- .NET Framework Standard Library - Base runtime
- OpenCvSharp4 v4.11.0.20250507 - Computer vision operations, frame processing
- OpenCvSharp4.runtime.win v4.11.0.20250507 - Native Windows runtime for OpenCV
- OpenCvSharp4.Extensions v4.11.0.20250507 - Extension methods and utilities
- Newtonsoft.Json (JSON.NET) v13.0.3 - JSON serialization/deserialization for COCO format data
- FFMpegCore v5.1.0 - FFmpeg wrapper for video processing, subtitle extraction, video analysis
- System.Drawing.Common v8.0.11 - Graphics, image rendering, coordinate transformations
## Key Dependencies
- OpenCvSharp4 v4.11.0.20250507 - Core video frame capture, image processing (VideoCapture, Mat objects)
- FFMpegCore v5.1.0 - SRT subtitle extraction, video codec handling
- Newtonsoft.Json v13.0.3 - COCO format JSON export/import, annotation serialization
- System.Drawing.Common v8.0.11 - Display rendering, bounding box visualization, coordinate math
## Configuration
- Solution file: `ASLTv1.0.sln`
- Project file: `ASLTv1.0.csproj`
- Target platforms: AnyCPU, x64 (x64 preferred)
- ImplicitUsings: enabled
- Nullable: disabled
- Platforms: Debug|Any CPU, Debug|x64, Debug|x86, Release variants (6 configurations)
- Primary target: x64 platform
- Output: WinExe (Windows Forms executable)
## Platform Requirements
- Windows (Windows Forms requirement)
- Visual Studio 2022+ (SDK Microsoft.NET.Sdk)
- .NET 8.0 SDK
- OpenCV native DLL dependencies (OpenCvSharpExtern.dll, opencv_videoio_ffmpeg4110_64.dll)
- FFmpeg binary (system PATH or `/ffmpeg/ffmpeg.exe` in app folder)
- Windows 7 or later (Windows Forms compatible)
- .NET 8.0 Runtime
- OpenCV native binaries
- FFmpeg binary (for subtitle extraction feature)
- x64 architecture recommended
## Feature Dependencies
- OpenCvSharp4 with FFmpeg codec support for video file formats (MP4, AVI, MOV, etc.)
- VideoCapture class for frame-by-frame processing
- Mat image objects for frame storage
- FFMpegCore to extract embedded SRT subtitle streams
- File I/O for SRT parsing (SubtitleEntry model)
- Newtonsoft.Json for COCO format JSON output
- JSON schema includes: images, annotations, categories, track info (entry/exit frames, timestamps)
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming Patterns
- PascalCase for classes: `MainForm.cs`, `VideoService.cs`, `BoundingBox.cs`
- Designer files: `MainForm.Designer.cs`
- Plural for folders: `Forms/`, `Models/`, `Services/`, `Helpers/`, `Theme/`
- PascalCase for public methods: `LoadVideoAsync()`, `LoadFrame()`, `GetBoxId()`
- PascalCase for private methods: `EnableDoubleBuffering()`, `ApplyToControls()`
- Async methods suffixed with `Async`: `LoadVideoAsync()`, `LoadSrtFileAsync()`, `ExtractSrtFromVideoAsync()`
- camelCase for local variables and parameters: `videoCapture`, `currentFrame`, `frameIndex`, `isPlaying`
- camelCase for private fields: `_videoService`, `_jsonService`, `videoCapture`, `currentFrame`
- Prefix underscore for private fields (inconsistently used): Some fields use underscore (`_videoService`), others don't (`videoCapture`)
- ALL_CAPS for constants: `HANDLE_SIZE`, `MIN_BBOX_SIZE`, `MAX_UNDO_STACK`, `RESIZE_BORDER_WIDTH`
- PascalCase for class names: `VideoService`, `JsonService`, `BoundingBox`, `WaypointMarker`
- PascalCase for enum names: `DrawMode`, `UndoActionType`, `ResizeHandle`
## Code Style
- No enforced automatic formatter (no .editorconfig, prettier, or similar found)
- 4-space indentation observed
- Braces follow C# convention (opening brace on same line)
- No enforced line length limit observed
- No linting configuration found (no .editorconfig, .ruleset files)
- StyleCop or Roslyn analyzers not configured
- Nullable types disabled in csproj: `<Nullable>disable</Nullable>`
## Import Organization
- No path aliases configured
- Fully qualified namespace imports used throughout
## Error Handling
- Try-catch blocks with specific exception types:
- Custom error messages with context: `"OpenCvSharp 네이티브 DLL 초기화 실패:\n\n"`
- Result objects for async operations: `LoadResult` class with `Success`, `ErrorMessage`, and data fields
- Debug output via `System.Diagnostics.Debug.WriteLine()` for logging errors
## Logging
- Debug output for errors: `System.Diagnostics.Debug.WriteLine($"[프레임 로드 오류] {ex.Message}\n{ex.StackTrace}")`
- Prefix format: `[Operation Name]` for categorization
- Korean language comments and messages throughout codebase
- Error tracking in try-catch blocks only
## Comments
- XML documentation comments (/// style) for public classes and public methods
- Inline comments for complex logic or non-obvious behavior
- Korean language comments predominate throughout
## Function Design
- Methods range from 20-600+ lines
- Service methods tend to be longer (180-500 lines) due to complex data processing
- Helper methods are typically shorter (5-30 lines)
- Minimal parameter lists observed
- Action/callback parameters used for progress updates: `Action<string>? progressCallback = null`
- Optional parameters with null defaults: `VideoService? videoService = null`
- Nullable return types: `string?`, `Bitmap?`, `LoadResult`
- Result objects returned instead of throwing exceptions
- Properties exposed via public Properties blocks
## Module Design
- Public classes define API surface
- Services use dependency injection through constructor
- Events for notifications: `FrameChanged`, `PlayStateChanged`, `VideoLoaded`
- No barrel files or index.ts equivalents
- Direct imports from specific files: `using ASLTv1.Services;`
## Region Organization
- Code organized into logical sections using `#region`/`#endregion` blocks
- Common region names:
#region Fields
#region Properties
#region Events
#region Video Loading
#region Frame Loading
#region Video Playback
#region Time Formatting
#region SRT Subtitle Extraction
#region IDisposable
## Static Methods vs Instance Methods
- Helper classes use static methods: `CoordinateHelper` with all static methods
- Service classes use instance methods with stateful fields
- Factory methods: `DarkTheme.Apply()`, `DarkTheme.ApplyButton()`
## Nullability Handling
- Csproj sets `<Nullable>disable</Nullable>` - nullable reference types NOT enforced
- Null-coalescing operator used: `?? ""`, `?? null`
- Null-conditional operator used: `?.Invoke()`, `?.Dispose()`
- Explicit null checks: `if (string.IsNullOrEmpty(...))`, `if (videoCapture != null)`
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern Overview
- Presentation Layer (Windows Forms UI) decoupled from business logic via Services
- Service Layer provides video and JSON data operations
- Model Layer defines data contracts with COCO-format JSON serialization
- Single entry point through `MainForm`, driven by user interaction events
- Unidirectional data flow: UI → Services → Models → UI
## Layers
- Purpose: Render video frames, handle user annotations, display playback controls
- Location: `Forms/`
- Contains: UI forms (`MainForm.cs`, `AboutForm.cs`), designer files, event handlers
- Depends on: `Services/`, `Models/`, `Helpers/`, `Theme/`
- Used by: Entry point (`Program.cs`), user interaction loop
- Purpose: Encapsulate business logic for video management and data persistence
- Location: `Services/`
- Contains: `VideoService.cs`, `JsonService.cs`
- Depends on: `Models/`, external libraries (OpenCvSharp4, FFMpegCore, Newtonsoft.Json)
- Used by: MainForm for async operations, data loading/saving
- Purpose: Define data contracts and structures for annotations, COCO JSON format, and tracking
- Location: `Models/`
- Contains: `BoundingBox.cs`, `LabelingData.cs`, `WaypointMarker.cs` and supporting classes
- Depends on: Newtonsoft.Json for serialization
- Used by: Services and Presentation for data representation
- Purpose: Provide utility functions for coordinate transformations between image and view space
- Location: `Helpers/`
- Contains: `CoordinateHelper.cs` (static helper methods)
- Depends on: System.Drawing
- Used by: MainForm for bounding box positioning with aspect-ratio scaling
- Purpose: Apply consistent dark theme across all UI controls
- Location: `Theme/`
- Contains: `DarkTheme.cs` (static theme definitions and application logic)
- Depends on: System.Windows.Forms
- Used by: MainForm during initialization
## Data Flow
- **Annotation State:** `boundingBoxes` (List<BoundingBox>), `selectedBox`, `waypointMarkers` (List<WaypointMarker>)
- **UI State:** `currentMode` (DrawMode), `isDrawing`, `isDragging`, `isResizing`, draw points, drag offsets
- **Video State:** Maintained in `VideoService` (frame index, FPS, playback speed, is playing)
- **Undo/Redo:** `undoStack` and `redoStack` (Stack<UndoAction>) track annotation changes
## Key Abstractions
- Purpose: Encapsulates video capture lifecycle, frame loading, SRT subtitle extraction, FFmpeg integration
- Examples: `LoadVideoAsync()`, `LoadFrame()`, `GetSubtitleTimestampForFrame()`, playback control methods
- Pattern: Stateful service managing single video at a time; events raised on frame change and playback state change
- Purpose: Handles COCO-format JSON serialization/deserialization and category mapping
- Examples: `LoadLabelingDataAsync()`, `ExportToJsonExtended()`, category name/ID mapping via static dictionaries
- Pattern: Stateless service; static helper methods for category management; returns structured results (LoadResult)
- Purpose: Transforms between image space coordinates and PictureBox view space (handles aspect ratio scaling)
- Examples: `ImageToView()`, `ViewToImage()`, `GetImageDisplayRectangle()`
- Pattern: Pure static utility class with no state
- Purpose: Represents a single annotation rectangle with label, object IDs (person/vehicle/event), and frame association
- Fields: `FrameIndex`, `Rectangle`, `Label`, `PersonId`, `VehicleId`, `EventId`, `Action`, `IsDeleted`
- Usage: Core data structure passed between layers
- Purpose: Represents temporal range (entry/exit frames) for tracking objects across multiple frames
- Fields: `EntryFrame`, `ExitFrame`, `ObjectId`, `Label`, `MarkerColor`, timestamps, `InteractingObject`
- Usage: Defines track lifecycle; linked to bounding boxes during export
## Entry Points
- Location: `Program.cs`
- Triggers: Application startup (STAThread entry point)
- Responsibilities: Initialize Windows Forms application, instantiate and run MainForm
- Location: `Forms/MainForm.cs`
- Triggers: Form load event, user clicks on buttons/controls, keyboard input, paint events
- Responsibilities: Render video frames, handle user annotation input, manage playback, coordinate services
## Error Handling
- `LoadVideoWithSubtitle()`: Catches and displays video load failures
- `JsonService.LoadLabelingDataAsync()`: Handles OutOfMemoryException separately; returns success flag in LoadResult
- `ExportToJsonExtended()`: Wraps in try-catch, throws InvalidOperationException with context
- Backup creation: JSON files backed up before load; exceptions logged to Debug output, not thrown
- Missing resources: ResolveJsonPath() returns null if no JSON exists (no exception)
## Cross-Cutting Concerns
- Bounding box size validation: `MIN_BBOX_SIZE` constant enforced
- Frame index bounds checking before LoadFrame()
- JSON file existence checks via Directory/File APIs
- Video capture readiness verified via `IsVideoLoaded` property
- Image space: Raw frame pixel coordinates (e.g., 1920x1080 actual frame size)
- View space: PictureBox display coordinates with aspect-ratio scaling applied
- Automatic transformation via `CoordinateHelper` for all annotations
- Zoom mode in PictureBox applies letterboxing; scale factors computed per render
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd:quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd:debug` for investigation and bug fixing
- `/gsd:execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd:profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
