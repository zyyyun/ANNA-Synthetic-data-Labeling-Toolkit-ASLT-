# ASLT - ANNA Synthetic data Labeling Toolkit

## What This Is

ASLT는 영상 내 객체(사람, 차량, 이벤트)에 바운딩 박스를 그리고 COCO 형식 JSON으로 내보내는 Windows 데스크톱 라벨링 도구다. IFEZ 등 내부 연구원/엔지니어가 교통 영상 분석용 학습 데이터를 생성하는 데 사용한다.

## Current State

**Shipped:** v1.0.2 (2026-04-28) — ASLT-Setup-v1.0.2.exe (98.19 MB)
**Status:** GS인증 1등급 품질 개선 사이클 완료 — 인증 신청 전 문서 작업 필요
**Coverage:** v1 요구사항 43개 중 39 완료 (90.7%). DOC-01/02/03 + USAB-05 는 Known Gaps

상세: [MILESTONES.md](MILESTONES.md), [milestones/v1.0-ROADMAP.md](milestones/v1.0-ROADMAP.md)

## Core Value

모든 라벨링 기능이 GS인증 1등급 기준(ISO/IEC 25023 8대 품질 특성)을 충족하며 결함 없이 정확하게 동작해야 한다.

## Next Milestone Goals

v1.1 또는 인증신청-전 milestone 의 후보 작업:

- **문서화 마무리** — DOC-01 (제품설명서), DOC-02 (사용자취급설명서), DOC-03 (버전 일치 검증)
- **USAB-05 재검토** — Undo/Redo UI 버튼 추가 여부 결정
- **AVI 코덱 cold-start 정밀 조사** — 특정 파일 재현 데이터 확보 후 디코더 워밍업 fix
- **HumanUAT 미완 항목 정리**

> 다음 milestone 시작 시 `/gsd:new-milestone` 으로 fresh requirements 정의.

## Requirements

> v1.0 의 active 항목 전체는 [milestones/v1.0-REQUIREMENTS.md](milestones/v1.0-REQUIREMENTS.md) 에 archived.
> 다음 milestone 의 fresh requirements 는 `/gsd:new-milestone` 후 새로 정의된다.

### Validated (v1.0)

기존 코드베이스 기능 + v1.0 에서 개선된 항목:

- ✓ 영상 파일 로드 및 프레임 탐색 — existing
- ✓ 바운딩 박스 그리기/선택/이동/크기 조정 — existing + v1.0 (클램핑, 좌표 정확성)
- ✓ Person/Vehicle/Event 라벨 분류 — existing + v1.0 (ID 관리 재설계)
- ✓ COCO 형식 JSON 저장/로드 — existing + v1.0 (타임스탬프 정확화, 카테고리 매핑)
- ✓ Undo/Redo 기능 — existing + v1.0 (상한 + W/A/S/D + Waypoint atomic)
- ✓ Waypoint 마커 관리 — existing + v1.0 (동반 삭제, 일괄 삭제, 탐색 이동)
- ✓ SRT 자막 추출 및 표시 — existing
- ✓ 다크 테마 UI — existing
- ✓ 재생 속도 조절 — existing
- ✓ 키보드 단축키 기반 조작 — existing + v1.0 (포커스 가드, 화이트리스트)
- ✓ 파일 기반 구조화 로그 + HMAC 무결성 체인 — v1.0 (Phase 1, Phase 5.6)
- ✓ 영상 최초 로드 가드 + AVI 코덱 호환성 안전망 — v1.0 (Phase 5.5, v1.0.2)
- ✓ 시작 가이드 (온보딩 + 헤더 가이드 켜기 버튼) — v1.0
- ✓ Inno Setup 6 self-contained 인스톨러 + 클린 언인스톨 — v1.0 (Phase 5)

### Active (Next Milestone)

차기 milestone 에서 결정될 항목 — 현재는 backlog 형태:

- DOC-01: 제품설명서 (버전, 연동 제품, 시스템 요구사항)
- DOC-02: 사용자취급설명서 (모든 기능, 입력값 유효 범위, 오류 메시지)
- DOC-03: 프로그램 내 버전 정보와 문서 버전 일치
- USAB-05: Undo/Redo UI 버튼 활성/비활성 — UI 신규 vs 영구 N/A 결정

### Out of Scope

