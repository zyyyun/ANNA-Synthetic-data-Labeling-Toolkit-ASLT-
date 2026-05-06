# Phase 7: JSON 저장 결함수정 (KTC 2차) - Context

**Gathered:** 2026-05-06
**Status:** Ready for planning

<domain>
## Phase Boundary

KTC 2차 결함보고서에서 식별된 JSON 저장 관련 결함 2건을 수정한다:

- **FUNC-11 (DF-2-05)** — JSON 저장 시 모드 버튼의 시각 표시(좌측 `btnSelectAll`/`btnEdit` BackColor) 가 내부 모드 상태와 동기화되지 않는 결함
- **FUNC-12 (DF-2-06)** — BBOX 가 1개만 존재하는 상태에서 JSON 저장이 시도될 때 Entry==Exit Waypoint 가 생성되어 사전 안내 메시지(수동 Exit 설정 시 표시) 와 흐름이 모순되는 결함

수정 + 회귀 테스트 + 1.0.2 → 1.0.3 in-place 업그레이드 검증까지 포함. 새 기능 추가 없음.

</domain>

<decisions>
## Implementation Decisions

### FUNC-11 (DF-2-05) — 모드 버튼 시각 동기화

- **D-01:** `private void SetMode(DrawMode mode)` 헬퍼를 신규 도입하여 `currentMode` 필드 + `btnSelectAll`/`btnEdit` 의 `BackColor` + `pictureBoxVideo.Cursor` 를 한 곳에서 처리한다.
- **D-02:** 기존 모드 변경 지점 모두 헬퍼 호출로 통일:
  - `btnSelectAll_Click` ([Forms/MainForm.cs:1582](Forms/MainForm.cs:1582)) → `SetMode(DrawMode.Select)`
  - `btnEdit_Click` ([Forms/MainForm.cs:1590](Forms/MainForm.cs:1590)) → `SetMode(DrawMode.Draw)`
  - LoadLabelingData reset 블록 ([Forms/MainForm.cs:675](Forms/MainForm.cs:675)) → `SetMode(DrawMode.Select)`
  - 필드 기본값 ([Forms/MainForm.cs:61](Forms/MainForm.cs:61)) 은 `DrawMode.Select` 그대로, 다만 폼 OnLoad 또는 InitializeMainForm 시 `SetMode(DrawMode.Select)` 한 번 호출하여 초기 시각도 일치시킨다.
- **D-03:** 단축키 D1/D2 ([Forms/MainForm.cs:2913-2914](Forms/MainForm.cs:2913)) 는 기존처럼 `btnSelectAll_Click` / `btnEdit_Click` 호출 유지 (헬퍼가 자동 적용됨).
- **D-04:** ARGB 상수 (`Color.FromArgb(59, 130, 246)` 활성, `Color.FromArgb(62, 62, 66)` 비활성) 는 헬퍼 내부에 묶어 두 호출지점 중복 제거.

### FUNC-12 (DF-2-06) — BBOX 1개 시 저장 차단

- **D-05:** **저장 자체를 막고 안내 메시지를 표시한다.** Export 단계 silent skip 이나 Import 측 패치는 채택하지 않음 — 기존 수동 Exit 설정 안내 메시지와 일관된 사용자 흐름을 유지하기 위함.
- **D-06:** 검사 위치는 `btnExportJson_Click` ([Forms/MainForm.cs:716](Forms/MainForm.cs:716)) — 기존 BBOX/Waypoint 0개 체크(line 729) 직후, 저장 다이얼로그 표시 직전.
- **D-07:** 검사 로직: `boundingBoxes` 를 `(Label, GetBoxId(box))` 로 group → 각 그룹 중 sameObjectBoxes 가 1개이면서 매칭되는 Waypoint 가 없는 경우를 "Entry/Exit 미완성 트랙" 으로 판정. 하나라도 발견되면 저장 차단.
- **D-08:** 사용자 메시지: 기존 수동 Exit 안내 메시지와 동일/유사 톤. 예시 — `"Entry 이후 프레임에서 Exit 를 설정해주세요. 하나의 BBOX 만 존재하는 객체가 있어 Waypoint 를 생성할 수 없습니다.\n\n해당 객체: [Label] [ID]"`. 정확한 문구는 plan 단계에서 v1.0 기존 메시지 톤 매칭하여 확정.
- **D-09:** "정상 저장" 케이스(모든 객체에 ≥ 2 BBOX 또는 명시적 Waypoint) 는 동작 변경 없음 — 회귀 0 보장.
- **D-10:** Phase 5.6 의 DF-1-04 "BBOX/Waypoint 0개" 체크 흐름과 같은 위치에서 분기 추가, 동일 다이얼로그 톤 유지.

