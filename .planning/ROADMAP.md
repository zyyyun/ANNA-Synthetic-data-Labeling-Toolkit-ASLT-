# Roadmap: AOLT GS인증 1등급

## Overview

GS인증 1등급(ISO/IEC 25023 8대 품질 특성) 통과를 위한 품질 개선 로드맵. 기존 구현된 기능을 결함 없이 동작하도록 개선하되 새 기능은 추가하지 않는다. 로그 인프라를 먼저 구축하고(Phase 1), 안정성·기능 정확성·성능·설치 환경 순으로 개선한 뒤, 바이너리가 확정된 후 문서를 작성한다(Phase 6).

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: 로그 인프라** - 파일 기반 구조화 로그 및 감사 추적 시스템 구축 (completed 2026-04-16)
- [x] **Phase 2: 안정성 기반** - 비정상 종료·리소스 누수·레이스 컨디션 제거 (completed 2026-04-16)
- [ ] **Phase 3: 기능 정확성 + 보안** - 핵심 버그 수정, COCO 정합성, KISA 보안 강화
- [ ] **Phase 4: 성능 + 사용성** - 조회 최적화, UI 피드백 일관성 확보
- [ ] **Phase 5: 이식성** - 클린 환경 설치·실행·제거 보장
- [ ] **Phase 6: 문서화** - 잠긴 바이너리 기준 제품설명서·사용자취급설명서 작성

## Phase Details

### Phase 1: 로그 인프라
**Goal**: 애플리케이션 전반에 파일 기반 구조화 로그가 기록되고 감사 추적이 가능하다
**Depends on**: Nothing (first phase)
**Requirements**: MAINT-01, SECU-02, SECU-03
**Success Criteria** (what must be TRUE):
  1. 앱 시작·종료·저장·라이선스 오류 등 주요 이벤트가 날짜별 로그 파일에 기록된다
  2. 로그 레벨(Debug/Info/Warning/Error)이 구분되어 기록된다
  3. 감사 로그에 MAC 주소 원문이 저장되지 않고 해싱 또는 마스킹 처리된다
  4. 로그 파일이 날짜별로 로테이션되어 지정 디렉토리에 생성된다
**Plans**: 1 plan
Plans:
- [x] 01-01-PLAN.md — Serilog 기반 로그 인프라 구축 + Debug.WriteLine 교체 + 감사 로그

### Phase 2: 안정성 기반
**Goal**: 비정상 종료, 리소스 누수, 레이스 컨디션이 제거되어 앱이 안정적으로 동작한다
**Depends on**: Phase 1
**Requirements**: RELI-01, RELI-02, RELI-03, RELI-04, PERF-02, PERF-03
**Success Criteria** (what must be TRUE):
  1. 처리되지 않은 예외가 발생해도 앱이 비정상 종료되지 않고 오류 메시지를 표시한다
  2. 영상을 반복 열고 닫아도 메모리·타이머 누수가 발생하지 않는다
  3. 빠른 영상 전환 시 레이스 컨디션으로 인한 화면 오류가 발생하지 않는다
  4. null 참조로 인한 NullReferenceException이 발생하지 않는다
  5. Undo/Redo 스택이 설정 상한을 초과하지 않는다
**Plans**: 2 plans
Plans:
- [x] 02-01-PLAN.md — Global exception handler + CancellationToken video loading + playback stop
- [x] 02-02-PLAN.md — Timer disposal + null guards + undo stack verification

