# VRBeat - 포트폴리오 & 기술 문서

> **Unity 기반 VR 리듬게임** | Meta Quest 3 | C# / Python  
> 개발자: ooweaJ  
> 문서 갱신 기준: 2026-05-29

---

## 1. 프로젝트 개요

VRBeat는 **VR 공간에서 전통 리듬게임의 정박 타격감**을 구현하는 1인 개발 프로젝트입니다.

플레이어는 Meta Quest 3 컨트롤러를 양손 세이버처럼 사용해, 음악 박자에 맞춰 다가오는 빨강/파랑 노트를 베어냅니다. Beat Saber처럼 VR에서 몸을 움직이는 액션성을 가져오되, 핵심 재미는 "멋있게 베는 것"보다 **노트가 판정선에 닿는 정확한 순간을 맞추는 리듬감**에 두었습니다.

```
Beat Saber   -> 방향과 궤적 중심의 베기 액션
VRBeat       -> 박자와 판정 중심의 정박 타격감
```

현재 프로젝트는 SongSelect -> Gameplay -> Result로 이어지는 기본 게임 루프, 튜토리얼, 설정, 캘리브레이션, 점수 저장, VR 월드 UI, 노트 슬라이스 이펙트, 음악 동기화형 환경 연출까지 구현된 상태입니다.

---

## 2. 핵심 콘셉트

### 2-1. 전통 리듬게임식 판정선

노트는 VR 공간의 정면 레인을 따라 플레이어 쪽으로 접근하고, 판정선에 도달하는 순간에 맞춰 베어야 합니다. 방향 입력보다 **타이밍 정확도**가 더 크게 체감되도록 설계했습니다.

### 2-2. 양손 색상 분리

빨강 노트는 왼손, 파랑 노트는 오른손으로 처리합니다. 채보도 빨강은 왼쪽 2레인, 파랑은 오른쪽 2레인 중심으로 배치해 플레이 중 손이 자연스럽게 분리되도록 만들었습니다.

### 2-3. 리듬과 반응하는 VR 무대

바닥 레인, 링, 레이저, 배경 스카이박스, 사이드 이퀄라이저가 음악 박자와 슬라이스 이벤트에 반응합니다. 중앙 시야는 노트 가독성을 위해 비워두고, 주변부에서 무대감과 속도감을 만드는 방향으로 구성했습니다.

---

## 3. 차별화 포인트

| 구분 | Beat Saber | VRBeat |
|---|---|---|
| 핵심 재미 | 방향성 있는 베기 액션 | 정박 타이밍과 리듬 정확도 |
| 판정 중심 | 베는 방향/궤적 | 판정선 도달 시점 |
| 노트 접근 | 양쪽에서 입체적으로 접근 | 전통 리듬게임처럼 레인을 따라 접근 |
| 색상 규칙 | 색상별 손 분리 | 색상별 손 분리 + 레인 분리 강화 |
| 싱크 보정 | 고정 오프셋 중심 | 사용자 캘리브레이션 + 수동 오프셋 |
| 채보 제작 | 수작업 또는 외부 툴 | BeatSage 변환 + 자체 자동 채보 생성기 |
| 환경 연출 | 라이트쇼 중심 | 박자/슬라이스 연동 라이트쇼 |

---

## 4. 핵심 기술 도전 - 오디오 싱크

### 4-1. 문제 정의

VR 리듬게임에서 가장 민감한 문제는 **오디오 출력 지연(Audio Output Latency)** 입니다.

특히 독립형 VR 기기에서는 실제 음악이 플레이어에게 들리는 시점과 게임 로직상 판정 시점 사이에 차이가 발생할 수 있습니다. 이 차이를 방치하면 노트가 시각적으로는 맞아도 소리가 늦게 들려 "박자가 안 맞는다"는 느낌이 생깁니다.

```
[노트가 판정선에 닿는 시각] ---- 오디오 출력 지연 ----> [플레이어가 소리를 듣는 시각]
```

### 4-2. DSP 기반 타이밍

