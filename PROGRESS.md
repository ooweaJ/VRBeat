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
| 10 | **SongSelect UI + 최고점수** | ✅ | 씬 제작 + Build Settings 추가 완료, 최고점수 표시 검증됨 |
| 11 | **Result 씬 + SaveSystem** | ✅ | 점수/정확도/랭크/NEW RECORD 표시 + 최고점수 저장 검증됨 |
| 12 | **Calibration 씬** (userOffset 측정) | ✅ | 메트로놈(dspTime 예약) + 탭 오프셋 중앙값 측정 + GameSettings 저장, 검증됨 |
| 13 | **Settings 씬** | ✅ | 노트속도/좌우손/3종 볼륨 슬라이더 + 오프셋 ± + Save/Back 와이어링, 저장 검증됨 |
| 14 | **Tutorial 씬** | ✅ | chanran_bit 채보 + TutorialController + 씬 생성 완료 |
| 15 | **ChartEditorWindow** 채보 제작 | 🔲 | 로직은 있으나 UI/사용법 검증 필요 |
| 16 | 노래 2~3곡 추가 + 풀 플레이 테스트 | ✅ | song_001 2분 분량 확장 완료 |
| 17 | Quest 빌드 + OVR 최적화 + 90fps 측정 | 🔶 | 설정/코드 최적화 완료(아래 참조). **실제 APK 빌드·90fps 실측은 기기 연결 후 대기** |
| 18 | 이펙트/사운드/햅틱 폴리싱 | 🔶 | SliceEffect, SaberTrail, EzySlice + **히트/미스/색오류 효과음(절차적 SFX) 연결 완료**. 햅틱 폴리싱 미완 |
| 19 | (선택) 리플레이 시스템 | 🔲 | - |
| 20 | README + 시연 영상 | 🔲 | - |

---

## 오늘 완료한 작업 (채보/플레이어블 폴리싱 세션)

- **채보 Beat Saber 스타일 전면 재작성**: 빨강=왼손(레인 0-1)/파랑=오른손(레인 2-3) 완전 분리, 16박자 사이클로 자연스러운 방향 흐름(down→up→diagonal 반복). Easy 1박/beat, Normal 0.5박/beat.
- **banjjak_dalryeoga 채보 재생성**: Easy 317개, Normal 633개 노트. UTF-8 BOM 없음 확인.
- **시동 걸어 신규 추가**: `StreamingAssets/Songs/sidong_georeo/` 생성, MP3 복사, info.json(BPM 120) + chart_easy(395개)/chart_normal(789개) 완성.
- **테스트 노래(song_001) 삭제**: 폴더 및 .meta 제거.
- **자르면 체력 회복 구현**: `ScoreManager.RegisterHit()`에 힐 추가 — Perfect +5, Great +3, Good +1 HP.
- **HUD 위치 최적화**: distanceFromCamera 1→2.5m, heightOffset 0→+0.3(노트 레인 위), followSpeed 5→3(멀미 감소).
- **노트 그리드 시야각 개선**: `GameConfig.baseHeight` 0.7→0.5 (상단 노트 1.7m→1.5m, 플레이어가 약간 내려다보는 비트 세이버 각도).
- **난이도 버튼 선택 하이라이트**: `SongSelectUI` — 선택된 난이도 노란색, 미선택 흰색.

---

## 이전 완료한 작업 (사운드/씬/최적화 세션)

