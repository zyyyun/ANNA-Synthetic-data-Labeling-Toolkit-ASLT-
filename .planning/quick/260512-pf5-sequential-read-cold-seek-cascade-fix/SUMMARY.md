---
slug: 260512-pf5-sequential-read-cold-seek-cascade-fix
status: complete
completed: 2026-05-12
commit: e65e0fe
---

# SUMMARY — Phase 4 Sequential Read

## 변경
`Services/VideoService.cs::LoadFrame` — forward gap 1..60 시 Set 대신 sequential Read.

## 진단된 병목
- OpenCV `Set(PosFrames)` 의 GOP keyframe 점프 + internal decode chain 이 hidden cost (~150ms)
- PerfLog decode= 필드는 명시적 Read 만 측정 → 진짜 비용 노출 안 됨
- 4x 재생 매 LoadFrame 이 200ms 사이클 락 → fps 7.4

## 해결 방식
`SEQUENTIAL_READ_THRESHOLD = 60`. forward gap 60 이내면 sequential Read.
디코더 pipeline warm → frame 당 4-8ms. cascade 깨짐.

## 예상 측정 결과 (4x 재생)
- skipSeek=true 100%
- decode= sequential reads 합산 (gap=2 시 ~16ms)
- avgGap ~20ms, maxGap < 50ms
- 사용자 체감: 부드러움

## 검증 시나리오
1. 1x 재생 → 동작 변화 없음 (gap=1 항상)
2. 4x 재생 → skipSeek=true 비율 확인, fps ~30-60 표시 확인
3. 16x 재생 → smooth 확인
4. User trackbar drag (큰 jump > 60) → 기존 Set 경로
5. BBox 좌표 정확성 → 무변경 (Read 횟수만 다를 뿐)
