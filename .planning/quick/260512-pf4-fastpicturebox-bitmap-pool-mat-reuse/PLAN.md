---
slug: 260512-pf4-fastpicturebox-bitmap-pool-mat-reuse
status: in-progress
created: 2026-05-12
priority: high
deadline: GS인증 신청 직전 (내일~모레)
---

# 4x 재생 부드러움 — FastPictureBox + Bitmap pool + Mat reuse

## 배경

GS인증 측 추가 요청 — Shift+>/< 단축키 작동 외에 **4x 배속에서도 영상이 부드럽게 재생되어야** 함.
현재 4x 시 frame budget 16ms 안에 decode(4ms) + paintLatency(9-12ms) + 기타 = 거의 한계.
maxGap 50ms+ spike 가 사용자 체감 "버벅임" 의 원인.

좌표 시스템 (1920×1080 frame ↔ 1508×848 view) 리팩토링은 회귀 위험 큼 + GS deadline 내 불가.
대안: GDI+ scaling 알고리즘 변경 + LOH 할당 churn 제거로 paintLatency 와 GC jitter 동시 절감.

## 3-Phase 구현

### Phase 1 — `FastPictureBox` subclass (paintLatency 절감)
**파일**: `Forms/FastPictureBox.cs` (신규)

PictureBox 를 subclass. `OnPaint` 에서 `Graphics.InterpolationMode = Bilinear`,
`PixelOffsetMode = Half` 설정 후 base.OnPaint 호출. base 의 image scaling 이
우리 Graphics 설정으로 동작 → HighQualityBicubic → Bilinear 전환.

**예상 효과**: paintLatency 9-12ms → 3-5ms (60% 절감). 1080p downsample 화질 차이 미미.

**위험**: 매우 낮음. Image, SizeMode, Paint event, BBox drawing 모두 무변경.

**Designer 변경**:
- `Forms/MainForm.Designer.cs` line 46: `new System.Windows.Forms.PictureBox()` → `new ASLTv1.Forms.FastPictureBox()`
- line 820: `private System.Windows.Forms.PictureBox pictureBoxVideo;` → `private ASLTv1.Forms.FastPictureBox pictureBoxVideo;`

### Phase 2 — Bitmap pool (GC 압박 제거)
**파일**: `Services/VideoService.cs`

2-슬롯 Bitmap 풀. LoadFrame 마다 alternating slot 에 `BitmapConverter.ToBitmap(Mat, Bitmap)` in-place
write. PictureBox.Image 가 마지막 할당 슬롯 참조. 다음 LoadFrame 은 다른 슬롯에 write —
표시 중 슬롯과 충돌하지 않음.

영상 차원 mismatch 시 (해상도 변경 등) 슬롯 dispose 후 재할당. Dispose 도 추가
(Form 종료 시 양 슬롯 정리).

`MainForm.cs` 의 `pictureBoxVideo.Image?.Dispose()` 호출 (line 414, 526) 제거 — pool 이 슬롯 소유.

**예상 효과**: 매 frame ~6MB LOH Bitmap 할당 → 0. Gen2 GC 빈도 대폭 감소 → jitter spike 제거.

**위험**: 중간. Bitmap lifecycle. 2-slot alternation 으로 in-place write race 회피.

### Phase 3 — Mat reuse (unmanaged 메모리 churn 제거)
**파일**: `Services/VideoService.cs`

`currentFrame?.Dispose(); currentFrame = new Mat();` → `if (currentFrame == null) currentFrame = new Mat();`

`videoCapture.Read(mat)` 는 기존 Mat 에 in-place write 가능. dispose+new 사이클 제거.

**예상 효과**: 매 frame Mat wrapper 할당 churn 감소. minor 하지만 누적 효과 있음.

**위험**: 낮음. OpenCV C++ Mat 이 ref-counted, Read 가 안전하게 buffer 교체.

## 검증

- `dotnet build` — 0 errors
- 영상 로드 + 1x 재생: paintLatency 3-5ms 로 감소 확인
- 4x 재생: avgGap ~16ms, maxGap < 25ms (이전 50ms+ 에서)
- BBox 생성/편집/드래그/리사이즈: 좌표 정확성 무변화
- 영상 전환 (다른 해상도): pool 슬롯 재할당, crash 없음
- Form resize / 최대화: GDI+ scaling 결과 OK
- 5분 재생 후 작업 관리자 working set 안정 (메모리 누수 없음)

## 비범위

- Cv2.Resize pre-scaling: 좌표 시스템 리팩토링 동반 — 다음 milestone phase 로
- 8x/16x 최적화: 모니터 refresh hard cap (60Hz) 으로 frame skip 불가피
- Custom Paint (PictureBox.Image 우회): 좌표 시스템 침투 — 미루기
- GPU 렌더 (D2D/D3D): 아키텍처 변경 — 미루기

## Definition of Done

1. FastPictureBox.cs 신규, Designer.cs 두 군데 변경
2. VideoService.cs Bitmap pool + Mat reuse + Dispose 정리
3. MainForm.cs `Image?.Dispose()` 두 군데 제거
4. dotnet build 0 errors
5. Phase 1, Phase 2+3 두 개 atomic commits
6. STATE.md 갱신
7. v1.0.4 installer 재빌드 (최종)
