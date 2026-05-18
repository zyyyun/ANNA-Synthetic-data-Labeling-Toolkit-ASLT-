---
slug: 260512-pf6-rollback-pf5-multithread-decode
status: complete
completed: 2026-05-12
commits:
  - b6d424e  # Part 1: pf5 rollback
  - 0b2d258  # Part 2: Task A multi-thread decode
---

# SUMMARY — pf5 롤백 + 멀티스레드 디코드

## 메타 교훈
이전 작업 흐름에서 "성능 한계 → GPU 가 답" 패턴 반복. 사용자 지적 후 교정:
- 4x cascade 분석 시 CPU 옵션 (멀티스레드 decode, background producer thread, grab/retrieve 분리, OS 내장 hw accel) 미탐색
- pf6 부터 CPU-first. GPU/외부 SDK 는 CPU 옵션 모두 소진 후만 검토.

## Part 1 — pf5 롤백 (commit b6d424e)
실측 결과 회귀 (4x: fps 7.4 → 6.5) 확인 후 sequential read 가설 폐기. OpenCvSharp 의
C# ↔ C++ marshaling overhead (~4ms/call) 가 Set 의 internal batch decode 보다 비싸기 때문.

## Part 2 — Task A 멀티스레드 디코드 (commit 0b2d258)
`Program.cs` Main 진입 직후:
```csharp
Environment.SetEnvironmentVariable(
    "OPENCV_FFMPEG_CAPTURE_OPTIONS", "threads;0", EnvironmentVariableTarget.Process);
```

FFmpeg backend 가 logical core 수 자동 검출하여 H.264 디코드를 멀티스레드로 실행.

## 예상 효과
1x 재생: decode 8ms → 2-4ms
4x 재생: cycle 17ms → 11-12ms → budget 16ms 안 → smooth 60fps 표시 가능

## 검증 시나리오
1. v1.0.4 설치 후 영상 로드
2. F12 켜고 1x 재생 → `decode=` 값이 2-4ms 인지 확인 (이전 8ms)
3. Shift+> 로 4x 재생 → `avgGap` 이 ~16-20ms, `fps=30+` 표시 확인 (이전 7.4)
4. 영상이 실제로 부드럽게 4x 진행되는지 시각 확인

## Task A 효과 부족 시 다음 단계
- Task B: background producer thread — decode 와 paint 병렬 (효과 큼, ~1일 작업)
- Task C: CAP_PROP_HW_ACCELERATION — OS 내장 hw decode (1줄, 외부 SDK 없음)
- 그래도 부족하면 GPU 외부 SDK 검토 (CUDA/D2D 등) — 배포 비용 큼
