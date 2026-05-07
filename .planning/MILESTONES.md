# Milestones — ASLT (ANNA Synthetic data Labeling Toolkit)

> 이 파일은 shipped 마일스톤의 chronological log 다.
> 상세는 각 milestone 의 `milestones/v[X.Y]-ROADMAP.md` 참조.

---

## v1.0 — GS인증 1등급 결함수정 사이클 (2026-03-16 → 2026-04-28)

**Status:** ✅ SHIPPED 2026-04-28
**Final Build:** ASLT-Setup-v1.0.2.exe (98.19 MB)
**Phases:** 1-5.6 (6 통합 phase + 2 decimal)
**Plans:** 18 / 19 (Phase 5.5-02 코드 반영, SUMMARY 미작성; Phase 6 미실행)
**Timeline:** 약 6주
**Commits:** 130+ on main

### Delivered

C# WinForms 라벨링 도구의 GS인증 1등급(ISO/IEC 25023 8대 품질 특성) 통과를 목표로 한 품질 개선 사이클. 기존 기능 유지 + 결함 0건 달성을 우선했다. KTC 1차 결함보고서 + 자체 QA 발견 결함 17건을 Phase 5.6 결함수정에서 일괄 처리하고 v1.0.2 인스톨러까지 산출.

### Key Accomplishments

1. **로그 인프라** — Serilog 기반 파일 로그 + HMAC-SHA256 무결성 체인 + 13종 감사 이벤트 (Phase 1, 5.6)
2. **안정성 기반** — 전역 예외 처리 + CancellationToken + 타이머 누수 제거 + Undo 스택 상한 (Phase 2)
3. **기능 정확성 + KISA 보안** — Vehicle 드롭다운 + COCO 타임스탬프 + bbox 클램핑 + PBKDF2-HMAC-SHA256 (310k iter) + 경로 트래버설 방지 (Phase 3)
4. **성능 + 사용성** — bbox 조회 O(n)→O(1) + 툴팁 + 미저장 경고 + 단축키 정리 + 수동 추적 (Phase 4)
5. **이식성** — Inno Setup 6 self-contained 인스톨러 + Windows 10/11 클린 환경 + 클린 언인스톨 (Phase 5)
6. **결함수정 17건** — ID 관리 재설계 (NEW-01~07) + Waypoint 동반 삭제 + 영상 미로드 크래시 가드 + 온보딩 가이드 + 한국어 메시지 + Waypoint 일괄 삭제 (Phase 5.6)
7. **v1.0.2 추가 보완** — 가이드 켜기 헤더 버튼 + Waypoint 삭제 atomic Undo + AVI 코덱 호환 안전망 + AOLT 잔재 ASLT 통일

### Known Gaps (deferred to next milestone)

| ID | Description | Reason |
|----|-------------|--------|
| **DOC-01** | 제품설명서 작성 (버전, 연동 제품 정보, 시스템 요구사항) | Phase 6 미실행 — 사용자 의사결정으로 v1.0 종료 시점 deferred |
| **DOC-02** | 사용자취급설명서 작성 (모든 기능, 입력값 유효 범위, 오류 메시지) | 동상 |
| **DOC-03** | 프로그램 내 버전 정보와 문서 버전 일치 | 문서 작성 후 검증 필요 |
| **USAB-05** | Undo/Redo 가능 여부 버튼 활성/비활성 시각 표시 | 코드상 N/A — UI 버튼 미존재 (키보드 단축키만 제공). UI 보강 시 재검토 |

> **인증 영향**: DOC-01/02/03 는 GS인증 1등급의 일반적 요구사항. 미완료 상태에서는 인증 신청 불가. 별도 milestone 또는 인증 신청 직전 작업으로 이월.

### Tech Debt

- Phase 5.5-02 코드 반영(commit `f58159b`)이나 SUMMARY.md 미작성 — retrospective 정리 시 보강
- 일부 AVI 파일 cold-start 첫 로드 재생 불가 보고 — paint handshake safety net + sequential read seek-skip + cold-decoder retry 로 완화. 그러나 그 특정 파일은 process warm-up 후에만 정상. 코덱별 정밀 조사는 향후 보강 (재현 데이터 부족)
- HumanUAT 일부 항목 in-progress 상태 — 추후 추적

### Archives

- [v1.0-ROADMAP.md](milestones/v1.0-ROADMAP.md) — phase 상세
- [v1.0-REQUIREMENTS.md](milestones/v1.0-REQUIREMENTS.md) — 43개 v1 요구사항 final status

### Quick Tasks

| # | Description | Date | Commit |
|---|-------------|------|--------|
| 260421-mzz | Codex 교차검증 후속 — installer uninstall + build.bat 내구성 | 2026-04-21 | ef84820 |
| 260427-eyf | Installer 빌드 자동화 + 1.0.0 → 1.0.1 bump | 2026-04-27 | dbe7a84 |

---

## v1.0.3 — JSON 저장 결함 수정 (KTC 2차) (2026-05-06)