Unity의 `AudioSettings.dspTime`을 기준 시간으로 사용해 음악 재생, 노트 위치, 판정 계산을 같은 시간축에 맞췄습니다. `Time.time`이나 프레임 기반 타이머는 프레임 드랍에 영향을 받기 때문에 리듬게임 기준 시간으로 사용하지 않았습니다.

```csharp
public double SongTime =>
    AudioSettings.dspTime - dspStartTime - songOffset - userOffset;

public float SongBeat => (float)(SongTime * bpm / 60.0);
```

`noteSpeed`는 노트가 화면에서 이동하는 시각적 속도만 결정합니다. 실제 판정은 `SongBeat` 기준으로 처리되므로, 노트 속도를 조절해도 음악 타이밍 자체는 흔들리지 않습니다.

### 4-3. 2단계 캘리브레이션

캘리브레이션은 측정과 검증을 분리했습니다.

```
1단계 - 소리만 듣고 탭
  - 메트로놈 클릭을 AudioSource.PlayScheduled로 예약 재생
  - 플레이어가 클릭을 들은 뒤 버튼 입력
  - 5회 입력값의 중앙값을 userOffset으로 계산

2단계 - 비주얼 트랙 확인
  - 측정된 오프셋을 적용한 상태로 노트 트랙을 반복 표시
  - 노트가 판정선에 닿는 순간과 클릭음이 맞는지 사용자가 확인
  - 맞으면 저장, 아니면 다시 측정
```

처음부터 시각 정보를 같이 주면 사용자가 무의식적으로 예측 입력을 할 수 있어, 1단계에서는 소리만 사용했습니다. 이후 2단계에서 실제 플레이 감각을 눈으로 검증하게 해 수치 측정과 체감 싱크를 모두 잡는 구조로 만들었습니다.

---

## 5. 채보 및 콘텐츠 파이프라인

### 5-1. Beat Saber 맵 변환기

`Tools/beatsaber_to_vrbeat.py`는 BeatSage나 ChroMapper에서 생성한 Beat Saber 맵을 VRBeat 전용 JSON으로 변환합니다.

주요 처리:

- `Info.dat`에서 BPM, 곡 제목, 아티스트, 오프셋 추출
- Beat Saber v2/v3 노트 포맷 모두 파싱
- 빨강/파랑 색상, 레인, 높이, 방향을 VRBeat 포맷으로 변환
- 소수점 박자값을 1/4박자 그리드로 양자화
- HPSS 기반 하모닉 분석으로 긴 공백 구간에 롱노트 자동 삽입
- `StreamingAssets/Songs/{song_id}/` 구조로 출력

```python
def quantize_beat(beat, grid=0.25):
    return round(round(beat / grid) * grid, 4)
```

### 5-2. 자체 자동 채보 생성기

`Tools/generate_beatmap.py`는 mp3/wav/ogg 파일을 직접 분석해 `info.json`, `chart_easy.json`, `chart_normal.json`을 생성합니다.

주요 처리:

- `librosa.beat.beat_track` 기반 BPM 검출
- onset strength 기반 타격 지점 추출
- 스펙트럼 무게중심으로 노트 높이 결정
- 빨강/파랑 손 교대 및 좌우 레인 분리
- 이전 방향을 고려한 자연스러운 방향 흐름 생성
- 하모닉 성분이 이어지는 구간을 롱노트로 변환

### 5-3. 현재 수록 데이터

`Assets/StreamingAssets/Songs/` 기준으로 튜토리얼 1곡과 플레이용 곡들이 등록되어 있습니다.

| songId | 제목 | 난이도 |
|---|---|---|
| `chanran_bit` | 찬란한 빛 | Easy |
| `banjjak_dalryeoga` | 반짝 달려가 | Easy / Normal / Hard |
| `banjjak_switch` | 반짝 스위치 | Easy / Normal / Hard |
| `gwangran_hey_kids` | 광란 Hey Kids!! | Easy / Normal / Hard |
| `gwangran_test` | 광란 Hey Kids!! (원본) | Easy / Normal / Hard |
| `sidong_georeo` | 시동 걸어 | Easy / Normal / Hard |

---

## 6. 구현된 주요 시스템

### 6-1. 게임 루프