### Phase 7 구조

- **D-11:** 3개 plan 으로 분할:
  1. **Plan 07-01: FUNC-11 (DF-2-05) — SetMode 헬퍼 + 시각 동기화**
  2. **Plan 07-02: FUNC-12 (DF-2-06) — 저장 시 1-BBOX 가드**
  3. **Plan 07-03: 회귀 테스트 + ASLT-Setup-v1.0.3.exe 빌드 + 1.0.2 → 1.0.3 in-place 업그레이드 검증**
- **D-12:** 인스톨러 빌드는 `installer/build-installer.ps1` 기존 자동화 사용. AppId pinned 유지. 버전은 이미 1.0.3 으로 bump 되어 있음 (commit `c5d8102`) — 그대로 사용.

### Claude's Discretion

- 메시지 박스 정확 문구 (D-08) — v1.0 톤 매칭 후 plan 단계에서 final.
- SetMode 헬퍼의 정확한 위치(`#region UI State` 인근 vs 기존 click 핸들러 인근) 는 plan 에서 결정.
- 회귀 테스트 시나리오 분해는 planner 가 acceptance criteria 기반으로 작성.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Roadmap
- [.planning/REQUIREMENTS.md](.planning/REQUIREMENTS.md) — FUNC-11/FUNC-12 정의 + acceptance criteria
- [.planning/ROADMAP.md](.planning/ROADMAP.md) — Phase 7 goal + success criteria
- [.planning/PROJECT.md](.planning/PROJECT.md) — Core value, constraints, Out of scope (v1.0.3)

### KTC Defect Report Trace
- [.planning/DEFECTS-INBOX.md](.planning/DEFECTS-INBOX.md) — KTC 1차 결함 취합본 (참고용 패턴, 2차는 별도)
- KTC 2차 결함보고서 — DF-2-05/DF-2-06 원문 (사용자 제공 스크린샷 — 본 CONTEXT 의 Phase Boundary 섹션에 핵심 내용 옮겨 적음)

### Source Files (수정 대상)
- [Forms/MainForm.cs](Forms/MainForm.cs) — UI/이벤트 핸들러, JSON 저장 진입점 (`btnExportJson_Click`, line 716), 모드 핸들러 (line 1582/1590), LoadLabelingData reset 블록 (line 675)
- [Services/JsonService.cs](Services/JsonService.cs) — Export/Import 로직, Waypoint 생성 (line 342-371), TrackInfo entry/exit 결정 (line 533-552)

### v1.0 Pattern Reference (Phase 5.6 결함수정)
- [.planning/milestones/v1.0-ROADMAP.md](.planning/milestones/v1.0-ROADMAP.md) — Phase 5.6 결함수정 패턴 (KTC 1차) — DF-1-04 의 0-BBOX 가드 위치/톤이 본 phase 의 1-BBOX 가드 모범 예
- [.planning/milestones/v1.0-REQUIREMENTS.md](.planning/milestones/v1.0-REQUIREMENTS.md) — 기존 FUNC-* 카테고리 (FUNC-01~10) — 본 phase 는 FUNC-11/12 추가

