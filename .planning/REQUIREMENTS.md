# Requirements — v1.0.3 (JSON 저장 결함 수정)

**Milestone:** v1.0.3 — JSON 저장 결함 수정 (KTC 2차 결함보고서)
**Defined:** 2026-05-06
**Status:** ✅ Phase 7 Complete 2026-05-06 (awaiting milestone closeout)

> v1.0 의 archived requirements (43개) 는 [milestones/v1.0-REQUIREMENTS.md](milestones/v1.0-REQUIREMENTS.md) 참조.
> 이 파일은 v1.0.3 milestone 에서 신규로 정의된 요구사항만 포함한다 (기존 FUNC-* 카테고리 연장).

## Source

KTC 2차 결함보고서 — JSON 저장(`프로그램 > JSON저장`) 기능 검사에서 식별된 결함 2건. 모두 기능 적합성, 결함 정도: 중간.

## v1.0.3 Requirements

### 기능적합성 (Functional Suitability) — 신규

- [x] **FUNC-11**: 메뉴를 통해 JSON 저장을 호출했을 때 '전체선택(1번)' 모드가 활성화되면, 좌측 모드 버튼의 시각 표시(파란색 등 활성 상태) 가 '전체선택' 으로 동기화되어야 한다. 편집모드(0번) 가 활성화된 것처럼 보이는 시각 불일치를 제거한다. _(KTC: DF-2-05)_ ✅ 2026-05-06 (Phase 7-01, commit 18f3126)

- [x] **FUNC-12**: BBOX 가 1개만 생성된 상태에서 JSON 저장을 시도하는 경우, Entry 와 Exit 가 동일한 프레임으로 설정되어 Waypoint 가 생성되는 동작을 차단해야 한다. 사용자에게 'Exit 는 Entry 이후 프레임에서 설정' 안내 메시지를 일관되게 제공하여 저장 흐름과 사전 검증 메시지가 모순되지 않도록 한다. _(KTC: DF-2-06)_ ✅ 2026-05-06 (Phase 7-02, commit 9eb6940)

## Acceptance Criteria

각 요구사항의 검증은 phase 의 PLAN 에서 세부 시나리오로 분해된다. 최소 검증 기준:

**FUNC-11** ✅ 2026-05-06 (Phase 7-01 코드 fix commit 18f3126 + Phase 7-03 UAT A-1..A-5 pass)
- [x] 메뉴 → JSON 저장 시 '전체선택' 모드가 내부적으로 활성화됨 (현재 동작) — UAT A-3
- [x] 좌측 모드 버튼의 시각 표시가 '전체선택' 활성 상태로 갱신됨 (수정) — UAT A-3 (핵심 시나리오)
- [x] 메뉴 호출 후 사용자가 다른 모드를 누르지 않은 상태에서 좌측 버튼을 봤을 때 0번 (편집) 활성 표시가 남아 있지 않음 — UAT A-3
- [x] 회귀: 좌측 버튼 클릭으로 모드 전환 시에도 표시가 정확히 따라감 — UAT A-2 + A-4 (D1/D2 단축키 회귀 0)

**FUNC-12** ✅ 2026-05-06 (Phase 7-02 코드 fix commit 9eb6940 + Phase 7-03 UAT B-1..B-4 pass)
- [x] BBOX 1개만 생성한 상태에서 JSON 저장 시 Entry == Exit 인 Waypoint 가 생성되지 않음 — 가드가 저장 자체를 차단 — UAT B-1 (핵심 시나리오)
- [x] 사전 안내 메시지 ('Exit 는 Entry 이후 프레임에서 설정') 와 저장 흐름이 모순 없음 (저장 시도 → 안내 또는 저장 거부) — 메시지 톤 line 1104 와 일치 — UAT B-1 메시지 본문 검증
- [x] 회귀: 정상 케이스 (BBOX ≥ 2개, 서로 다른 프레임에서 Entry/Exit) 에서 Waypoint 생성/저장 정상 동작 — UAT B-2 + B-3 + B-4 + C-1 (다중 객체 회귀 0)

## Future Requirements (다음 milestone 후보)

- **RELI-NEW-01** _(Phase 7-03 발견, follow-up)_ — `btnExportJson_Click` 정상 저장 경로의 `LogService.AuditJsonSave` 호출 회귀 테스트 + 사용자 흐름 audit log E2E 검증 케이스 추가. UAT JSON 저장 활동의 [AUDIT] 엔트리가 audit log 에 부재 (HMAC chain 자체는 정상). KTC 인증 신청 직전에 closure 필요. 상세는 [phases/07-json-저장-결함수정/07-03-SUMMARY.md](phases/07-json-저장-결함수정/07-03-SUMMARY.md) "Audit Trail Gap" 섹션 참조.
- DOC-01/02/03 — 제품설명서, 사용자취급설명서, 버전 일치 검증 (인증 신청 전 필수)
- USAB-05 — Undo/Redo UI 버튼 결정 (UI 신규 추가 vs 영구 N/A)
- AVI cold-start 정밀 조사 — 재현 데이터 확보 후 디코더 워밍업 fix
- HumanUAT 잔여 항목 정리

## Out of Scope (v1.0.3)

- 새 기능 추가 — v1.0 에서 정한 "기존 기능만 개선" 원칙 유지
- JSON 저장 외 다른 결함 — 본 milestone 은 KTC 2차 보고서 중 JSON 저장 2건만 처리
- UI 리팩토링 — 모드 버튼 상태 동기화 외에 시각 디자인 변경 없음
- 인스톨러 형식/Inno Setup 변경 — 1.0.2 → 1.0.3 in-place 업그레이드만 수행

## Out-of-Scope but Applied (ship in v1.0.3 binary)

UAT 도중 사용자 발견 + 사용자 결정으로 동일 v1.0.3 binary 에 통합 빌드된 사용자 흐름 개선 사항 (REQUIREMENTS 카테고리 entry 등록은 안 함, 인증 신청 시 revisit 비용 최소화 목적):

- commit `8a22445` — `Forms/AboutForm.cs`: 정보 팝업 Entry/Exit 단축키 표기 Shift 변형 명시 (`E or 'Shift + E'`, `X or 'Shift + X'`) — UAT A-5 검증
- commit `df890bd` — `Services/LogService.cs`: 로그 보존 정책 30일 → 180일 (6개월). KTC 인증 감사 트레이스를 위한 보존 기간 확장. 동작 동일 (효과는 5/6개월 후 발현).

## Traceability

| REQ-ID | KTC ID | Phase | Status |
|--------|--------|-------|--------|
| FUNC-11 | DF-2-05 | Phase 7 (Plan 07-01) | ✅ Completed 2026-05-06 (commit 18f3126 + UAT A-1..A-5) |
| FUNC-12 | DF-2-06 | Phase 7 (Plan 07-02) | ✅ Completed 2026-05-06 (commit 9eb6940 + UAT B-1..B-4) |

---
*Last updated: 2026-05-06 — Phase 7 complete (UAT 15/15), v1.0.3 인스톨러 production-ready (SHA256 9ECAF3C3...)*