- **SongSelectUI**: 곡 목록 로드, 난이도 선택, 최고점수 표시, 선택 난이도 하이라이트
- **GameManager**: 선택 곡/난이도 보관, 씬 전환, 결과 스냅샷 관리
- **NoteSpawner**: 선택 곡의 오디오와 차트를 로드하고 DSP 기준으로 노트 스폰
- **ResultUI**: 점수, 정확도, 랭크, 신기록 표시
- **SaveSystem**: 곡/난이도별 최고점수, 정확도, Full Combo 저장

### 6-2. 노트와 판정

- **NormalNote**: 일반 노트 베기 판정
- **LongNote / LongNoteBody**: 헤드, 바디, 테일 구조의 롱노트 유지 판정
- **ScoreManager**: Perfect/Great/Good/Miss 판정, 콤보, 점수 배율, Hold 점수
- **HealthSystem**: 미스/색상 오류 시 체력 감소, 정확한 타격 시 체력 회복
- **HitGradePopup**: 판정 텍스트를 노트 위치에 표시

판정 기준은 시간 오차를 초 단위로 환산해 처리합니다.

```csharp
HitGrade grade = secDiff < 0.05f ? HitGrade.Perfect :
                 secDiff < 0.12f ? HitGrade.Great   : HitGrade.Good;
```

### 6-3. 세이버 및 타격감

- **SaberController / SaberHitDetector**: XR 컨트롤러 기반 세이버 입력과 충돌 판정
- **SaberTrailCheck**: 세이버 루트-팁 기반 커스텀 리본 트레일
- **NoteSliceHandler**: EzySlice 기반 노트 메시 분리
- **SliceEffect**: 슬라이스 파티클 이펙트 재생
- **SfxManager**: 히트/미스/색상 오류 효과음을 절차적 신스 톤으로 생성 및 재생
- **HapticFeedback**: VR 컨트롤러 진동 피드백 기반 마련

---

## 7. VR UI/UX

### 7-1. 월드 스페이스 UI

VR 환경에서는 일반 2D UI가 시야와 거리감에 맞지 않기 때문에 주요 UI를 World-space Canvas로 구성했습니다.

- HUD는 플레이어 앞 약 2.5m 거리에서 카메라를 부드럽게 따라감
- 좌측 판정, 중앙 콤보, 우측 점수, 하단 HP바로 구성
- SongSelect, Result, Settings, Calibration, Tutorial 씬 모두 VR 시야 안에서 조작 가능
- XR Interaction Toolkit 기반 레이 인터랙션 사용

### 7-2. 설정과 캘리브레이션

- 노트 속도 조절
- 마스터/음악/SFX 볼륨 조절
- 좌우손 설정 토글
- userOffset 수동 보정
- 2단계 캘리브레이션 씬 제공

### 7-3. 튜토리얼

`TutorialController`는 `chanran_bit` 튜토리얼 곡을 자동 로드해 30초 내외의 짧은 플레이로 기본 규칙을 익히게 합니다.

---

## 8. 환경 연출 및 비주얼 시스템

### 8-1. 음악 동기화 무대

- **VideoSkybox**: `motion.mp4`를 RenderTexture에 재생해 전 씬 배경으로 사용
- **HighwayFloor**: 바닥 레인과 구분선을 박자에 맞춰 발광
- **RotatingRings**: 노트 터널감과 깊이감을 주는 회전 링
- **SideLasers / CrossLaserSystem**: 좌우 레이저 스윕 및 크로스 연출
- **OscilloscopeWall**: AudioListener 스펙트럼 기반 사이드 이퀄라이저
- **MirrorFloorProbe**: 반사 바닥 연출용 Reflection Probe
- **BackgroundSphere**: 노트 스폰/히트 이벤트에 반응하는 배경 펄스

### 8-2. 슬라이스 반응형 라이트쇼

`EnvColorManager`가 환경 색상 상태를 관리하고, 노트를 벨 때 세이버 색상에 맞춰 링/레이저/바닥이 짧게 반응합니다. `SliceLightShow`는 상단 레이저 그룹, 후방 레이저, 링 그룹을 순차/랜덤 방식으로 점등해 실제 음악 공연 같은 반응을 만듭니다.

---

## 9. 성능 및 플랫폼 대응