**Status:** ✅ SHIPPED 2026-05-06
**Final Build:** ASLT-Setup-v1.0.3.exe (98.19 MB, SHA256 `2072B5B54AA1417DAFBD91219DFF3B8A7923B67987D1E51559DF06CB66E61698` — 4차 final, 2026-05-07 10:38:46)
**Phases:** 7 (single phase)
**Plans:** 3 / 3 (07-01, 07-02, 07-03)
**Timeline:** 단일 사이클 (2026-05-06)
**Commits:** 14 on main (3 source + 11 docs/chore)
**Files modified:** 15 (Forms/MainForm.cs, Forms/AboutForm.cs, Services/LogService.cs + 12 planning docs)
**LOC:** 6,655 C# (Forms/Services/Helpers/Models/Theme)

### Delivered

KTC 2차 결함보고서 JSON 저장 결함 2건 (DF-2-05, DF-2-06) 0건화 + v1.0.3 인스톨러 production-ready. v1.0 의 Phase 5.6 (KTC 1차 결함수정) 패턴과 일관되게 별도 milestone 으로 처리하여 인증 감사 트레이스 유지. UAT 15/15 시나리오 통과 (사용자 보고).

### Key Accomplishments

1. **FUNC-11 (DF-2-05)** — `SetMode(DrawMode)` 헬퍼 도입으로 모드 상태 + 좌측 버튼 시각 + 커서 원자적 동기화. 4개 호출 지점 통일 (Phase 7-01, commit `18f3126`)
2. **FUNC-12 (DF-2-06)** — `btnExportJson_Click` 1-BBOX 가드 추가. `Services/JsonService.cs` 무수정 (D-05 락) — UI-side guard 패턴, round-trip 진입에서 차단 (Phase 7-02, commit `9eb6940`)
3. **v1.0.3 인스톨러 production-ready** — Inno Setup 6 self-contained, AppId pinned, 1.0.2 → 1.0.3 in-place 업그레이드 검증 통과 (Phase 7-03)
4. **UAT 15/15 시나리오 통과** — FUNC-11/12 결함 비재현 + 회귀 0 + in-place 업그레이드 (옵션 C) (사용자 보고)
5. **사용자 흐름 개선 (out-of-scope but in-binary)** — 정보 팝업 Entry/Exit Shift 변형 표기 (commit `8a22445`), 로그 보존 30일 → 180일 (commit `df890bd`), 시작 가이드(OnboardingForm) Entry/Exit Shift 변형 표기 (commit `f719195`, v1.0.3 re-open + 4차 빌드 cycle)

### Known Gaps (deferred to next milestone)

| ID | Description | Reason |
|----|-------------|--------|
| **RELI-NEW-01** | `btnExportJson_Click` 정상 저장 경로의 `LogService.AuditJsonSave` 호출 회귀 테스트 + 사용자 흐름 audit log E2E 검증 | UAT JSON 저장 활동의 [AUDIT] 엔트리가 audit log 에 부재 (HMAC chain 자체는 정상). Phase 7 결함 0건화 자체는 primary evidence 3계층 (UAT + 코드 + 빌드) 으로 입증됨. KTC 인증 신청 직전에 closure 필요 |
| **DOC-01/02/03** | 제품설명서, 사용자취급설명서, 버전 일치 검증 | v1.0 부터 이월 — 인증 신청 전 필수 |
| **USAB-05** | Undo/Redo UI 버튼 결정 (UI 신규 추가 vs 영구 N/A) | v1.0 부터 이월 |
| **AVI cold-start** | 일부 AVI 파일 첫 로드 재생 불가 | v1.0 부터 이월 — 재현 데이터 확보 필요 |
| **HumanUAT 잔여** | v1.0 cycle 의 in-progress 항목들 | v1.0 부터 이월 |

### Tech Debt

- 3차 + 4차 빌드 cycle (UAT 도중 popup/retention 변경, closeout 직후 OnboardingForm 변경) — 향후 milestone 에서 UAT 단계 진입 전 source freeze 정책 명시 필요. closeout 후 re-open 은 tag 미배포 상태에서만 안전 (배포 후엔 v1.0.4 patch 로 처리해야 함).
- ISCC 컴파일러 deprecation 경고 (`Architecture identifier "x64"` → 권장 `x64compatible`) — `installer/ASLT-Setup.iss` 정리 보류
- `<Nullable>disable</Nullable>` 환경에서 `CS8632` 경고 다수 — annotations 무해, 별도 처리 안 함

### Archives

- [v1.0.3-ROADMAP.md](milestones/v1.0.3-ROADMAP.md) — phase 상세 + key decisions
- [v1.0.3-REQUIREMENTS.md](milestones/v1.0.3-REQUIREMENTS.md) — 2개 v1.0.3 requirements final status

### Quick Tasks

| # | Description | Date | Commit |
|---|-------------|------|--------|
| (없음) | v1.0.3 사이클 내 quick task 없음 — Phase 7 단일 phase 에 집중 | - | - |

---
