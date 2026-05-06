# Roadmap: ASLT (ANNA Synthetic data Labeling Toolkit)

## Milestones

- ✅ **v1.0 GS인증 결함수정 사이클** — Phases 1-5.6 (shipped 2026-04-28) — [archive](milestones/v1.0-ROADMAP.md)
- ✅ **v1.0.3 JSON 저장 결함 수정 (KTC 2차)** — Phase 7 complete 2026-05-06 (UAT 15/15, awaiting milestone closeout)
- 📋 **v1.x / next** — Planned (RELI-NEW-01 + DOC-01/02/03 + USAB-05 + AVI cold-start 등)

## Phases

<details>
<summary>✅ v1.0 (Phases 1-5.6) — SHIPPED 2026-04-28</summary>

- [x] Phase 1: 로그 인프라 (1/1 plans) — completed 2026-04-16
- [x] Phase 2: 안정성 기반 (2/2 plans) — completed 2026-04-16
- [x] Phase 3: 기능 정확성 + 보안 (4/4 plans) — completed 2026-04-16
- [x] Phase 4: 성능 + 사용성 (3/3 plans) — completed 2026-04-17
- [x] Phase 5: 이식성 (2/2 plans) — completed 2026-04-17
- [x] Phase 5.5: 기능 보정 + 안정화 (2/2 plans, 1 SUMMARY documented + 1 code-only)
- [x] Phase 5.6: 결함수정 INSERTED (5/5 plans)
- [ ] Phase 6: 문서화 — **DEFERRED** to 다음 milestone (DOC-01/02/03)

상세: [milestones/v1.0-ROADMAP.md](milestones/v1.0-ROADMAP.md)

</details>

### ✅ v1.0.3 — JSON 저장 결함 수정 (KTC 2차)

**Goal:** KTC 2차 결함보고서에서 식별된 JSON 저장 관련 결함 2건을 수정하여 인증 신청 전 결함 0건 상태를 유지한다.

**Phase 7: JSON 저장 결함수정 (KTC 2차)** — ✅ Complete 2026-05-06
- [x] Goal: DF-2-05, DF-2-06 두 결함을 정확히 재현 → 근본 원인 분석 → 수정 → 회귀 테스트 후 v1.0.3 인스톨러 빌드
- [x] Requirements: FUNC-11 (DF-2-05) ✅, FUNC-12 (DF-2-06) ✅
- [x] Plans: 3/3 plans complete
  - [x] 07-01-PLAN.md — FUNC-11 (DF-2-05): SetMode 헬퍼 도입 + 모드 버튼 시각 동기화 (completed 2026-05-06, commit 18f3126)
  - [x] 07-02-PLAN.md — FUNC-12 (DF-2-06): btnExportJson_Click 1-BBOX 가드 추가 (completed 2026-05-06, commit 9eb6940)
  - [x] 07-03-PLAN.md — 회귀 UAT 15/15 + ASLT-Setup-v1.0.3.exe 빌드 (3차 final, SHA256 9ECAF3C3...) + 1.0.2 → 1.0.3 in-place 업그레이드 검증 (completed 2026-05-06)
- [x] Success criteria:
  1. [x] FUNC-11 — JSON 저장 메뉴 호출 시 좌측 모드 버튼이 '전체선택' 활성 표시로 동기화 (편집모드 잔존 표시 0건) — UAT A-3
  2. [x] FUNC-12 — BBOX 1개 상태에서 JSON 저장 시 Entry==Exit Waypoint 생성 0건, 안내 메시지와 일관된 흐름 — UAT B-1
  3. [x] 회귀: v1.0.2 에서 통과한 JSON 저장 시나리오 (타임스탬프, 카테고리 매핑, BBOX ≥ 2개 정상 케이스) 미회귀 — UAT B-2/B-3/B-4/C-1/C-2
  4. [x] v1.0.3 인스톨러 빌드 성공 + 1.0.2 → 1.0.3 in-place 업그레이드 검증 통과 — Task 1 (3차 빌드) + UAT U-2/U-3/U-4/U-5

**Audit Trail Gap (follow-up to next milestone):** UAT JSON 저장 활동의 [AUDIT] 엔트리가 audit log 에 부재 (HMAC chain 자체는 정상). Phase 7 결함 0건화 자체는 primary evidence (UAT + 코드 + 빌드) 로 입증됨. Secondary evidence gap 은 **RELI-NEW-01** 으로 다음 milestone 이월. 상세는 [.planning/phases/07-json-저장-결함수정/07-03-SUMMARY.md](.planning/phases/07-json-저장-결함수정/07-03-SUMMARY.md) 참조.

### 📋 Next Milestone (TBD — v1.0.3 종료 후 정의)

> 시작 시 `/gsd:new-milestone` — fresh requirements 정의

**Candidate phases:**
- [ ] Phase 6: 문서화 (DOC-01/02/03 — 인증 신청 전 필수)
- [ ] Phase 8: UI/UX 보강 (USAB-05 재검토)
- [ ] Phase 9: AVI cold-start 정밀 조사 (재현 데이터 확보 후)
- [ ] Phase 10: HumanUAT 잔여 항목 정리
- [ ] **Phase 11 (TBD): RELI-NEW-01** — Audit log primary evidence gap closure (`btnExportJson_Click` 정상 저장 경로의 `LogService.AuditJsonSave` 호출 회귀 테스트 + E2E 검증). Phase 7-03 에서 발견 + 이월. KTC 인증 신청 직전에 closure 필요.

## Progress

| Milestone | Phases | Status | Date |
|-----------|--------|--------|------|
| v1.0 GS인증 결함수정 | 1-5.6 (8 phases) | ✅ Shipped | 2026-04-28 |
| v1.0.3 JSON 저장 결함수정 (KTC 2차) | Phase 7 (3/3 plans) ✅ Complete + UAT 15/15 | ✅ Complete | 2026-05-06 |
| (next) | 6, 8-11 (TBD) | 📋 Planned | - |
