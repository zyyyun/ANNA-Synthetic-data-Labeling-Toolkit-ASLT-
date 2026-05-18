---
gsd_state_version: 1.0
milestone: v1.0.3
milestone_name: JSON 저장 결함 수정 (KTC 2차)
status: shipped
shipped_at: 2026-05-06
last_updated: "2026-05-12T09:00:00Z"
last_activity: 2026-05-12
progress:
  total_phases: 1
  completed_phases: 1
  total_plans: 3
  completed_plans: 3
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-06)

**Core value:** 모든 라벨링 기능이 GS인증 1등급 기준(ISO/IEC 25023)을 충족하며 결함 없이 동작
**Current focus:** v1.0.3 shipped — Planning next milestone (`/gsd:new-milestone`)

## Current Position

**v1.0.3 — SHIPPED 2026-05-06**

마일스톤 종료. 다음 사이클 시작 전 idle 상태.

- Final build: ASLT-Setup-v1.0.3.exe (98.19 MB, 5차 빌드 final, SHA256 `D139CD900A36F2CB098DB66002CC82D4E61CA84FC0F90FEC547FA577C028B496`)
- Tag: v1.0.3 (re-tagged 2026-05-07 — OnboardingForm Shift 변형 일관성 `f719195` + FUNC-12 보강 `29be68e` close/switch path)
- Coverage: KTC 2차 결함 2건 (DF-2-05, DF-2-06) 0건화 + UAT 15/15 시나리오 통과 + OnboardingForm 일관성 + FUNC-12 implementation gap closure

상세: [MILESTONES.md](MILESTONES.md), [milestones/v1.0.3-ROADMAP.md](milestones/v1.0.3-ROADMAP.md), [milestones/v1.0.3-REQUIREMENTS.md](milestones/v1.0.3-REQUIREMENTS.md)

## Accumulated Context

### Decisions

전체 결정 로그: PROJECT.md Key Decisions 표 + milestones/v1.0-ROADMAP.md + milestones/v1.0.3-ROADMAP.md.

### Open Blockers

- **DOC-01/02/03**: 제품설명서·사용자취급설명서 미작성 — 인증 신청 전 처리 필요 (다음 milestone, v1.0 부터 이월)
- **AVI cold-start**: 일부 AVI 파일 첫 로드 재생 불가 — 재현 데이터 부족, 정밀 조사는 다음 milestone (v1.0 부터 이월)
- **RELI-NEW-01** _(v1.0.3 Phase 7-03 발견)_: `btnExportJson_Click` 정상 저장 경로의 `LogService.AuditJsonSave` 호출 회귀 테스트 + audit log E2E 검증. UAT JSON 저장 활동의 [AUDIT] 엔트리가 audit log 에 부재 (HMAC chain 자체는 정상). KTC 인증 신청 직전에 closure 필요. 상세는 [milestones/v1.0.3-ROADMAP.md](milestones/v1.0.3-ROADMAP.md) "Issues Deferred" 섹션 참조.
- **USAB-05**, **HumanUAT 잔여 항목** — v1.0 부터 이월

### Pending Todos

None — milestone 종료. 다음 milestone 의 fresh requirements 는 `/gsd:new-milestone` 호출 시 정의.

