---
slug: 260512-pf5-sequential-read-cold-seek-cascade-fix
status: in-progress
created: 2026-05-12
priority: critical
deadline: GS인증 신청 직전 (내일~모레)
---

# Phase 4 — Sequential read 로 4x 재생 cold seek cascade 차단

## 배경
260512-pf4 측정 후 발견: 4x 재생 시 매 LoadFrame 이 200ms 사이클 (fps 7.4 표시).
원인은 OpenCV `Set(PosFrames, target)` 의 hidden cost — H.264 keyframe 점프 + GOP decode chain.
30-frame 점프 시 ~150ms 비용. 4x timer 가 매 tick 이 그 비용을 반복 → cascade 락.

PERF 데이터:
- decode=12-14ms (Read 만 측정, Set 비포함)
- 실제 LoadFrame 사이클 ~200ms (Set hidden cost 추정 ~180ms)
- skipSeek=false 가 100% — 매 frame 이 cold seek

## Fix
`VideoService.LoadFrame` 의 seek 로직 재구조:

```
gap = frameIndex - currentFrameIndex
if (freshFirstRead): no-op
elif (gap == 1): adjacent — Set 없이 단일 Read (기존 skipSeek)
elif (1 < gap <= 60): sequential — Set 없이 (gap-1) discard Read + 1 최종 Read
else (gap > 60 or gap <= 0): Set + 단일 Read (기존 큰 점프 / backward)
```

`SEQUENTIAL_READ_THRESHOLD = 60` (H.264 GOP 보통 30-60, conservative).

## 작동 원리
- Sequential Read 는 디코더 pipeline 이 warm — frame 당 4-8ms
- Set 은 cold seek (keyframe 점프 + 그 사이 GOP 만큼 internal decode) — 50-200ms
- 4x 재생 시 framesToMove = 1-2 (sequential 경로) 또는 catch-up 시 1-30 (sequential 경로) → cascade 깨짐
- 16x 도 framesToMove 16 → sequential 경로 → smooth
- User scrub (큰 jump > 60) → Set 경로 그대로

## 예상 효과 (4x)
| 지표 | 현재 (pf4 후) | Phase 4 후 |
|------|--------------|-----------|
| LoadFrame 사이클 | ~200ms | ~16-30ms |
| 표시 fps (= LoadFrame 호출/sec) | 7.4 | ~30-60 |
| skipSeek=true 비율 | 0% | ~95%+ |
| avgGap | 135ms | ~20ms |
| maxGap | 240ms | < 50ms |
| 4x 효과 체감 | 끊김 | 부드러움 |

## 부수 효과
- decode= 측정값이 sequential reads 합산으로 변함. gap=2 시 decode=16ms (2 reads). 진단 해석 시 주의.
- 1x 재생: gap=1 항상 → 기존 skipSeek=true 경로 → 동작 변화 없음.
- 큰 user scrub (수천 frame jump): gap > 60 → Set 경로 → 동작 변화 없음.

## 검증
- `dotnet build` — 0 errors
- 1x 재생: 동작 변화 없음 확인 (fps 30, decode 8ms)
- 4x 재생: skipSeek=true 비율 ~100%, avgGap ~20ms, fps ~30-60 표시
- 16x 재생: framesToMove ~16, sequential 경로, smooth
- User trackbar drag (큰 jump): 기존 Set 경로 작동, crash 없음
- 좌표/BBox: 무영향 (Read 횟수만 변경, frame 데이터 처리 동일)

## Definition of Done
1. SEQUENTIAL_READ_THRESHOLD constant 추가
2. LoadFrame 의 seek 로직 재구조 (3-way branch)
3. decode 측정 범위에 sequential reads 포함
4. dotnet build 0 errors
5. atomic commit 1개
6. STATE.md 갱신
7. v1.0.4 installer 재빌드
