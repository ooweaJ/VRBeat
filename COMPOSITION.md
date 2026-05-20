# VRBeat Compound Engineering Plan (자동화 및 협업 계획서)
이 문서는 AI 에이전트가 시스템 구축(Wiring)을 자동화하고, 사용자가 시각적 배치(Design)를 수행하는 "컴파운드 엔지니어링" 방식의 진행 로드맵입니다.

---

## 1. 역할 분담 원칙 (The Hybrid Logic)

| 영역 | AI 에이전트 (Automation) | 사용자 (Manual/Design) |
|---|---|---|
| **코드/로직** | 스크립트 작성, 버그 수정, 인터페이스 설계 | 코드 리뷰 및 로직 승인 |
| **시스템 구축** | 에디터 툴 제작, 자동 오브젝트 생성, 컴포넌트 연결(Wiring) | 에디터 툴 실행, 최종 연결 확인 |
| **비주얼/배치** | 프리팹 뼈대 생성 (Mesh, Collider, Script) | 오브젝트 위치(Transform) 조정, 환경 데코레이션 |
| **에셋 관리** | 데이터 파일(JSON, ScriptableObject) 자동 생성 | 실제 리소스(음원, 텍스처) 준비 및 적용 |

---

## 2. 자동화 구축 도구 리스트 (Editor Tools)

사용자는 유니티 상단 메뉴 `VRBeat > Setup Tools`에서 아래 기능들을 실행하여 시스템을 구축합니다.

### [Tool A] `Generate_GameConfig`
- **역할**: `Resources/GameConfig` 에셋 자동 생성 및 초기화.
- **자동화 내용**: `ScriptableObject` 생성, 기본 속도/거리 값 설정.

### [Tool B] `Create_Basic_Prefabs`
- **역할**: 게임에 필요한 최소 사양의 프리팹 세트 제작.
- **자동화 내용**: 
  - `NormalNote`: Cube + Script + Collider + Material
  - `LongNote`: Head/Body/Tail 구조 + Script
  - `Saber`: Cylinder + Script + Trigger + Tip/Root Transform

### [Tool C] `Setup_GameplayScene`
- **역할**: `Game` 씬의 뼈대 자동 조립.
- **자동화 내용**: 
  - `Managers` 그룹 생성 (GameManager, Conductor, ScoreManager 등)
  - `Gameplay` 그룹 생성 (NoteSpawner, NotePool)
  - **인스펙터 자동 연결**: Spawner에 Pool 연결, Conductor에 AudioSource 연결 등.

---

## 3. 진행도 체크리스트 (Milestones)

### Phase 1: 시스템 인프라 구축
- [ ] **A-1**: `Generate_GameConfig` 툴 제작 및 실행 (에셋 생성)
- [ ] **B-1**: `Create_Basic_Prefabs` 툴 제작 및 실행 (프리팹 생성)

### Phase 2: Gameplay 씬 조립
- [ ] **C-1**: `Setup_GameplayScene` 툴 제작 및 실행 (하이어라키 조립)
- [ ] **D-1**: (사용자) XR Origin 배치 및 세이버 프리팹 연결
- [ ] **D-2**: (사용자) 오브젝트 위치 최종 조정 및 배경 설정

### Phase 3: 데이터 및 콘텐츠 검증
- [ ] **E-1**: 테스트 음원(`audio.ogg`) 추가
- [ ] **E-2**: `song_001` 로드 및 노트 스폰 테스트
- [ ] **E-3**: 슬라이스 판정 및 점수 시스템 검증

---

## 4. 바로 시작할 작업

1. **`Assets/_Project/Scripts/Editor/SetupTools.cs`** 파일을 생성하여 위 도구들을 통합 구현하겠습니다.
2. 도구 구현이 완료되면, 사용자님은 유니티에서 버튼을 클릭하여 구축을 시작하시면 됩니다.

---
*본 계획서는 진행 상황에 따라 실시간으로 업데이트됩니다.*
