# Roadmap: ASLT (ANNA Synthetic data Labeling Toolkit)

## Milestones

- ✅ **v1.0 GS인증 결함수정 사이클** — Phases 1-5.6 (shipped 2026-04-28) — [archive](milestones/v1.0-ROADMAP.md)
- ✅ **v1.0.3 JSON 저장 결함 수정 (KTC 2차)** — Phase 7 (shipped 2026-05-06) — [archive](milestones/v1.0.3-ROADMAP.md)
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

<details>
<summary>✅ v1.0.3 (Phase 7) — SHIPPED 2026-05-06</summary>

- [x] Phase 7: JSON 저장 결함수정 (KTC 2차) (3/3 plans) — completed 2026-05-06
  - [x] 07-01: FUNC-11 / DF-2-05 SetMode 헬퍼 (commit `18f3126`)
  - [x] 07-02: FUNC-12 / DF-2-06 1-BBOX 가드 (commit `9eb6940`)
  - [x] 07-03: 회귀 UAT 15/15 + 1.0.3 인스톨러 (5차 final, SHA256 `D139CD90...`) + in-place 업그레이드 검증 + OnboardingForm 일관성 patch (`f719195`) + FUNC-12 보강 (`29be68e`, close/switch 자동 저장 path)

상세: [milestones/v1.0.3-ROADMAP.md](milestones/v1.0.3-ROADMAP.md)

</details>

### 📋 Next Milestone (TBD)

> 시작 시 `/gsd:new-milestone` — fresh requirements 정의

**Candidate phases:**
- [ ] Phase 6: 문서화 (DOC-01/02/03 — 인증 신청 전 필수)
- [ ] Phase 8: UI/UX 보강 (USAB-05 재검토)
- [ ] Phase 9: AVI cold-start 정밀 조사 (재현 데이터 확보 후)
- [ ] Phase 10: HumanUAT 잔여 항목 정리
- [ ] **Phase 11 (TBD): RELI-NEW-01** — `LogService.AuditJsonSave` 호출 회귀 테스트 + audit log E2E 검증 (Phase 7-03 follow-up — KTC 인증 신청 직전 closure 필수)

## Progress

| Milestone | Phases | Status | Date |
|-----------|--------|--------|------|
| v1.0 GS인증 결함수정 | 1-5.6 (8 phases) | ✅ Shipped | 2026-04-28 |
| v1.0.3 JSON 저장 결함수정 (KTC 2차) | Phase 7 (3 plans, UAT 15/15) | ✅ Shipped | 2026-05-06 |
| (next) | 6, 8-11 (TBD) | 📋 Planned | - |