- **사운드 연결 (#18)**: `Audio/SfxManager.cs` 신규 — 효과음 에셋 없이 절차적 신스 톤 생성(히트=고음, 색오류=중음, 미스=저음). `ScoreManager` 히트/미스/색오류 시점에 연결, `GameSettings.sfxVolume × masterVolume` 볼륨 연동. 런타임 재생 검증 완료.
- **Calibration 씬 (#12)**: `Calibration/CalibrationController.cs` 신규 — dspTime 예약 메트로놈(소스 풀) + 탭 오프셋 중앙값 측정 + `userOffset` 저장. `CreateScenes`에 빌더 추가, Build Settings index 3. 측정→저장→영속화 검증.
- **Settings 씬 (#13)**: `CreateScenes`에 슬라이더/토글 빌더 + Settings 씬 빌더 추가. `SettingsUI` 전체 와이어링(슬라이더 onValueChanged 영구 리스너 포함), Build Settings index 4. 값 변경→저장→영속화 검증.
- **Quest 최적화 (#17, 설정 한정)**: 프로젝트가 이미 Quest 준비 상태 확인 — IL2CPP/ARM64/minSdk32/Linear/Vulkan, OpenXR **SinglePassInstanced(Multiview)** + **MetaQuestFeature** 활성. `OVRSettings`를 `RuntimeInitializeOnLoadMethod`로 전역 90fps 적용하도록 개선. **실제 APK 빌드는 사용자 요청으로 보류(기기 필요).**

---

## 이전 완료한 작업 (게임 루프 연결 세션)

- **SongSelect→Gameplay 전환 크래시 수정**: `GameManager`가 `[Managers]`(Conductor 등 자식 보유)에 얹혀 있어, 중복 GameManager의 `Destroy(gameObject)`가 Conductor까지 파괴 → `Conductor.Instance` null → NRE. `Destroy(this)`(컴포넌트만 제거)로 변경해 자식 매니저 보존.
- **Gameplay→Result 점수 전달 구현**: `ScoreManager`는 씬 단위 싱글톤이라 전환 시 파괴됨. `GameResult` 스냅샷을 신규 추가하고, `GameManager.LastResult`에 전환 직전 캡처 + `SaveSystem` 저장/신기록 판정. `ResultUI`는 `LastResult`에서 읽도록 변경.
- **전체 루프 검증 완료**: SongSelect → Gameplay(점수) → Result(점수·정확도·랭크·NEW RECORD 표시 + 최고점수 저장) → SongSelect(Best 표시)까지 end-to-end 동작 확인.

---

## 이전 완료한 작업 (비주얼 폴리싱 세션)

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

## 오늘 완료한 작업 (UI/Tutorial/배경 세션)

- **HUD 위치 조정**: `HUD.cs` heightOffset `-0.3f` → `-0.5f` (플레이어 눈높이 기준 너무 높다는 피드백 반영)
- **Tutorial 씬 준비 (#14)**:
  - `StreamingAssets/Songs/chanran_bit/` 생성 — `찬란한_빛.mp3` 복사, `info.json`(BPM 120, Easy Lv.1), `chart_easy.json`(28노트/30초, 3단계 튜토리얼 채보) 완성
  - `Scripts/UI/TutorialController.cs` 신규 — chanran_bit 자동 로드, 시작하기/뒤로 버튼
  - `CreateScenes.cs`에 `VRBeat > Create Tutorial Scene` 메뉴 추가
  - **유니티에서 할 일**: `VRBeat > Create Tutorial Scene` 실행하면 Tutorial.unity 생성됨
- **배경 스피어 시스템 (신규)**:
  - `Shaders/BackgroundSphere.shader` — Cull Front + 격자 패턴, CGPROGRAM 방식 (URP 호환)
  - `Scripts/Gameplay/BackgroundSphere.cs` — Conductor beat마다 빨강↔파랑 emission 펄스
  - `CreateScenes.cs`에 `VRBeat > Create Background Sphere` 메뉴 추가
  - **유니티에서 할 일**: Gameplay 씬 열고 `VRBeat > Create Background Sphere` 실행

---

## 다음 즉시 할 일

1. **햅틱 폴리싱** (#18) — 슬라이스 속도 비례 진동, 판정 등급별 진동 강도 차별화
2. **시동 걸어 BPM 검증** — 현재 120 설정, 실제 박자와 맞지 않으면 `sidong_georeo/info.json`의 `"bpm"` 값 조정
3. **(기기 연결 시) Quest APK 빌드 + 90fps 실측**

---

## 환경 비주얼 TODO

| # | 항목 | 상태 | 메모 |
|---|---|---|---|
| E1 | **바닥 제작** — 노트 레인 바닥 + 플레이어 발밑 | 🔲 | Beat Saber 스타일 어둡고 광택 있는 반사 바닥. 레인 구분선 포함 |
| E2 | **오실로스코프 벽** — 좌우 벽면 오디오 리액티브 | 🔲 | AudioSpectrum 데이터 → 실시간 높낮이 변하는 그래프 형태. 벽 생기면 LightPillar 빛 반사로 원작과 훨씬 유사해짐 |

## 최근 완료 (UI/배경 세션)

- **VideoSkybox 전 씬 적용**: Settings, Calibration, Tutorial, SongSelect, Result, Gameplay 모두 motion.mp4 스카이박스 등록
- **HUD VR 최적화**: 캔버스 900×180, distanceFromCamera 2.5m, heightOffset +0.25m(노트 레인 위). 레이아웃: 좌=판정, 중=콤보(노랑), 우=점수, 하단=초록 HP바
- **SongSelect 배경/패널 반투명** (alpha 0.72/0.82) — VR 영상 비쳐 보임
- **Result 배경 반투명** (alpha 0.78), 캔버스 650×700, 랭크 폰트 110px
- **난이도 버튼 텍스트 검은색** — 흰 배경에 흰 글씨 문제 수정
- **Tutorial NEXON Lv2 Gothic 폰트** 적용 (한글 지원)

---

## 알려진 제한 사항 및 조치

- `SceneLoader.Load()`: `Result`, `SongSelect` 씬 부재 시 크래시 방지를 위해 `Application.CanStreamedLevelBeLoaded` 체크 로직 추가됨. (추후 씬 제작 시 자동 해결 예정)
- `NoteSpawner.cs`: 테스트를 위해 곡 선택 없이 실행 시 `song_001`을 강제 로드하도록 수정됨.
- `SongLibrary.LoadAllSongs()`: Android(Quest) 빌드 전 `manifest.json` 기반 로딩 교체 필요.
- `audio.ogg` -> `audio.mp3` 지원하도록 `SongLoader.cs` 수정 완료.