### Build/Install
- `installer/build-installer.ps1` — 인스톨러 자동화 스크립트 (AppId pinned, in-place 업그레이드 보장)
- `installer/Output/` — 산출물 위치

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`btnSelectAll_Click` / `btnEdit_Click`** ([Forms/MainForm.cs:1582-1597](Forms/MainForm.cs:1582)): 기존 모드 전환 로직(상태 + BackColor + Cursor) 완전 셋이 이미 한 곳에 모여 있어 `SetMode` 헬퍼 추출이 쉽다. 단순한 method extract refactor.
- **DF-1-04 0-BBOX 가드 패턴** ([Forms/MainForm.cs:729-768](Forms/MainForm.cs:729)): "저장 가능 여부 사전 검사 + 다이얼로그 + early return" 흐름. FUNC-12 의 1-BBOX 가드는 이 패턴을 그대로 따른다.
- **`GetBoxId(box)`** (BoundingBox helper): Person/Vehicle/Event 별 ID 통합 조회 — sameObjectBoxes 그룹핑 키로 재사용.
- **`PathValidator.IsPathSafe`** ([Services/SaveCurrentLabelingData](Forms/MainForm.cs:849)): 본 phase 와 직접 관련 없으나 저장 흐름의 일부 — 회귀 테스트 시 함께 동작 확인.

### Established Patterns

- **단일 핸들러로 두 버튼 연결**: `btnExportJson` (헤더) + `btnExportJsonInLabels` (라벨 그룹박스) 모두 `btnExportJson_Click` 에 연결 ([MainForm.Designer.cs:138, 721](Forms/MainForm.Designer.cs:138)) — 가드 추가 시 한 곳만 수정하면 양쪽 진입점 모두 보호됨.
- **시각 표시 ARGB 상수**: 활성 `(59, 130, 246)`, 비활성 `(62, 62, 66)` — 코드 전반에 inline 으로 등장. SetMode 헬퍼 도입 시 헬퍼 내부 상수화 가능.
- **사용자 안내 메시지 톤 (한국어)**: `MessageBox.Show("...해주세요.", "...", MessageBoxButtons.OK, MessageBoxIcon.Warning)` 형식. v1.0 의 기존 메시지와 톤 일치 필수 (USAB-03 / RELI-05 패턴).

### Integration Points

- **JSON 저장 → 자동 reload 사이클** ([MainForm.cs:796-798](Forms/MainForm.cs:796)): 저장 성공 후 `LoadLabelingData` 호출 → line 675 reset 트리거. SetMode 헬퍼 적용 후 이 사이클에서도 시각이 정상 동기화되어 FUNC-11 의 정상 동작이 보장됨.
- **JsonService Import 측 Waypoint 생성** ([Services/JsonService.cs:342-371](Services/JsonService.cs:342)): FUNC-12 를 저장 측에서 차단하므로 Import 측은 변경하지 않음. 단, 회귀 테스트에서 기존 정상 JSON(Entry≠Exit) 의 Waypoint 복원이 깨지지 않는지 확인.

</code_context>

<specifics>
## Specific Ideas

- **사용자 흐름 일관성** — 수동으로 Exit 를 Entry 와 같은 프레임에 잡으려 할 때 표시되는 안내 메시지를 저장 시 1-BBOX 케이스에서도 동일/유사 형태로 재사용. 사용자 입장에서 "왜 안 되는지" 가 즉시 이해되도록.
- **Phase 5.6 의 DF-1-04 패턴 재사용** — 가드 위치와 다이얼로그 톤은 v1.0 의 0-BBOX 케이스를 그대로 본떠야 retrospective 시 일관됨.

</specifics>

<deferred>
## Deferred Ideas

다음 milestone 으로 미룬 항목 (PROJECT.md Out of scope 와 일관):
- DOC-01/02/03 — 제품/사용자 설명서 + 버전 일치
- USAB-05 — Undo/Redo UI 버튼 결정
- AVI cold-start 정밀 조사
- HumanUAT 잔여 항목 정리

본 phase 의 scope creep 차단:
- JSON 저장 메뉴/버튼 자체의 UI 디자인 변경 — 본 phase 는 동작 정합성만 다룬다.
- Waypoint 데이터 모델 리팩토링 — 본 phase 는 Entry==Exit 가 생기지 않게 차단만 한다.
- 새로운 단축키/메뉴 추가 — Out of scope.

</deferred>

---

*Phase: 7-json-저장-결함수정*
*Context gathered: 2026-05-06*