### Phase 3: 기능 정확성 + 보안
**Goal**: 핵심 기능 버그가 수정되고 COCO JSON 정합성과 KISA 보안 기준이 충족된다
**Depends on**: Phase 2
**Requirements**: FUNC-01, FUNC-02, FUNC-03, FUNC-04, FUNC-05, FUNC-06, FUNC-07, FUNC-08, FUNC-09, FUNC-10, COMP-01, RELI-05, USAB-03, SECU-01, SECU-04, MAINT-02
**Success Criteria** (what must be TRUE):
  1. Vehicle 라벨 드롭다운에서 차량 종류를 선택하고 교체할 수 있다
  2. 내보낸 COCO JSON의 타임스탬프가 실제 프레임 시간을 반영한다
  3. 바운딩 박스 좌표가 이미지 경계를 절대 초과하지 않는다
  4. 라이선스 검증에 SHA-256 + Salt 기반 PBKDF2 해싱이 적용된다
  5. 손상된 JSON/SRT 파일을 열 때 크래시 없이 사용자 안내 메시지가 표시된다
  6. BBOX 생성 후 ID를 사후 지정해도 Entry-Exit 구간에서 동일 객체 ID가 유지된다
  7. Entry-Exit 프레임 간 객체 ID가 불일치하면 안내 메시지가 표시된다
  8. 객체 선택 상태에서 ID 변경 단축키가 Person/Vehicle/Event 전 클래스에서 일관되게 동작한다
  9. 다중 BBOX 상태에서 단축키로 ID 변경 시 선택된 객체만 변경된다 (전체 일괄 변경 방지)
  10. 새 영상 로드 시 이전 작업의 Waypoint·Labels·JSON 상태가 완전히 초기화된다
  11. BBOX 삭제 시 내부 데이터에 즉시 반영되어 화면 상태와 저장 데이터가 일치한다
**Plans**: 4 plans
Plans:
- [x] 03-01-PLAN.md — COCO JSON 타임스탬프 수정 + bbox 좌표 클램핑 + 카테고리 ID 정확화
- [x] 03-02-PLAN.md — PBKDF2 해싱 모듈 + 경로 트래버설 방지 모듈
- [x] 03-03-PLAN.md — 상태 초기화 + 삭제 통일 + 단축키 충돌 해소 + bbox 클램핑 적용
- [x] 03-04-PLAN.md — 구체적 예외 처리 + 사용자 친화적 오류 메시지
**UI hint**: yes

### Phase 4: 성능 + 사용성
**Goal**: 프레임 조회 성능이 최적화되고 UI 피드백이 일관되게 동작한다
**Depends on**: Phase 3
**Requirements**: PERF-01, COMP-02, USAB-01, USAB-02, USAB-04, USAB-05, USAB-06, USAB-07, USAB-08, MAINT-03
**Success Criteria** (what must be TRUE):
  1. 프레임 이동 시 바운딩 박스 조회가 체감 지연 없이 즉시 표시된다
  2. 모든 툴바 버튼에 마우스를 올리면 툴팁이 나타난다
  3. 전체 삭제 등 파괴적 작업 실행 전 확인 다이얼로그가 표시된다
  4. 저장하지 않고 앱을 닫으려 하면 경고 메시지가 표시된다
  5. Undo/Redo 가능 여부에 따라 버튼이 활성/비활성으로 표시된다
  6. 프로그램 상단 제품명이 문서 기준 정식 명칭과 일치하고, 상단 버튼이 우측으로 재배치되어 공간이 확보된다
  7. [정보] 단축키 설명에 Person/Vehicle/Event 클래스별 적용 범위가 명확히 구분되고, Vehicle 단축키가 포함되며, event_ 접두어가 제거된다
  8. Person/Vehicle 클래스에서 Entry-Exit 구간 내 프레임 단위 수동 추적(좌클릭 유지 + 프레임 이동으로 BBOX 위치 갱신)이 가능하다
**Plans**: 3 plans
Plans:
- [x] 04-01-PLAN.md — 딕셔너리 인덱싱 O(1) bbox 조회 + 매직 넘버 상수 추출
- [x] 04-02-PLAN.md — isDirty 미저장 경고 + 종료/영상전환 확인 다이얼로그 + FFmpeg 안내
- [x] 04-03-PLAN.md — 툴팁 추가 + 제품명 정식 명칭 + 단축키 목록 재구성
**UI hint**: yes

