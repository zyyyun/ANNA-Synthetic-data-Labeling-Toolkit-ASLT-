# Phase 6: 문서화 - Context

**Gathered:** 2026-04-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 6는 GS인증 제출용 두 가지 문서를 작성하여 잠긴 바이너리(v1.0.0)와 완전히 일치하는 제품 문서를 완성한다:

1. **제품설명서** (Product Specification) — 버전, 연동 제품, 시스템 요구사항 (DOC-01)
2. **사용자취급설명서** (User Manual) — 전 기능, 입력값 유효 범위, 오류 메시지 (DOC-02)
3. **버전 동기화** — 문서-프로그램 버전 일치 (DOC-03)

## Out of Scope
- 개발자 가이드, API 문서, 기술 스펙 (인증 평가 범위 밖)
- 영문/다국어 번역 (OTS — 한국어 UI 일관성)
- 온라인 도움말 시스템 통합 (v2)
- 비디오 튜토리얼

</domain>

<decisions>
## Implementation Decisions

### 문서 형식 (D-01)
- **D-01**: Markdown(`.md`)을 source of truth로 작성 → **pandoc**으로 `.docx` 변환
- **D-02**: 변환 명령: `pandoc input.md -o output.docx --reference-doc=template.docx`
- **D-03**: Git에는 `.md`만 커밋, 생성된 `.docx`는 `docs/output/` (gitignore)
- **D-04**: 제출 시 DOCX 파일 2개 제공 (제품설명서 + 사용자취급설명서)

### 스크린샷 정책 (D-05~D-09)
- **D-05**: 주요 화면 **5-10장** 포함
- **D-06**: 필수 스크린샷 목록:
  1. 메인 화면 전체 (영상 로드 후, 다크 테마 확인)
  2. 파일 열기 및 영상 로드 중 ("영상 로드 중..." 오버레이)
  3. BBOX 그리기 모드 (연필 아이콘 + Person/Vehicle/Event 선택)
  4. Waypoint 생성 예시 (Entry/Exit 마커 + 타임라인)
  5. 우측 Waypoint 리스트 + 사이드바 (ID/Class ComboBox 포함)
  6. AboutForm 단축키 다이얼로그 (Person/Vehicle/Event 섹션)
  7. JSON 내보내기 성공 메시지
  8. 오류 메시지 예시 (손상 파일 로드 시 해결 방법 포함)
- **D-07**: 스크린샷 해상도: 1920×1080 기반, 주요 영역 크롭
- **D-08**: 저장 위치: `docs/images/` (Git tracked)
- **D-09**: 명명 규칙: `01-main-screen.png`, `02-file-load.png`, ...

### 버전 동기화 (D-10~D-13)
- **D-10**: **수동 표기** + 제출 전 **체크리스트** 방식
- **D-11**: 문서 철두(frontmatter/표지)에 표기:
  ```
  **소프트웨어 버전**: ASLT v1.0.0
  **문서 버전**: 1.0
  **작성일**: 2026-04-17
  ```
- **D-12**: 체크리스트 (새 빌드 시 확인):
  - [ ] `ASLTv1.0.csproj` `<Version>` = 문서 버전
  - [ ] `AboutForm.cs` line 31 `lblTitle.Text` 프로그램 명칭 일치
  - [ ] `installer/ASLT-Setup.iss` `AppVersion` 일치
  - [ ] 스크린샷 내 타이틀바 버전 표기 일치
- **D-13**: 체크리스트는 `docs/VERSION-CHECKLIST.md`로 별도 문서화

### 사용자취급설명서 목차 (D-14~D-20)
전통적 구성:

- **D-14**: 1장. **설치 및 제거**
  - 시스템 요구사항 (Windows 10/11 x64)
  - 설치 절차 (ASLT-Setup-v1.0.0.exe 실행 → 마법사)
  - 제거 절차 (제어판 또는 시작 메뉴)
  - 번들 의존성 (.NET 8 Runtime, FFmpeg — 별도 설치 불필요)

- **D-15**: 2장. **화면 구성**
  - 상단: 타이틀바 ("ASLT(v1.0)"), 툴바 버튼 (21개, 툴팁 포함)
  - 중앙: 영상 영역 (pictureBoxVideo), 타임라인, 재생 컨트롤
  - 좌측: Bbox 목록
  - 우측: Person/Vehicle/Event Waypoint 리스트

- **D-16**: 3장. **기능별 조작**
  - 영상 로드 및 재생
  - BBOX 그리기/이동/삭제/크기 조정
  - 클래스 선택 (Person/Vehicle/Event)
  - ID 지정 및 변경
  - Waypoint 생성 (Entry/Exit)
  - 수동 추적 (드래그하며 프레임 이동)
  - JSON 저장 및 불러오기
  - 자막 표시/숨김

