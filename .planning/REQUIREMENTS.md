# Requirements — v1.0.3 (JSON 저장 결함 수정)

**Milestone:** v1.0.3 — JSON 저장 결함 수정 (KTC 2차 결함보고서)
**Defined:** 2026-05-06
**Status:** Active

> v1.0 의 archived requirements (43개) 는 [milestones/v1.0-REQUIREMENTS.md](milestones/v1.0-REQUIREMENTS.md) 참조.
> 이 파일은 v1.0.3 milestone 에서 신규로 정의된 요구사항만 포함한다 (기존 FUNC-* 카테고리 연장).

## Source

KTC 2차 결함보고서 — JSON 저장(`프로그램 > JSON저장`) 기능 검사에서 식별된 결함 2건. 모두 기능 적합성, 결함 정도: 중간.

## v1.0.3 Requirements

### 기능적합성 (Functional Suitability) — 신규

- [x] **FUNC-11**: 메뉴를 통해 JSON 저장을 호출했을 때 '전체선택(1번)' 모드가 활성화되면, 좌측 모드 버튼의 시각 표시(파란색 등 활성 상태) 가 '전체선택' 으로 동기화되어야 한다. 편집모드(0번) 가 활성화된 것처럼 보이는 시각 불일치를 제거한다. _(KTC: DF-2-05)_ ✅ 2026-05-06 (Phase 7-01, commit 18f3126)

- [ ] **FUNC-12**: BBOX 가 1개만 생성된 상태에서 JSON 저장을 시도하는 경우, Entry 와 Exit 가 동일한 프레임으로 설정되어 Waypoint 가 생성되는 동작을 차단해야 한다. 사용자에게 'Exit 는 Entry 이후 프레임에서 설정' 안내 메시지를 일관되게 제공하여 저장 흐름과 사전 검증 메시지가 모순되지 않도록 한다. _(KTC: DF-2-06)_

## Acceptance Criteria

각 요구사항의 검증은 phase 의 PLAN 에서 세부 시나리오로 분해된다. 최소 검증 기준:

**FUNC-11**
- [ ] 메뉴 → JSON 저장 시 '전체선택' 모드가 내부적으로 활성화됨 (현재 동작)
- [ ] 좌측 모드 버튼의 시각 표시가 '전체선택' 활성 상태로 갱신됨 (수정)
- [ ] 메뉴 호출 후 사용자가 다른 모드를 누르지 않은 상태에서 좌측 버튼을 봤을 때 0번 (편집) 활성 표시가 남아 있지 않음
- [ ] 회귀: 좌측 버튼 클릭으로 모드 전환 시에도 표시가 정확히 따라감

**FUNC-12**
- [ ] BBOX 1개만 생성한 상태에서 JSON 저장 시 Entry == Exit 인 Waypoint 가 생성되지 않음
- [ ] 사전 안내 메시지 ('Exit 는 Entry 이후 프레임에서 설정') 와 저장 흐름이 모순 없음 (저장 시도 → 안내 또는 저장 거부)
- [ ] 회귀: 정상 케이스 (BBOX ≥ 2개, 서로 다른 프레임에서 Entry/Exit) 에서 Waypoint 생성/저장 정상 동작

## Future Requirements (다음 milestone 후보)

- DOC-01/02/03 — 제품설명서, 사용자취급설명서, 버전 일치 검증 (인증 신청 전 필수)
- USAB-05 — Undo/Redo UI 버튼 결정 (UI 신규 추가 vs 영구 N/A)
- AVI cold-start 정밀 조사 — 재현 데이터 확보 후 디코더 워밍업 fix
- HumanUAT 잔여 항목 정리

## Out of Scope (v1.0.3)

- 새 기능 추가 — v1.0 에서 정한 "기존 기능만 개선" 원칙 유지
- JSON 저장 외 다른 결함 — 본 milestone 은 KTC 2차 보고서 중 JSON 저장 2건만 처리
- UI 리팩토링 — 모드 버튼 상태 동기화 외에 시각 디자인 변경 없음
- 인스톨러 형식/Inno Setup 변경 — 1.0.2 → 1.0.3 in-place 업그레이드만 수행

## Traceability

| REQ-ID | KTC ID | Phase | Status |
|--------|--------|-------|--------|
| FUNC-11 | DF-2-05 | Phase 7 (Plan 07-01) | ✅ Completed 2026-05-06 |
| FUNC-12 | DF-2-06 | Phase 7 | Active |

---
*Last updated: 2026-05-06 — v1.0.3 milestone 시작*