### Phase 5: 이식성
**Goal**: 명시된 Windows 환경에서 정상 설치·실행·제거가 보장된다
**Depends on**: Phase 4
**Requirements**: PORT-01, PORT-02, PORT-03
**Success Criteria** (what must be TRUE):
  1. Windows 10/11 클린 환경에서 설치 후 추가 설정 없이 앱이 정상 실행된다
  2. FFmpeg 또는 .NET Runtime 미설치 시 구체적인 안내 메시지가 표시된다
  3. 제거 후 레지스트리 잔여 항목 및 파일이 남지 않는다
**Plans**: 2 plans
Plans:
- [x] 05-01-PLAN.md — csproj 메타데이터 + self-contained publish 파이프라인 (PORT-02)
- [x] 05-02-PLAN.md — Inno Setup 스크립트 + build.bat + README (PORT-01, PORT-03)

### Phase 5.5: 기능 보정 + 안정화
**Goal**: Waypoint 탐색 편의와 영상 로드 경합 가드가 적용되어 잠긴 바이너리 기능이 완성된다
**Depends on**: Phase 5
**Requirements**: USAB-09, RELI-06
**Success Criteria** (what must be TRUE):
  1. Waypoint 선택 상태에서 Entry 버튼 클릭 시 해당 Waypoint의 Entry 프레임으로 즉시 이동한다
  2. Waypoint 선택 상태에서 Exit 버튼 클릭 시 해당 Waypoint의 Exit 프레임으로 즉시 이동한다
  3. Waypoint 미선택 상태에서는 기존대로 현재 프레임이 Entry/Exit로 지정된다 (기능 공존)
  4. 영상 로드 중에는 타임라인 클릭/드래그가 무시되어 렉/크래시가 발생하지 않는다
  5. Person/Vehicle 수동 추적 중에도 로드 경합이 발생하지 않는다
**Plans**: 2 plans
Plans:
- [x] 05.5-01-PLAN.md — Entry/Exit 버튼 이중 기능 분기 (USAB-09) + 타임라인 로드 가드 (RELI-06)
- [ ] 05.5-02-PLAN.md — RELI-06 gap closure: _isVideoReady 통합 가드 + 중복 LoadFrame(0) 제거 + 자동재생 연기

### Phase 5.6: 결함수정 (INSERTED)
**Goal**: KTC 1차 결함보고서 주황색 결함 및 QA팀 발견 결함을 수정하여 GS인증 1등급 기준(ISO/IEC 25023 8대 품질 특성)을 결함 없이 충족한다
**Depends on**: Phase 5
**Requirements**: 결함 17건 — 상세 `.planning/DEFECTS-INBOX.md` 참조
  - **크래시/치명**: DF-1-13 (영상 미로드 + Entry 클릭 시 TimeSpan NaN unhandled exception)
  - **ID 관리 계열 (신규)**: NEW-01~07 (BBOX 생성 후 사후 ID 지정 시 Entry-Exit 유지 실패, Event ID 불일치 경고 누락, Ctrl+N 유령 단축키 실구현, ID 변경 단축키 포커스 미동작, 라벨 사이드바 수동 변경 시 클래스 전체 변경, 다중 BBOX ID 수렴 버그)
  - **BBOX/Waypoint/JSON 정합성**: DF-1-03, 04, 05 (+ DF-1-16 검증)
  - **사용성**: DF-1-06(Tab 문서), 07(Ctrl+Z 검증), 11(온보딩), 14(메시지 한국어화), 18(Waypoint 일괄 삭제)
  - **보안**: DF-1-17 (감사 이벤트 확장 + HMAC 무결성 체인)
  - **제외**: DF-1-12 는 Phase 6-문서화에서 처리 (플랜 이월)