- **D-17**: 4장. **단축키** — `AboutForm.cs` 108-140 라인 내용 이식
  - 공통 단축키
  - Person 전용
  - Vehicle 전용
  - Event 전용

- **D-18**: 5장. **오류 메시지** — 실제 코드에서 추출 (`JsonService.cs`, `VideoService.cs`, `MainForm.cs` 모든 MessageBox 텍스트)
  - 파일 로드 오류
  - JSON 파싱 오류
  - 코덱 지원 오류
  - FFmpeg 미설치
  - 권한 오류
  - ID 불일치 경고
  - 미저장 경고
  - 각 항목마다 "해결 방법:" 포함

- **D-19**: 6장. **자주 묻는 질문 (FAQ)**
  - AVI 영상 로드가 느린 이유
  - 백업 JSON 파일 위치 및 복구
  - 라벨링 파일 호환성 (COCO)
  - 다른 컴퓨터로 이동 (설치 관련)

- **D-20**: 부록: 입력값 유효 범위
  - BBOX 최소 크기 (`MIN_BBOX_SIZE = 10`)
  - Undo 스택 최대 (`MAX_UNDO_STACK = 100`)
  - Person ID 범위 (1-20)
  - Vehicle ID 범위 (1-4)
  - Event ID 범위 (1-10)
  - 지원 영상 포맷 (MP4 권장, AVI 지원)

### 제품설명서 구조 (D-21~D-24)

- **D-21**: 1. **제품 개요**
  - 제품명, 버전, 작성일
  - 용도 및 핵심 가치
  - 주요 특징 요약

- **D-22**: 2. **기술 사양**
  - 개발 환경: C# .NET 8.0, WinForms, x64
  - 주요 라이브러리: OpenCvSharp4 4.11, FFMpegCore 5.1, Newtonsoft.Json 13.0, Serilog 4.2
  - 지원 영상 형식 및 코덱
  - 출력 데이터 형식 (COCO JSON)

- **D-23**: 3. **시스템 요구사항**
  - 운영체제: Windows 10/11 x64
  - RAM/디스크 권장 사양
  - 별도 설치 불필요 (.NET 런타임, FFmpeg 모두 번들)

- **D-24**: 4. **연동 제품 정보** (사용자가 별도 논의 필요 시 여기 확장)
  - 독립 실행 가능 (stand-alone)
  - COCO JSON 형식으로 타 ML 도구와 데이터 교환
  - 현재 명시된 외부 연동 없음 (IFEZ 등 사용 기관 정보는 필요 시 추가)

### Claude's Discretion
- Markdown 템플릿 세부 스타일 (제목 레벨, 표 형식 등)
- 스크린샷 주석 방식 (번호 + 설명 vs 화살표/박스 annotation)
- FAQ 항목 선정 (기술 지원 관점에서 자주 나올 법한 질문)
- pandoc reference-doc 템플릿 (한글 폰트, 목차, 페이지 번호 설정)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Specs
- `.planning/PROJECT.md` — 제품 비전, 핵심 가치, 제약사항
- `.planning/REQUIREMENTS.md` §DOC — DOC-01, DOC-02, DOC-03 정의
- `.planning/ROADMAP.md` §Phase 6 — Goal + Success Criteria

### Source Content (직접 이식 대상)
- `Forms/AboutForm.cs` lines 108-140 — 단축키 목록 (사용자취급설명서 4장 이식)
- `Services/JsonService.cs` — MessageBox 텍스트 (5장 오류 메시지 추출)
- `Services/VideoService.cs` — FFmpeg/SRT 오류 메시지
- `Forms/MainForm.cs` — 미저장 경고, ID 불일치 경고, FFmpeg 안내 등
- `installer/README.md` — 설치/제거 절차 (1장 이식)

### Version Sources (DOC-03 동기화 대상)
- `ASLTv1.0.csproj` — `<Version>1.0.0</Version>`, `<Product>` 필드
- `Forms/AboutForm.cs` line 31 — 파란색 제목 (정식 명칭)
- `installer/ASLT-Setup.iss` — `AppVersion`, `OutputBaseFilename`

### Constants (입력값 범위 부록)
- `Forms/MainForm.cs` lines 26-30 — `HANDLE_SIZE`, `MIN_BBOX_SIZE=10`, `MAX_UNDO_STACK=100`, `RESIZE_BORDER_WIDTH`
- `Services/JsonService.cs` — 카테고리 ID 범위 (person 1-20, vehicle 1-4, event 1-10)