- 새 기능 추가 (배치 처리, 키보드 설정 등) — GS인증은 기존 기능 완성도 평가
- 크로스 플랫폼 지원 — Windows 전용 WinForms 앱
- 대규모 리팩토링 (MVVM 전환 등) — 기능 변경 없이 품질만 개선
- 상용 라이선스 시스템 전면 교체 — 현재 MAC 기반 인증 개선만
- 단위 테스트 프레임워크 도입 — 인증 평가에서 테스트 코드 자체 미평가
- 4K 60fps / 다국어 지원 — 인증 범위 밖

## Context

- **기술 스택**: C# / .NET 8.0 / WinForms / OpenCvSharp4 v4.11.0 / FFMpegCore v5.1.0 / Newtonsoft.Json v13.0.3 / Serilog v4.2
- **코드 규모**: MainForm.cs 약 3,500+ 줄 단일 클래스 + Services/Models/Helpers/Theme 계층 (총 7,616 LOC C#)
- **신규 파일 (v1.0 누적)**: `Helpers/SecurityHelper.cs` (PBKDF2), `Helpers/PathValidator.cs` (트래버설 방지), `Services/HmacChainSink.cs` (HMAC 체인), `Services/HmacKeyProvider.cs` (키 관리), `Services/LogIntegrityVerifier.cs`, `Services/SettingsService.cs` (온보딩 영속화), `Forms/OnboardingForm.cs` (시작 가이드), `installer/build-installer.ps1` (인스톨러 자동화)
- **로그 위치**: `%LOCALAPPDATA%\ANNA\ASLT\logs\ASLT-yyyy-MM-dd.log` (HMAC 체인)
- **인스톨러 출력**: `installer/Output/ASLT-Setup-v1.0.2.exe`
- **알려진 이슈**: 일부 AVI 파일 cold-start 첫 로드 재생 불가 (process warm-up 후 정상). HumanUAT 일부 미완 항목.

## Constraints

- **Tech stack**: C# .NET 8.0 WinForms 유지 — 기존 코드 기반 개선만
- **Certification**: ISO/IEC 25023 8대 품질 특성 모두 충족 필요
- **Security**: KISA 가이드 준수 — SHA-256 이상 단방향 암호화 + Salt
- **Defects**: Critical/High 등급 결함 0건 필수 (Medium 이하 최소화)
- **Documentation**: 제품 설명서 + 사용자 취급 설명서 필요 (코드와 동작 일치) — DEFERRED to next milestone

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 기존 기능만 개선, 새 기능 추가 안 함 | GS인증은 기존 기능 완성도 평가 | ✓ Good — 결함 17건 일괄 처리 + 1.0.2 안정화 완료 |
| 대규모 아키텍처 리팩토링 제외 | 기능 변경 리스크 최소화 | ✓ Good — MainForm 단일 클래스 유지하면서 분기별 fix 성공 |
| KISA 가이드 기반 보안 강화 | GS인증 보안성 항목 충족 | ✓ Good — PBKDF2-HMAC-SHA256 (310k iter) + 경로 트래버설 방지 적용 |
| Static LogService + Serilog daily rotation + [AUDIT] prefix | 단순/관측 가능성/감사 추적 | ✓ Good — Phase 5.6 에서 13종 감사 이벤트로 자연 확장 |
| HMAC 체인 무결성 (HmacChainSink) | 로그 변조 검출 — KISA 보안성 강화 | ✓ Good — cross-day chain continuity + recovery 경로까지 안정화 |
| ChangeBoxIdOnly: Waypoint.ObjectId 자동 변경 안 함 | 사용자 의도 (선택 박스만 변경) — 분리 시나리오 허용 | ✓ Good (NEW-05 locked) — v1.0.2 시도 후 NEW-05 재확인 |
| Waypoint 삭제 atomic Undo (composite action) | 박스+Waypoint 단일 Ctrl+Z 복원, double-restore 방지 | ✓ Good (v1.0.2) |
| 자동재생 first-paint handshake (RELI-06) | cold-decoder seek 폭주 차단 | ✓ Good — finally 블록 safety net 추가 (v1.0.2) |
| AVI sequential read seek-skip | OpenCV/FFmpeg seek 호환성 | ⚠ 일부 파일은 여전히 cold-start 실패 — 추후 정밀 조사 |
| Phase 6 deferred at v1.0 close | 사용자 의사결정 — DOC 작업은 별도 milestone | ⚠ 인증 신청 전 처리 필요 |
| Inno Setup 6 + AppId pinned | in-place 1.0.x 업그레이드 + 클린 언인스톨 | ✓ Good — 1.0.0 → 1.0.1 → 1.0.2 가능 |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd:transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd:complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-28 after v1.0 milestone completion*