**Success Criteria** (what must be TRUE):
  1. 영상 미로드 상태에서 Entry/Exit 클릭 시 unhandled exception 없이 안내 메시지가 표시된다
  2. BBOX 생성 → 사후 ID 지정 → Entry 설정 후 Exit 프레임 BBOX 생성 시 Entry 지정 ID가 유지된다
  3. 선택된 개별 BBOX의 ID만 변경되고, 같은 클래스·같은 ID의 다른 박스는 영향받지 않는다
  4. Ctrl+N 입력 시 Exit 프레임 BBOX의 ID가 Entry 프레임 BBOX의 ID와 자동 일치된다
  5. Entry-Exit ID 불일치 시 Person/Vehicle/Event 모두 경고 메시지가 표시된다
  6. Waypoint 구간 내 마지막 BBOX 삭제 시 "Waypoint 구간 BBOX가 전부 삭제됩니다. 해당 Waypoint도 함께 삭제하시겠습니까?" 프롬프트(Yes/No)가 표시되고 선택 결과가 즉시 반영된다
  7. Waypoint/BBOX 없는 상태로 JSON 저장 시도 시 "Waypoint가 없어 해당 JSON 파일은 삭제됩니다. 삭제하시겠습니까?" 프롬프트(Yes/No)가 제공되고 선택이 파일 상태에 반영된다
  8. 영상 전환 시 "저장하지 않은 편집이 있습니다…" 프롬프트에서 "아니요" 선택 시 자동 저장된 JSON이 롤백 삭제된다
  9. 다중 선택된 Waypoint 에 대해 일괄 삭제가 가능하다
  10. 최초 실행 시 "동영상 선택 → BBOX 생성 → Entry/Exit 설정" 가이드 UI가 표시된다
  11. 지원하지 않는 파일 확장자 선택 시 오류 메시지가 전부 한국어로 출력된다
  12. 시스템 주요 동작 로그(영상 로드/BBOX 생성·삭제/Waypoint/Export/예외 등)가 감사 이벤트로 기록되고 HMAC 기반 무결성 체인으로 변조를 검출할 수 있다
**Plans**: 5 plans
Plans:
- [x] 05.6-01-crash-guard-PLAN.md — DF-1-13 영상 미로드 + Entry/Exit 크래시 가드 (btnEntry/Exit_Click + Keys.E/X)
- [x] 05.6-02-id-subsystem-PLAN.md — ID 관리 재설계 (NEW-01~07): ChangeBoxIdOnly, Ctrl+N 신규, Event 불일치 경고, 포커스 가드, Person NumericUpDown, BBOX 생성 ID 승계
- [x] 05.6-03-bbox-waypoint-json-PLAN.md — Waypoint 동반 삭제 + Empty JSON 방지 + 영상 전환 롤백 삭제 (DF-1-03, 04, 05, 16)
- [x] 05.6-04-usability-PLAN.md — 온보딩 가이드 + 한국어 메시지 + Waypoint 일괄 삭제 + Tab/Undo UAT 체크포인트 (DF-1-06, 07, 11, 14, 18)
- [ ] 05.6-05-secure-logging-PLAN.md — 감사 이벤트 9종 확장 + HMAC 무결성 체인 + 키 관리 + 검증 유틸 (DF-1-17)

### Phase 6: 문서화
**Goal**: 잠긴 바이너리와 완전히 일치하는 제품설명서 및 사용자취급설명서가 완성된다
**Depends on**: Phase 5.5
**Requirements**: DOC-01, DOC-02, DOC-03
**Success Criteria** (what must be TRUE):
  1. 제품설명서에 버전, 연동 제품 정보, 시스템 요구사항이 명시된다
  2. 사용자취급설명서에 모든 기능·입력값 유효 범위·오류 메시지가 기술된다
  3. 문서 내 버전 정보가 실제 프로그램 버전과 일치한다
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 5.5 → 5.6 → 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. 로그 인프라 | 1/1 | Complete   | 2026-04-16 |
| 2. 안정성 기반 | 2/2 | Complete   | 2026-04-16 |
| 3. 기능 정확성 + 보안 | 4/4 | Complete   | 2026-04-16 |
| 4. 성능 + 사용성 | 3/3 | Complete   | 2026-04-17 |
| 5. 이식성 | 2/2 | Complete   | 2026-04-17 |
| 5.5. 기능 보정 + 안정화 | 1/2 | In progress | - |
| 5.6. 결함수정 (INSERTED) | 4/5 | In progress | - |
| 6. 문서화 | 0/TBD | Not started | - |
