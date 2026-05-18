# VRBeat 구현 진행도

설계서 `BeatSaber_Clone_Design_v2.md` 기준. 단계별 체크 후 다음 단계 진행.

---

## 스크립트 구현 현황

| 파일 | 상태 |
|---|---|
| `GameEnums.cs` | ✅ 완료 |
| `Core/GameConfig.cs` | ✅ 완료 |
| `Core/GameSettings.cs` | ✅ 완료 |
| `Core/SaveSystem.cs` | ✅ 완료 |
| `Core/GameManager.cs` | ✅ 완료 |
| `Core/SceneLoader.cs` | ✅ 완료 |
| `Song/SongInfo.cs` | ✅ 완료 |
| `Song/SongData.cs` | ✅ 완료 |
| `Song/SongLoader.cs` | ✅ 완료 |
| `Song/SongLibrary.cs` | ✅ 완료 |
| `Chart/NoteData.cs` | ✅ 완료 |
| `Chart/ChartData.cs` | ✅ 완료 |
| `Chart/ChartParser.cs` | ✅ 완료 |
| `Gameplay/Conductor.cs` | ✅ 완료 |
| `Gameplay/NotePool.cs` | ✅ 완료 |
| `Gameplay/NoteSpawner.cs` | ✅ 완료 |
| `Gameplay/NoteBase.cs` | ✅ 완료 |
| `Gameplay/NormalNote.cs` | ✅ 완료 |
| `Gameplay/LongNote.cs` | ✅ 완료 |
| `Gameplay/LongNoteBody.cs` | ✅ 완료 |
| `Saber/SaberController.cs` | ✅ 완료 |
| `Saber/SaberHitDetector.cs` | ✅ 완료 |
| `Saber/SliceEffect.cs` | ✅ 완료 |
| `Saber/SaberTrailCheck.cs` | ✅ 완료 |
| `Saber/HapticFeedback.cs` | ✅ 완료 |
| `Scoring/ScoreManager.cs` | ✅ 완료 |
| `Scoring/HealthSystem.cs` | ✅ 완료 |
| `Scoring/AccuracyCalc.cs` | ✅ 완료 |
| `UI/HUD.cs` | ✅ 완료 |
| `UI/PauseMenu.cs` | ✅ 완료 |
| `UI/SongSelectUI.cs` | ✅ 완료 |
| `UI/ResultUI.cs` | ✅ 완료 |
| `UI/SettingsUI.cs` | ✅ 완료 |
| `VR/XRRigSetup.cs` | ✅ 완료 |
| `Performance/PerformanceMonitor.cs` | ✅ 완료 |
| `Performance/OVRSettings.cs` | ✅ 완료 |
| `Editor/ChartEditorWindow.cs` | ✅ 완료 |
| `StreamingAssets/Songs/song_001/info.json` | ✅ 완료 |
| `StreamingAssets/Songs/song_001/chart_normal.json` | ✅ 완료 |
| `StreamingAssets/Songs/song_001/chart_easy.json` | ✅ 완료 |

---

## 구현 단계 (설계서 §12 기준)

| # | 단계 | 상태 | 메모 |
|---|---|---|---|
| 1 | 프로젝트 셋업 (URP, XRI, Meta XR SDK, Git) | 🔲 | 패키지 수동 설치 필요 |
| 2 | **Conductor + 테스트 씬** (박자 동기화 검증) | 🔲 | Gameplay씬에 Conductor 배치 후 큐브로 확인 |
| 3 | **NoteData/ChartData/SongLibrary** JSON 로드 검증 | 🔲 | song_001 로드 → 콘솔 확인 |
| 4 | **NoteSpawner + NotePool + NormalNote** | 🔲 | 풀링, 박자 도달, 미스 감지 |
| 5 | **SaberController** VR 입력 + 터널링 방지 | 🔲 | XR 컨트롤러에 붙이기 |
| 6 | 색 매칭 + 속도 임계값 + 방향 판정 | 🔲 | NormalNote.OnSliced 검증 |
| 7 | **LongNote** 머리/꼬리/유지 로직 | 🔲 | LongNoteBody 트리거 확인 |
| 8 | **ScoreManager + HealthSystem** | 🔲 | HUD 연결 |
| 9 | **HUD + PauseMenu** | 🔲 | VR 월드스페이스 Canvas 배치 |
| 10 | **SongSelect UI + 최고점수** | 🔲 | SongSelect 씬 구성 |
| 11 | **Result 씬 + SaveSystem** | 🔲 | 결과 표시 + 신기록 저장 |
| 12 | **Calibration 씬** (userOffset 측정) | 🔲 | 박수 타이밍 측정 로직 추가 |
| 13 | **Settings 씬** | 🔲 | SettingsUI 배치 |
| 14 | **Tutorial 씬** | 🔲 | 기본 조작 가이드 |
| 15 | **ChartEditorWindow** 채보 제작 | 🔲 | VRBeat > Chart Editor 메뉴 |
| 16 | 노래 2~3곡 추가 + 풀 플레이 테스트 | 🔲 | audio.ogg 파일 필요 |
| 17 | Quest 빌드 + OVR 최적화 + 90fps 측정 | 🔲 | OVRSettings 활성화 |
| 18 | 이펙트/사운드/햅틱 폴리싱 | 🔲 | SliceEffect 파티클 제작 |
| 19 | (선택) 리플레이 시스템 | 🔲 | - |
| 20 | README + 시연 영상 | 🔲 | - |

---

## 다음 즉시 할 일

1. **Unity 에서 컴파일 오류 확인** — 패키지 누락(TMPro, XRI, OVR) 있으면 설치
2. **Gameplay 씬 셋업**
   - 빈 `GameManager` GameObject → `GameManager` + `SongLibrary` 컴포넌트
   - `Conductor` GameObject → `Conductor` + AudioSource
   - `NoteSpawner` GameObject → `NoteSpawner` + `NotePool` (프리팹 연결)
   - `ScoreManager`, `HealthSystem` GameObject
   - `HUD` Canvas (World Space, 플레이어 앞)
3. **NormalNote 프리팹** 제작 (BoxCollider + NormalNote 컴포넌트)
4. **LongNote 프리팹** 제작 (head/body/tail 구조, LongNoteBody 컴포넌트)
5. **Saber 프리팹** 제작 (SaberController + tip/root Transform)
6. `StreamingAssets/Songs/song_001/audio.ogg` 추가 (120 BPM 테스트용 음원)

---

## 알려진 제한 사항

- `SongLibrary.LoadAllSongs()`: Android(Quest) 빌드에서는 `Directory.GetDirectories` 미작동 → **빌드 전 `manifest.json` 기반 로딩으로 교체 필요**
- `PauseMenu.cs`: `OVRInput` 참조 → Meta XR SDK 없으면 `#if` 처리 필요
- `OVRSettings.cs`: `OVRManager` 참조 → Meta XR SDK 필요
- `SliceEffect.cs`: 파티클 시스템 프리팹 Inspector에서 연결 필요
- `audio.ogg` 파일은 직접 추가해야 함 (저작권 파일 별도 준비)
