---
slug: 260512-gsv-shortcut-speed-shift-period-comma
status: in-progress
created: 2026-05-12
priority: critical
deadline: 2026-05-13~14 (GS인증 측 요청)
---

# Shift + > / < 배속 단축키 동작 수정

## 배경 (External Trigger)

GS인증 검토 측에서 다음 이슈 보고:

> 현재 Shift +> or < 단축키로 배속 증가/감소 기능이 안된다고해서 수정 좀 해달라고 합니다.
> 제가 실제로 사용을 해보니 배속 기능이 안돼더라구요.

내일 또는 모레까지 closure 요청. KTC 인증 직전 단계라 시급.

## 진단

### 현재 구현
`Forms/MainForm.cs` line 3056-3067 (`MainForm_KeyDown`):

```csharp
else if (e.Shift && e.KeyCode == Keys.OemPeriod)
{
    if (playbackSpeed < 1.0) playbackSpeed = 1.0;
    else if (playbackSpeed < 4.0) playbackSpeed = 4.0;
    else if (playbackSpeed < 8.0) playbackSpeed = 8.0;
    else if (playbackSpeed < 16.0) playbackSpeed = 16.0;
    if (isPlaying) lastFrameTime = DateTime.Now.Ticks / 10000;
    UpdateTimeLabels(); e.Handled = true;
}
else if (e.Shift && e.KeyCode == Keys.Oemcomma) { ... 동일 패턴 ... }
```

로직 자체는 정확. 그러나 `MainForm_KeyDown` 도달 전 line 2890-2898 의 textbox/combobox 포커스 가드가 차단한다:

```csharp
Control focusedControl = this.ActiveControl;
if (focusedControl is TextBox || focusedControl is ComboBox)
{
    bool isIdShortcut =
        (e.Control && (IsIdAssignmentKey(e.KeyCode) || e.KeyCode == Keys.N))
        || (e.Alt && IsIdAssignmentKey(e.KeyCode));
    if (!isIdShortcut && e.KeyCode != Keys.Enter && e.KeyCode != Keys.Escape) return;
}
```

화이트리스트: Ctrl+숫자, Ctrl+N, Alt+숫자, Enter, Escape. **Shift+./, 는 없음** → 차단됨.

GS인증 reviewer 가 라벨/ID 콤보박스 클릭 후 단축키를 시도하면 어떤 키도 작동 안 함.

### 비교: 화살표 키는 왜 작동하는가?
`ProcessCmdKey` (line 2855-2861) 에서 처리:
```csharp
if (keyData == (Keys.Shift | Keys.Left))  { LoadFrame(...); return true; }
if (keyData == (Keys.Shift | Keys.Right)) { LoadFrame(...); return true; }
if (keyData == Keys.Left)  { LoadFrame(...); return true; }
if (keyData == Keys.Right) { LoadFrame(...); return true; }
```

`ProcessCmdKey` 는 컨트롤 라우팅 **전** 호출되므로 포커스와 무관하게 항상 작동.

## Fix

화살표 키와 동일한 패턴으로 **배속 단축키를 `ProcessCmdKey` 로 이동**.

### 변경 1: `ProcessCmdKey` 에 추가
```csharp
if (isVideoLoaded && _isVideoReady)
{
    // ... 기존 arrow seek ...
    if (keyData == (Keys.Shift | Keys.OemPeriod))
    {
        if (playbackSpeed < 1.0) playbackSpeed = 1.0;
        else if (playbackSpeed < 4.0) playbackSpeed = 4.0;
        else if (playbackSpeed < 8.0) playbackSpeed = 8.0;
        else if (playbackSpeed < 16.0) playbackSpeed = 16.0;
        if (isPlaying) lastFrameTime = DateTime.Now.Ticks / 10000;
        UpdateTimeLabels();
        return true;
    }
    if (keyData == (Keys.Shift | Keys.Oemcomma))
    {
        if (playbackSpeed > 8.0) playbackSpeed = 8.0;
        else if (playbackSpeed > 4.0) playbackSpeed = 4.0;
        else if (playbackSpeed > 1.0) playbackSpeed = 1.0;
        else playbackSpeed = 0.5;
        if (isPlaying) lastFrameTime = DateTime.Now.Ticks / 10000;
        UpdateTimeLabels();
        return true;
    }
}
```

### 변경 2: `MainForm_KeyDown` 의 중복 분기 제거
Line 3056-3067 (Shift+OemPeriod / Oemcomma 두 else-if 블록) 삭제.

## 검증

- `dotnet build "ASLTv1.0.csproj" -c Debug` — 0 errors
- Manual 검증 (사용자):
  1. 영상 로드 후 트랙바 또는 사이드바 콤보박스 클릭하여 포커스 이동
  2. Shift+> 누름 → 1x → 4x → 8x → 16x 단계 증가, 우측 하단 시간 라벨에 "Nx" 표시 확인
  3. Shift+< 누름 → 16x → 8x → 4x → 1x → 0.5x 단계 감소
  4. 영상 재생 중에도 동일하게 작동 확인

## Definition of Done

1. ProcessCmdKey 에 Shift+OemPeriod/Oemcomma 분기 추가 (return true)
2. MainForm_KeyDown 의 line 3056-3067 두 분기 제거
3. dotnet build 0 errors
4. CLAUDE.md 준수 (Critical/High 0건 — 단순 위치 이동, 기존 로직 보존)
5. atomic commit 1개: `fix(shortcuts): move Shift+./,  speed shortcut to ProcessCmdKey for focus-independent activation`
6. v1.0.4 installer 재빌드 (인증 측 전달용)
7. STATE.md "Quick Tasks Completed" 갱신

## 비범위

- 배속 단계 수정 (1/4/8/16 → 다른 비율) — GS인증 측 요청 사항 외
- 배속 단축키 키 자체 변경 — 외부 문서에 Shift+> < 로 안내되어 있을 가능성
- 다른 단축키 focus 동작 — 본 이슈 보고 외