타겟 플랫폼은 Meta Quest 3입니다.

적용된 설정과 구조:

- Unity 6 URP 기반 프로젝트
- OpenXR + XR Interaction Toolkit 사용
- Android IL2CPP / ARM64 / Vulkan / Linear Color 설정
- OpenXR Single Pass Instanced(Multiview) 사용
- `Application.targetFrameRate = 90`, `vSyncCount = 0` 자동 적용
- 노트 오브젝트 풀링 기본 64개
- UI/환경 빌더를 Editor 메뉴로 자동 생성해 씬 연결 실수를 줄임

아직 실제 APK 빌드와 Quest 3 실기기 90fps 측정은 최종 검증 단계로 남아 있습니다.

---

## 10. 기술 스택

| 분류 | 기술 |
|---|---|
| 엔진 | Unity 6, URP |
| VR | XR Interaction Toolkit 3.3, OpenXR, Meta Quest 3 |
| 언어 | C#, Python 3 |
| 오디오 | Unity DSP Time, AudioSource.PlayScheduled |
| 채보 분석 | librosa, numpy |
| 메시 슬라이스 | EzySlice |
| UI | TextMeshPro, World-space Canvas |
| 데이터 | JSON, StreamingAssets, Application.persistentDataPath |
| 최적화 | Object Pooling, Single Pass Instanced, 90fps target |

---

## 11. 개발 과정에서 배운 점

### 오디오 시간축과 게임 시간축은 다르다

리듬게임에서는 `Time.time`보다 `AudioSettings.dspTime`이 훨씬 중요합니다. 프레임 기반 시간으로는 음악과 판정이 미세하게 어긋날 수 있어, 오디오 엔진 기준 시간을 중심으로 노트 이동과 판정을 계산해야 했습니다.

### 사용자가 느끼는 싱크는 단순 수치가 아니다

사람은 리듬을 들으면 자연스럽게 예측 입력을 합니다. 그래서 단순 탭 측정만으로는 실제 체감 오프셋과 다를 수 있었습니다. 측정 단계와 확인 단계를 나눈 이유가 여기에 있습니다.

### 자동 생성 채보는 후처리가 중요하다

BeatSage나 onset 분석으로 만든 노트는 원본 그대로 쓰면 박자가 미묘하게 흐릿할 수 있습니다. 1/4박자 양자화, 레인/색상 규칙, 방향 흐름, 롱노트 삽입 같은 후처리를 거쳐야 실제 게임 플레이가 자연스러워졌습니다.

### VR UI는 거리와 시야가 곧 사용성이다

UI가 너무 가깝거나 높으면 읽기 힘들고 멀미감이 생길 수 있습니다. HUD와 메뉴는 실제 HMD 시야에서 반복 조정하며 거리, 높이, 투명도, 폰트 크기를 맞췄습니다.

---

## 12. 현재 상태와 남은 작업

### 구현 완료

- 기본 게임 루프: SongSelect -> Gameplay -> Result
- 곡 선택, 난이도 선택, 최고점수 저장
- 일반 노트/롱노트, 점수/콤보/체력 시스템
- 세이버 트레일, EzySlice 슬라이스, 파티클, 효과음
- 튜토리얼, 설정, 캘리브레이션 씬
- 음악 반응형 바닥/링/레이저/이퀄라이저/스카이박스
- Beat Saber 맵 변환기와 자체 자동 채보 생성기

### 남은 작업

- Quest 3 실기기 APK 빌드 및 90fps 측정
- Android StreamingAssets 곡 로딩용 manifest 방식 보강
- 햅틱 피드백 세부 폴리싱
- 환경 조명 배치와 원근감 추가 조정
- README 및 최종 시연 영상 정리

---

## 13. 스크린샷 / 데모

스크린샷은 `Assets/Screenshots/` 폴더에 정리되어 있습니다.

| 구분 | 파일 |
|---|---|
| 레퍼런스/플레이 화면 | `Assets/Screenshots/image.png` |
| 최근 캡처 | `Assets/Screenshots/screenshot-20260528-*.png` |

---

*이 문서는 포트폴리오 및 기술 설명을 겸한 개발 문서입니다.*
