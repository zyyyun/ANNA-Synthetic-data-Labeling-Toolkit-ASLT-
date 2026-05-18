---
slug: 260512-pf6-rollback-pf5-multithread-decode
status: in-progress
created: 2026-05-12
priority: critical
deadline: GS인증 신청 직전 (내일~모레)
---

# pf5 롤백 + 멀티스레드 디코드 (CPU-first 접근)

## 배경 (메타 — 패턴 교정)
이전 작업 흐름에서 성능 한계 부딪힐 때마다 "GPU 가 답" 으로 점프하는 패턴 발견.
- 학습 데이터 편향 (real-time video → GPU)
- 닫음 욕구 (CPU 옵션 2-3개 시도 후 GPU 로 결론)
- 책임 회피 (GPU 답안은 implementation 약속 없이 결론지을 수 있음)

사용자가 직접 지적: "성능 떨어질 때마다 GPU 사용 부추기는 느낌".
정정: GPU 도입 전 시도해야 할 **CPU 옵션** 들이 남아있었음.

## 변경

### Part 1 — Phase 4 (pf5) 롤백
**근거**: pf5 의 sequential read 실측 결과 회귀 — fps 7.4 → 6.5.
OpenCvSharp 의 C# ↔ C++ marshaling overhead (~4ms/call) 가 Set 의 internal batch decode 보다 비싸서,
큰 gap 점프 시 sequential 가 N × 8ms vs Set 의 N × 4ms (internal) 로 더 느림.

`Services/VideoService.cs`:
- `SEQUENTIAL_READ_THRESHOLD` const 제거
- `LoadFrame` 의 seek 로직 원복 (skipSeek = freshFirstRead || gap==1)
- decode 측정 단일 Read 만 측정 (sequential discard 로직 제거)

### Part 2 — Task A: FFmpeg 멀티스레드 디코드 활성화
`Program.cs` Main 진입 시 환경변수 설정:
```csharp
Environment.SetEnvironmentVariable(
    "OPENCV_FFMPEG_CAPTURE_OPTIONS",
    "threads;0",
    EnvironmentVariableTarget.Process);
```

`threads;0` = FFmpeg 가 자동으로 코어 수 검출. 보통 모든 logical core 사용.

**효과 예상**:
- 1080p H.264 sequential decode 8ms → 2-4ms (코어 수에 비례)
- 4x cycle: decode 비용 절반 이하 감소 → smooth 가능성 증가
- GPU/외부 SDK 없이 순수 CPU 멀티스레드 활용
- 배포 영향 0 (환경변수만)

**리스크**: 매우 낮음. FFmpeg 의 표준 옵션. 멀티스레드 디코드는 성숙한 기능.

## 검증
- 1x 재생: decode 시간 8ms → 2-4ms 확인 (logical core 수 클수록 효과)
- 4x 재생: decode 비용 절반 이하 → cycle 단축 → fps 표시 개선
- decode 측정값이 충분히 작으면 (~2-3ms), 4x cycle = 2-3 + 9 (paint) ≒ 11-12ms < 16ms budget → 60fps 표시 가능
- 환경변수 미설정 환경 (테스트 환경 등) 에서도 fallback 정상 작동 (FFmpeg default = single thread)
- 메모리 누수 없음 — 환경변수만 set, 추가 자원 없음

## Task A 가 부족할 때 — Task B, C
이번 PLAN 의 범위 밖이지만, 효과 부족 시 다음 단계:
- **Task B (background producer 스레드)**: ~1일. decode 와 paint 를 별도 스레드로 분리. 효과 큼.
- **Task C (CAP_PROP_HW_ACCELERATION)**: 1줄. OS 내장 hw decode 활성화. 외부 SDK 없음.

## Definition of Done
1. pf5 변경 사항 (`SEQUENTIAL_READ_THRESHOLD`, sequential read 로직) 완전 제거
2. `Program.cs` 에 환경변수 설정 추가 (LogService.Initialize 전에)
3. dotnet build 0 errors
4. 2개 atomic commits (롤백 + Task A 분리)
5. STATE.md 갱신
6. v1.0.4 installer 재빌드

## 비범위
- Task B, C — 다음 단계 검증 후 결정
- GPU/Cuda/D2D — 본 접근으로 충분치 않을 때만
