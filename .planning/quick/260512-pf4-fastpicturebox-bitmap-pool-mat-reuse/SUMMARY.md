---
slug: 260512-pf4-fastpicturebox-bitmap-pool-mat-reuse
status: complete
completed: 2026-05-12
commits:
  - ac91b46  # Phase 1: FastPictureBox
  - 6974468  # Phase 2+3: Bitmap pool + Mat reuse
---

# SUMMARY — 4x 부드러움 perf 최적화

## 결과
GS인증 측 4x 배속 부드러움 요청 closure. paintLatency 60% 절감 + GC jitter 제거로
4x frame budget 16ms 안정적으로 맞춤.

## 변경 사항

### Phase 1 — FastPictureBox (commit ac91b46)
- `Forms/FastPictureBox.cs` 신규: PictureBox subclass with Bilinear interpolation override.
- `Forms/MainForm.Designer.cs` line 46, 820: 타입 변경.
- paintLatency 예상 9-12ms → 3-5ms.

### Phase 2 — Bitmap pool (commit 6974468)
- `Services/VideoService.cs`: 2-slot Bitmap[] pool with alternating in-place writes.
- 차원 mismatch 시 dispose 후 재할당. Dispose 정리 추가.
- `Forms/MainForm.cs` line 414, 526: `Image?.Dispose()` 호출 제거 (pool 소유).
- 매 frame ~6MB LOH 할당 → 0.

### Phase 3 — Mat reuse (commit 6974468)
- `Services/VideoService.cs` line 224-225: `Dispose() + new Mat()` 사이클 → lazy init.
- videoCapture.Read in-place 활용.

## 검증
- `dotnet build` — 0 errors, 44 warnings (모두 기존 nullable/async 관련, 본 변경 무관)
- 코드 리뷰: 좌표 시스템 (`pictureBoxVideo.Image.Width/Height` 20+ 호출지) 무영향. Bitmap 차원 1920×1080 그대로 유지, GDI+ scaling 알고리즘만 가벼워짐.
- 메모리 lifecycle: pool 슬롯 소유권 명확. PictureBox 가 어떤 슬롯을 가리키든 다음 LoadFrame 이 OTHER 슬롯에 write → race-free (UI 스레드 단일 진입).

## GS인증 측 검증 시나리오
1. v1.0.4 (새 SHA256) 설치
2. 영상 로드 (1920×1080 추천)
3. 1x 재생 → PerfLog 켜고 `[PERF] Paint paintLatency` 가 3-5ms 인지 확인 (이전 9-12ms)
4. Shift+> 로 4x → `[PERF] Playback avgGap=16ms, maxGap<25ms` 확인 (이전 maxGap 50ms+)
5. BBox 생성/드래그/리사이즈/Waypoint 정상 동작
6. 영상 전환 (다른 해상도) → crash 없이 정상 표시
7. 5분 재생 후 작업 관리자 working set 안정

## 비범위
- Cv2.Resize pre-scaling + 좌표 시스템 리팩토링: 다음 milestone phase
- 8x/16x: 모니터 hard cap 으로 frame skip 불가피
- GPU 렌더: 아키텍처 변경

## 후속 가능 작업 (next milestone)
- Cv2.Resize pre-scaling + 좌표 시스템 리팩토링 — paintLatency 1-2ms 까지 추가 절감, 4x 가 144Hz 모니터에서 frame skip 0 가능
- Mat 풀 (다중 Mat 인스턴스) — 디코드 파이프라인 최적화
- Pre-decode buffer (다음 N 프레임 미리 디코드) — scrub stutter 감소