### Quick Tasks Completed (cumulative)

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260421-mzz | Codex 교차검증 후속 — installer uninstall + build.bat 내구성 | 2026-04-21 | ef84820 | [260421-mzz](./quick/260421-mzz-codex-installer-uninstall-build-bat/) |
| 260427-eyf | Installer 빌드 자동화 + 1.0.0 → 1.0.1 bump | 2026-04-27 | dbe7a84 | [260427-eyf](./quick/260427-eyf-installer-1-0-1/) |
| 260512-ifn | 영상 hot path perf 진단 계측 (PerfLog + F12 토글, LoadFrame/Paint/MouseMove) | 2026-05-12 | cb8d7f7 | [260512-ifn](./quick/260512-ifn-perf-instrumentation-for-video-hot-paths/) |
| 260512-kma | Timer 정밀도 수정 (timeBeginPeriod 1ms scheduler) + v2 jitter 계측 (Playback fps, paintLatency, gc2) | 2026-05-12 | 91a0b39 | [260512-kma](./quick/260512-kma-timer-fix-timebeginperiod-v2-jitter-inst/) |
| 260512-m02 | 실제 timer fix — Windows.Forms.Timer Interval 33→8 + lastFrameTime drift 누적 차단 (22fps→30fps 회복) | 2026-05-12 | ccf1218 | [260512-m02](./quick/260512-m02-real-timer-fix-interval-8-lastframetime-/) |
| 260512-sek | Seek cascade fix — 재생 중 사용자 seek 시 lastFrameTime 리셋으로 cold seek 폭주 차단 (15 user-seek paths) | 2026-05-12 | 5414a5c | (inline fix, PLAN not split) |
| 260512-v04 | Version bump 1.0.3 → 1.0.4 + installer 재빌드 (perf 개선 누적 반영) | 2026-05-12 | 49b8319 | (inline) |
| 260512-gsv | GS인증 측 보고 — Shift+>/< 배속 단축키 textbox 포커스 시 차단 → ProcessCmdKey 로 이동하여 포커스 무관 작동 | 2026-05-12 | 48b7f5b | [260512-gsv](./quick/260512-gsv-shortcut-speed-shift-period-comma/) |
| 260512-spd | 배속 시뮬레이션 공식 회귀 fix — `lastFrameTime += N * msPerFrame / playbackSpeed` (260512-m02 가 / playbackSpeed 누락 → 모든 속도가 1x 로 고착). 1x 의 30fps 회복은 항등식이라 영향 없음 | 2026-05-12 | 6ebf9c2 | (inline, 1-line fix) |
| 260512-pf4 | 4x 부드러움 perf — FastPictureBox (Bilinear interpolation, paintLatency 9-12ms→8-10ms 측정) + Bitmap pool (LOH 할당 churn 제거, toBmp=0ms 확인) + Mat reuse (unmanaged 메모리 churn 제거). 좌표 시스템 무변경 | 2026-05-12 | ac91b46 + 6974468 | [260512-pf4](./quick/260512-pf4-fastpicturebox-bitmap-pool-mat-reuse/) |
| 260512-pf5 | 4x cold seek cascade 차단 — `VideoService.LoadFrame` 의 작은 forward gap(2..60)을 Set(PosFrames) 대신 sequential Read 로 처리. **실측 결과 회귀 (fps 7.4→6.5) — pf6 에서 롤백.** | 2026-05-12 | e65e0fe (롤백됨) | [260512-pf5](./quick/260512-pf5-sequential-read-cold-seek-cascade-fix/) |
| 260512-pf6 | pf5 롤백 + Task A 멀티스레드 디코드. (1) pf5 가설 회귀 확인 후 sequential read 로직 원복. (2) `OPENCV_FFMPEG_CAPTURE_OPTIONS=threads;0` 환경변수로 OpenCV FFmpeg 의 H.264 디코드를 logical core 수만큼 병렬화. 1080p decode 8ms → 2-4ms 예상. CPU-first 접근 (GPU/외부 SDK 도입 없음) | 2026-05-12 | b6d424e + 0b2d258 | [260512-pf6](./quick/260512-pf6-rollback-pf5-multithread-decode/) |

## Session Continuity

Last session: 2026-05-12T11:00:00Z
Stopped at: 260512-pf6 완료 — pf5 (sequential read) 실측 회귀 확인 후 롤백 + CPU-first Task A 진입 (OpenCV FFmpeg 멀티스레드 디코드 환경변수 활성화). 메타 교훈: 성능 한계 시 GPU 점프 패턴 교정 — CPU 옵션 (멀티스레드, background thread, OS hw accel) 충분히 탐색 후 GPU 검토. Task A 검증 후 부족 시 Task B (background producer thread) 또는 Task C (OS hw decode) 순차 진행. v1.0.4 installer 재빌드 후 GS 측 전달 예정.
Resume file: None
