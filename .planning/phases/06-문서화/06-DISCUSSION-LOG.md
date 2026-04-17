# Phase 6: 문서화 - Discussion Log

> **Audit trail only.** Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-04-17
**Phase:** 06-문서화
**Areas discussed:** 문서 형식, 스크린샷 수준, 버전 동기화, 사용자취급설명서 목차 구조

---

## 문서 형식

| Option | Description | Selected |
|--------|-------------|----------|
| Markdown → DOCX 변환 | pandoc으로 변환, git 추적 용이 | ✓ |
| DOCX 직접 작성 | Word 직접, 버전 추적 어려움 | |
| HWP (한글) | 인증 표준 포맷, 한글 필요 | |
| PDF 최종본만 | Markdown + PDF 바로 출력 | |

**User's choice:** Markdown → DOCX 변환

---

## 스크린샷 수준

| Option | Description | Selected |
|--------|-------------|----------|
| 주요 화면 5-10장 | 메인/파일로드/BBOX/Waypoint/버튼/단축키 다이얼로그 등 | ✓ |
| 전체 상세 30+ | 모든 버튼/엘리먼트/오류 다이얼로그 | |
| 스크린샷 없음 | 텍스트/표만으로 | |

**User's choice:** 주요 화면 5-10장

---

## 버전 동기화 (DOC-03)

| Option | Description | Selected |
|--------|-------------|----------|
| 수동 표기 + 체크리스트 | 문서 철두 수동 표기, 제출 전 체크리스트로 일치 확인 | ✓ |
| 빌드 시 자동 삽입 | MSBuild Task → csproj.Version → MD 자동 주입 | |
| 단일 VERSION 파일 | VERSION.md as single source, 모두 참조 | |

**User's choice:** 수동 표기 + 체크리스트

---

## 사용자취급설명서 목차 구조

| Option | Description | Selected |
|--------|-------------|----------|
| 전통적 구성 | 설치→화면→기능→단축키→오류→FAQ (6장) | ✓ |
| 태스크 중심 | 워크플로우별 (프로젝트 시작 / 라벨링 / 내보내기) | |
| 참조 문서 형식 | 단가격 나열, API doc 방식 | |

**User's choice:** 전통적 구성

---

## Claude's Discretion
- Markdown 템플릿 세부 스타일
- 스크린샷 주석 방식
- FAQ 항목 선정
- pandoc reference-doc 한글 폰트 설정

## Deferred Ideas
- 영문 번역 (v2)
- 비디오 튜토리얼 (v2)
- 온라인 F1 도움말 시스템 (v2)
- MSBuild + pandoc CI 통합 (v2)
- API 레퍼런스 (OTS)