### Existing Documentation
- `CLAUDE.md` — 프로젝트 설명 (제품 개요 참고용)
- `.planning/codebase/*.md` — 기존 7개 코드베이스 분석 문서

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **AboutForm 단축키 배열**: 튜플 `(shortcut, description)` 형식으로 이미 구조화 — Markdown 표로 직접 변환 가능
- **오류 메시지 문자열**: Phase 3에서 "해결 방법:" 형식으로 일관화됨 — 문서에 원문 그대로 사용
- **README.md (installer)**: 설치 절차 이미 상세 — 문서 1장 이식

### Established Patterns
- 한국어 UI 일관성 — 문서도 한국어 원문
- 정식 명칭 "ANNA 합성데이터 라벨링 툴킷 (ASLT)" + 축약 "ASLT(v1.0)" — 문서에서도 동일 규칙
- 버전 1.0.0 — 4-part AssemblyVersion 1.0.0.0과 별도로 사용자 대면 버전은 3-part

### Integration Points
- **pandoc**: 유지보수자가 수동 실행 (build.bat과 유사한 패턴)
- **스크린샷**: 유지보수자가 수동 캡처 (현재 빌드 실행 후)
- **체크리스트**: `docs/VERSION-CHECKLIST.md` — 제출 전 실행

### Tool Dependencies (추가 설치 필요, 유지보수자 책임)
- pandoc (https://pandoc.org) — Markdown → DOCX 변환
- (선택) Visual Studio Code + Markdown Preview Enhanced — 작성 편의

</code_context>

<specifics>
## Specific Ideas

### 문서 디렉토리 구조
```
docs/
├── PRODUCT-SPEC.md                  # 제품설명서 source
├── USER-MANUAL.md                   # 사용자취급설명서 source
├── VERSION-CHECKLIST.md             # DOC-03 동기화 체크리스트
├── template.docx                    # pandoc reference-doc (한글 폰트 등)
├── build-docs.bat                   # pandoc 실행 자동화
├── images/                          # 스크린샷 (Git tracked)
│   ├── 01-main-screen.png
│   ├── 02-file-load.png
│   └── ...
└── output/                          # DOCX 결과물 (.gitignore)
    ├── ASLT-ProductSpec-v1.0.0.docx
    └── ASLT-UserManual-v1.0.0.docx
```

### 문서 표지 템플릿
```markdown
---
title: "ANNA 합성데이터 라벨링 툴킷 (ASLT) 사용자취급설명서"
subtitle: "Version 1.0.0"
date: 2026-04-17
author: "ANNA"
---

# ANNA 합성데이터 라벨링 툴킷 (ASLT)
## 사용자취급설명서

**소프트웨어 버전**: ASLT v1.0.0
**문서 버전**: 1.0
**작성일**: 2026-04-17
**저작권**: Copyright © ANNA 2026

---

## 목차

1. [설치 및 제거](#1-설치-및-제거)
2. [화면 구성](#2-화면-구성)
...
```

### 오류 메시지 이식 방법
각 Service/Form 파일에서 `MessageBox.Show`, `Log.Warning`, `Log.Error` 호출을 grep으로 추출 → 한국어 텍스트 + "해결 방법:" 섹션 그대로 복사. 페이지별 표 형식으로 정리.

### 스크린샷 캡처 가이드
- 1920×1080 해상도에서 앱 실행
- 다크 테마 활성 (기본)
- 샘플 영상 로드 후 Person/Vehicle/Event 박스 각 1개씩 생성
- Waypoint 1개 생성 (데모용)
- PNG로 저장, 필요 시 주요 영역 크롭

</specifics>

<deferred>
## Deferred Ideas

### 향후 마일스톤 고려
- **영문 번역** — 해외 배포 시 필요 (v2)
- **비디오 튜토리얼** — YouTube 등 외부 플랫폼 (v2)
- **온라인 도움말 시스템** — 앱 내 F1 키 → HtmlHelp 연동 (v2)
- **CHANGELOG.md** — 다음 버전 출시 시 작성
- **자동화 빌드**: MSBuild Task + pandoc 통합으로 CI에서 DOCX 자동 생성 (v2)
- **문서 테스트** — Markdown lint, 깨진 링크 검사 (v2)

### Out of Current Scope
- API 레퍼런스 (public API 없음 — 내부 WinForms 전용)
- 개발자 가이드 (GS인증 평가 대상 아님)
- 릴리스 노트 (v1.0.0이 첫 버전)

</deferred>

---

*Phase: 06-문서화*
*Context gathered: 2026-04-17*
