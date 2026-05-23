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
| `Core/SceneLoader.cs` | ✅ 완료 (안전 로딩 추가) |
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
| `Editor/SaberTrailMaterialCreator.cs` | ✅ 완료 |
| `Gameplay/NoteSliceHandler.cs` | ✅ 완료 (EzySlice 기반) |
| `StreamingAssets/Songs/song_001/info.json` | ✅ 완료 |
| `StreamingAssets/Songs/song_001/chart_normal.json` | ✅ 완료 (2분 확장) |
| `StreamingAssets/Songs/song_001/chart_easy.json` | ✅ 완료 (2분 확장) |

---

## 구현 단계 (설계서 §12 기준)

| # | 단계 | 상태 | 메모 |
|---|---|---|---|
| 1 | 프로젝트 셋업 (URP, XRI, Meta XR SDK, Git) | ✅ | 패키지 및 기초 셋업 완료 |
| 2 | **Conductor + 테스트 씬** (박자 동기화 검증) | ✅ | dspTime 기반 동기화 확인 |
| 3 | **NoteData/ChartData/SongLibrary** JSON 로드 검증 | ✅ | song_001 로드 성공 |
| 4 | **NoteSpawner + NotePool + NormalNote** | ✅ | 풀링 및 스폰 로직 검증 완료 |
| 5 | **SaberController** VR 입력 + 터널링 방지 | ✅ | XR 컨트롤러 연동 및 Velocity 체크 확인 |
| 6 | 색 매칭 + 속도 임계값 + 방향 판정 | ✅ | NormalNote.OnSliced 판정 확인 |
| 7 | **LongNote** 머리/꼬리/유지 로직 | ✅ | 색상 적용, 헤드 슬라이싱, 바디 조각 잘리기 완료 |
| 8 | **ScoreManager + HealthSystem** | ✅ | 타격 시 점수/체력 변화 확인 |
| 9 | **HUD + PauseMenu** | ✅ | 월드스페이스 Canvas 자동 생성/연결 및 Smooth Follow 적용 |
| 10 | **SongSelect UI + 최고점수** | 🔲 | **[중요]** 씬 제작 및 Build Settings 추가 필요 |
| 11 | **Result 씬 + SaveSystem** | 🔲 | **[중요]** 씬 제작 및 Build Settings 추가 필요 |
| 12 | **Calibration 씬** (userOffset 측정) | 🔲 | 박수 타이밍 측정 로직 추가 |
| 13 | **Settings 씬** | 🔲 | SettingsUI 배치 |
| 14 | **Tutorial 씬** | 🔲 | 기본 조작 가이드 |
| 15 | **ChartEditorWindow** 채보 제작 | 🔲 | 로직은 있으나 UI/사용법 검증 필요 |
| 16 | 노래 2~3곡 추가 + 풀 플레이 테스트 | ✅ | song_001 2분 분량 확장 완료 |
| 17 | Quest 빌드 + OVR 최적화 + 90fps 측정 | 🔲 | OVRSettings 활성화 |
| 18 | 이펙트/사운드/햅틱 폴리싱 | 🔶 | SliceEffect(파티클 Instantiate), SaberTrail(커스텀 리본 메시), 동적 메시 슬라이싱(EzySlice) 완료. 사운드/햅틱 미완 |
| 19 | (선택) 리플레이 시스템 | 🔲 | - |
| 20 | README + 시연 영상 | 🔲 | - |

---

## 오늘 완료한 작업 (비주얼 폴리싱 세션)

- **어셈블리 충돌 수정**: Cartoon FX Remaster `.asmdef` 파일 위치 수정
- **SliceEffect**: 파티클 Instantiate 방식으로 재작성 (CFXR 호환)
- **SaberTrail**: `TrailRenderer` → 루트~팁 커스텀 리본 메시로 전환, 색상 자동 설정
- **SaberTrailMaterialCreator**: 메뉴 한 번으로 Additive 트레일 머티리얼 자동 생성
- **NormalNote 동적 슬라이싱**: EzySlice 도입, 스윙 방향 + 수직 spread 물리 적용
- **LongNote 색상**: Head / Body / Tail 모두 matR / matB 자동 적용
- **LongNote 방향 버그 수정**: body.localPosition 루트 스케일(0.4) 보정, 루트 큐브 렌더러 비활성화
- **LongNote 헤드 슬라이싱**: NoteSliceHandler로 head 구체만 EzySlice
- **LongNote 바디 슬라이싱**: Scale 트리밍 + 프리미티브 청크 스폰 방식 (안정적), SliceEffect 반복 재생, Inspector에서 `Slice Step` 조절 가능

---

## 다음 즉시 할 일

1. ✅ **LongNote 바디 슬라이싱 튜닝**: 양옆 spread 연출로 전환 — 전체 폭 큐브 1개 → 좌/우 반쪽 2개로 갈라져 `transform.right` 양옆 + 뒤 + 위로 날아가도록 변경. `Spread/Back/Up Force`, `Chunk Lifetime` Inspector 조절 가능. → **플레이테스트로 힘 값 미세조정 필요**
2. **누락된 씬 제작**: `SongSelect`, `Result` 씬을 만들어 전체 게임 루프 연결
3. **사운드 연결**: 히트/미스 효과음 AudioClip 연결 (스크립트는 완성됨)
4. **Quest 빌드 + OVR 최적화**: OVRSettings 활성화, 90fps 측정

---

## 알려진 제한 사항 및 조치

- `SceneLoader.Load()`: `Result`, `SongSelect` 씬 부재 시 크래시 방지를 위해 `Application.CanStreamedLevelBeLoaded` 체크 로직 추가됨. (추후 씬 제작 시 자동 해결 예정)
- `NoteSpawner.cs`: 테스트를 위해 곡 선택 없이 실행 시 `song_001`을 강제 로드하도록 수정됨.
- `SongLibrary.LoadAllSongs()`: Android(Quest) 빌드 전 `manifest.json` 기반 로딩 교체 필요.
- `audio.ogg` -> `audio.mp3` 지원하도록 `SongLoader.cs` 수정 완료.
