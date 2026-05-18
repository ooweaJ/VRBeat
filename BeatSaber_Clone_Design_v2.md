# Beat Saber 모작 VR 포트폴리오 설계서 v2

**스택:** Unity 6 + Meta Quest (XR Interaction Toolkit)
**노트:** 기본 + 롱노트
**데이터:** JSON

---

## 1. 핵심 설계 원칙

### 1-1. 박자-거리 동기화 (가장 중요)

노트가 멀리서 날아와 정확한 박자 시점에 플레이어 앞에 도달해야 한다.

```
spawnTime = noteBeatTime - (spawnDistance / noteSpeed)
```

매 프레임 `AudioSettings.dspTime` 기준으로 동기화. `Time.deltaTime`은 누적 오차 생기므로 절대 쓰지 않음.

```csharp
double songTime = AudioSettings.dspTime - songStartDspTime;
float distanceFromPlayer = (note.beatTime - (float)songTime) * noteSpeed;
note.transform.position = spawnAnchor + forward * distanceFromPlayer;
```

### 1-2. 노트 이동 방식

**위치 직접 계산 방식 채택** (Rigidbody/Translate 비추천). 매 프레임 dspTime 기준으로 재계산 → 프레임 드랍/끊김에도 박자 안 밀림.

### 1-3. 오디오 레이턴시 보정

사용자별 오프셋 설정값(`userOffset`) 필수. 캘리브레이션 씬 따로 만들어서 박수 치기로 측정.

### 1-4. Pause/Resume 시간 동결

일시정지 시 dspTime은 계속 흐르므로, 정지 시간만큼 `dspStartTime`을 보정해야 한다.

```csharp
public void Pause()
{
    pauseDspTime = AudioSettings.dspTime;
    audioSource.Pause();
}

public void Resume()
{
    double pausedDuration = AudioSettings.dspTime - pauseDspTime;
    dspStartTime += pausedDuration;
    audioSource.UnPause();
}
```

---

## 2. 폴더 구조

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/              # GameManager, SceneLoader, SaveSystem
│   │   ├── Song/              # SongData, SongLoader, SongLibrary
│   │   ├── Chart/             # ChartData, NoteData, ChartParser
│   │   ├── Gameplay/          # NoteSpawner, Conductor, NoteBase, NormalNote, LongNote
│   │   ├── Saber/             # SaberController, SaberHitDetector, SliceEffect, SaberTrailCheck
│   │   ├── Scoring/           # ScoreManager, ComboSystem, AccuracyCalc, HealthSystem
│   │   ├── UI/                # SongSelectUI, ResultUI, HUD, PauseMenu, SettingsUI
│   │   ├── VR/                # XRRigSetup, HapticFeedback
│   │   ├── Editor/            # ChartEditorWindow (커스텀 에디터)
│   │   └── Performance/       # PerformanceMonitor, OVRSettings
│   ├── Prefabs/
│   │   ├── Notes/             # NormalNote.prefab, LongNote.prefab
│   │   ├── Sabers/            # LeftSaber.prefab, RightSaber.prefab
│   │   └── Environment/
│   ├── ScriptableObjects/
│   │   └── GameConfig.asset   # 전역 설정 (속도, 거리 등)
│   ├── Scenes/
│   │   ├── Boot.unity
│   │   ├── SongSelect.unity
│   │   ├── Gameplay.unity
│   │   ├── Result.unity
│   │   ├── Calibration.unity
│   │   ├── Tutorial.unity
│   │   └── Settings.unity
│   └── Resources/
└── StreamingAssets/
    └── Songs/
        ├── song_001/
        │   ├── info.json
        │   ├── audio.ogg
        │   ├── cover.png
        │   └── chart_normal.json
        └── song_002/
            └── ...
```

**핵심:** 노래 추가는 `StreamingAssets/Songs/` 폴더에 폴더 하나 드롭하면 끝. 빌드 후에도 폴더 추가 가능.

---

## 3. 데이터 포맷 (JSON)

### 3-1. `info.json` (노래 메타데이터)

```json
{
  "songId": "song_001",
  "title": "Sample Track",
  "artist": "Artist Name",
  "mapper": "YourName",
  "bpm": 120,
  "audioFile": "audio.ogg",
  "coverFile": "cover.png",
  "previewStart": 30.0,
  "previewDuration": 10.0,
  "songOffset": 0.0,
  "difficulties": [
    { "name": "Easy", "chartFile": "chart_easy.json", "level": 3 },
    { "name": "Normal", "chartFile": "chart_normal.json", "level": 5 },
    { "name": "Hard", "chartFile": "chart_hard.json", "level": 8 }
  ]
}
```

### 3-2. `chart_*.json` (채보)

```json
{
  "version": "1.0",
  "noteSpeed": 10.0,
  "notes": [
    {
      "type": "normal",
      "beat": 4.0,
      "lane": 1,
      "row": 0,
      "direction": "down",
      "color": "red"
    },
    {
      "type": "long",
      "beat": 8.0,
      "duration": 2.0,
      "lane": 2,
      "row": 1,
      "direction": "right",
      "color": "blue"
    }
  ]
}
```

**필드 설명:**

- `beat`: 박자 단위 시간 (BPM으로 초 변환)
- `lane`: 0~3 (좌→우 4칸)
- `row`: 0~2 (하→상 3줄)
- `direction`: up/down/left/right/upLeft/upRight/downLeft/downRight/any
- `color`: red/blue (세이버 색 매칭)
- `duration`: 롱노트 길이 (박자 단위)

---

## 4. 핵심 클래스 설계

### 4-1. Conductor (박자 마스터)

모든 시간의 기준. 싱글톤.

```csharp
public class Conductor : MonoBehaviour
{
    public static Conductor Instance;
    public AudioSource audioSource;
    public float bpm;
    public float songOffset;       // 채보 작성자가 설정
    public float userOffset;       // 사용자 캘리브레이션

    private double dspStartTime;
    private double pauseDspTime;
    private bool isPaused;

    public double SongTime => AudioSettings.dspTime - dspStartTime - songOffset - userOffset;
    public float SongBeat => (float)(SongTime * bpm / 60.0);
    public float SecondsPerBeat => 60f / bpm;
    public bool IsSongFinished => audioSource.clip != null && SongTime >= audioSource.clip.length;

    public void StartSong(AudioClip clip)
    {
        audioSource.clip = clip;
        dspStartTime = AudioSettings.dspTime + 0.5;
        audioSource.PlayScheduled(dspStartTime);
    }

    public void Pause()
    {
        pauseDspTime = AudioSettings.dspTime;
        audioSource.Pause();
        isPaused = true;
    }

    public void Resume()
    {
        double pausedDuration = AudioSettings.dspTime - pauseDspTime;
        dspStartTime += pausedDuration;
        audioSource.UnPause();
        isPaused = false;
    }

    public float BeatToSeconds(float beat) => beat * SecondsPerBeat;
}
```

### 4-2. NoteSpawner (스폰 매니저)

시간 되면 노트 생성. 풀링 + 워밍업 필수.

```csharp
public class NoteSpawner : MonoBehaviour
{
    public ChartData chart;
    public float spawnDistance = 30f;    // 멀리서 보이는 거리
    public float hitDistance = 0f;       // 플레이어 앞 타격 지점
    public float despawnDistance = -2f;  // 미스 처리 지점
    public float noteSpeed = 10f;        // m/s

    private int nextNoteIndex = 0;
    private NotePool pool;
    private List<NoteBase> activeNotes = new List<NoteBase>();

    void Start()
    {
        pool.Warmup(64); // GC 끊김 방지 사전 워밍업
    }

    void Update()
    {
        float currentBeat = Conductor.Instance.SongBeat;
        float spawnLeadBeats = (spawnDistance / noteSpeed) / Conductor.Instance.SecondsPerBeat;

        // 스폰
        while (nextNoteIndex < chart.notes.Count &&
               chart.notes[nextNoteIndex].beat <= currentBeat + spawnLeadBeats)
        {
            SpawnNote(chart.notes[nextNoteIndex]);
            nextNoteIndex++;
        }

        // 미스 감지 및 풀 반환
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i].transform.position.z < despawnDistance)
            {
                ScoreManager.Instance.RegisterMiss(activeNotes[i]);
                pool.Return(activeNotes[i]);
                activeNotes.RemoveAt(i);
            }
        }

        // 곡 종료 → Result
        if (Conductor.Instance.IsSongFinished && activeNotes.Count == 0
            && nextNoteIndex >= chart.notes.Count)
        {
            GameManager.Instance.GoToResult();
        }
    }

    void SpawnNote(NoteData data)
    {
        NoteBase note = pool.Get(data.type);
        note.Initialize(data, noteSpeed, hitDistance);
        activeNotes.Add(note);
    }
}
```

### 4-3. NoteBase / NormalNote / LongNote

```csharp
public abstract class NoteBase : MonoBehaviour
{
    protected NoteData data;
    protected float noteSpeed;
    protected float hitZ;

    public NoteData Data => data;

    public virtual void Initialize(NoteData d, float speed, float hitZ)
    {
        data = d;
        noteSpeed = speed;
        this.hitZ = hitZ;
        ApplyDirectionRotation();
        UpdatePosition();
    }

    protected virtual void Update() => UpdatePosition();

    protected virtual void UpdatePosition()
    {
        float beatsRemaining = data.beat - Conductor.Instance.SongBeat;
        float secondsRemaining = beatsRemaining * Conductor.Instance.SecondsPerBeat;
        float zPos = hitZ + secondsRemaining * noteSpeed;

        Vector3 pos = LaneToWorldPos(data.lane, data.row);
        pos.z = zPos;
        transform.position = pos;
    }

    void ApplyDirectionRotation()
    {
        // direction 문자열 → Quaternion 변환 (화살표 회전)
        transform.localRotation = DirectionToRotation(data.direction);
    }

    public abstract void OnSliced(Vector3 sliceDirection, float velocity, SaberColor saberColor);
}

public class NormalNote : NoteBase
{
    public const float MIN_SLICE_VELOCITY = 2.0f;  // 최소 속도 임계값

    public override void OnSliced(Vector3 sliceDir, float velocity, SaberColor saberColor)
    {
        // 색 매칭 체크
        if (!ColorMatches(saberColor, data.color))
        {
            ScoreManager.Instance.RegisterWrongColor(this);
            return;
        }

        // 속도 임계값
        if (velocity < MIN_SLICE_VELOCITY)
        {
            return; // 너무 느린 슬라이스는 무시
        }

        // 방향 정확도
        float dot = Vector3.Dot(sliceDir.normalized, GetRequiredDirection());
        bool correctDirection = dot > 0.7f || data.direction == "any";

        ScoreManager.Instance.RegisterHit(this, correctDirection, velocity);
        // 슬라이스 이펙트 + 파괴
    }
}

public class LongNote : NoteBase
{
    public Transform head, tail, body;
    private bool isHeld = false;
    private float holdScore = 0f;

    public override void Initialize(NoteData d, float speed, float hitZ)
    {
        base.Initialize(d, speed, hitZ);
        float lengthInSeconds = d.duration * Conductor.Instance.SecondsPerBeat;
        float lengthInMeters = lengthInSeconds * speed;
        body.localScale = new Vector3(1, 1, lengthInMeters);
        tail.localPosition = new Vector3(0, 0, lengthInMeters);
    }

    // 머리 슬라이스 → isHeld true → 지속 시간 동안 세이버 트리거 체크
    // 꼬리까지 유지하면 만점, 중간에 빠지면 부분 점수
    public override void OnSliced(Vector3 sliceDir, float velocity, SaberColor color)
    {
        if (!isHeld && ColorMatches(color, data.color))
        {
            isHeld = true;
            // 홀드 시작
        }
    }
}
```

### 4-4. SaberController / 터널링 방지

```csharp
public class SaberController : MonoBehaviour
{
    public SaberColor color;
    public Handedness handedness;
    public Transform tip, root;

    private Vector3 lastTipPos;
    public Vector3 Velocity { get; private set; }

    void Update()
    {
        Velocity = (tip.position - lastTipPos) / Time.deltaTime;

        // 터널링 방지: 이전 프레임 위치 → 현재 위치 사이 Raycast
        Vector3 dir = tip.position - lastTipPos;
        float dist = dir.magnitude;
        if (dist > 0.01f && Physics.Raycast(lastTipPos, dir.normalized, out RaycastHit hit, dist))
        {
            if (hit.collider.TryGetComponent(out NoteBase note))
            {
                note.OnSliced(Velocity.normalized, Velocity.magnitude, color);
                HapticFeedback.Pulse(handedness, 0.5f, 0.1f);
            }
        }

        lastTipPos = tip.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out NoteBase note))
        {
            note.OnSliced(Velocity.normalized, Velocity.magnitude, color);
            HapticFeedback.Pulse(handedness, 0.5f, 0.1f);
        }
    }
}
```

### 4-5. SongLibrary (노래 로드)

```csharp
public class SongLibrary : MonoBehaviour
{
    public List<SongData> LoadAllSongs()
    {
        var songs = new List<SongData>();
        string songsPath = Path.Combine(Application.streamingAssetsPath, "Songs");
        foreach (var dir in Directory.GetDirectories(songsPath))
        {
            string infoPath = Path.Combine(dir, "info.json");
            if (!File.Exists(infoPath)) continue;
            var info = JsonUtility.FromJson<SongInfo>(File.ReadAllText(infoPath));
            songs.Add(new SongData { info = info, folderPath = dir });
        }
        return songs;
    }

    public IEnumerator LoadAudio(string path, Action<AudioClip> onLoaded) { /* UnityWebRequest */ }
    public IEnumerator LoadCover(string path, Action<Sprite> onLoaded) { /* UnityWebRequest */ }
}
```

**Quest 주의:** Android StreamingAssets는 `UnityWebRequest`로 비동기 로드 필수.

**오디오 로드 타입:**
- 곡 본편: `AudioClipLoadType.Streaming` (메모리 절약)
- 효과음/슬라이스: `DecompressOnLoad` (즉시 재생)

---

## 5. 점수 / 콤보 / 체력 시스템

```csharp
public class ScoreManager : MonoBehaviour
{
    public int score, combo, maxCombo;
    public int totalHits, perfectHits, missedHits;

    public void RegisterHit(NoteBase note, bool correctDir, float velocity)
    {
        float beatDiff = Mathf.Abs(note.Data.beat - Conductor.Instance.SongBeat);
        float secDiff = beatDiff * Conductor.Instance.SecondsPerBeat;

        HitGrade grade = secDiff < 0.05f ? HitGrade.Perfect :
                         secDiff < 0.12f ? HitGrade.Great : HitGrade.Good;

        int baseScore = grade == HitGrade.Perfect ? 100 :
                        grade == HitGrade.Great ? 70 : 40;
        if (!correctDir) baseScore /= 2;

        score += baseScore * Mathf.Max(1, combo / 8); // 콤보 배율
        combo++;
        maxCombo = Mathf.Max(maxCombo, combo);
        totalHits++;
        if (grade == HitGrade.Perfect) perfectHits++;
    }

    public void RegisterMiss(NoteBase note)
    {
        combo = 0;
        missedHits++;
        HealthSystem.Instance.TakeDamage(10);
    }

    public void RegisterWrongColor(NoteBase note)
    {
        combo = 0;
        HealthSystem.Instance.TakeDamage(15);
    }
}

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance;
    public float maxHealth = 100, currentHealth = 100;

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0) GameManager.Instance.GameOver();
    }
}
```

---

## 6. 세이브 시스템 (최고 점수)

```csharp
public class SaveSystem
{
    [Serializable]
    public class SongRecord
    {
        public string songId;
        public string difficulty;
        public int highScore;
        public float accuracy;
        public bool fullCombo;
    }

    static string SavePath => Path.Combine(Application.persistentDataPath, "saves.json");

    public static void SaveRecord(SongRecord record) { /* JSON 직렬화 */ }
    public static SongRecord LoadRecord(string songId, string diff) { /* JSON 역직렬화 */ }
}
```

---

## 7. 씬별 구성

| 씬 | 역할 |
|---|---|
| Boot | 초기화, SongLibrary 로드, Settings 로드, SongSelect로 이동 |
| SongSelect | VR UI로 노래/난이도 선택, 프리뷰 재생, 최고 점수 표시 |
| Gameplay | Conductor + Spawner + Sabers + HUD + PauseMenu |
| Result | 점수, 정확도, 콤보, 그래프, 신기록 알림 |
| Calibration | 박수치며 userOffset 측정 |
| Tutorial | 기본 조작 / 슬라이스 / 롱노트 튜토리얼 |
| Settings | 속도, 좌/우손, 볼륨, 오프셋 표시 |

---

## 8. 설정 메뉴 (Settings)

```csharp
[Serializable]
public class GameSettings
{
    public float noteSpeed = 10f;
    public bool leftHandedMode = false;
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float userOffset = 0f;
    public bool reducedMotion = false;
}
```

PlayerPrefs 또는 `Application.persistentDataPath/settings.json`에 저장.

---

## 9. 채보 에디터 (EditorWindow)

```csharp
public class ChartEditorWindow : EditorWindow
{
    // 오디오 파형 표시
    // 마우스로 박자 위치 클릭 → 노트 추가
    // lane/row/direction/color 선택 UI
    // 재생하며 미리보기
    // JSON 저장/로드
}
```

포트폴리오 어필 포인트 중 큰 비중. 채보 제작 워크플로우 강조.

---

## 10. Quest 최적화

### 10-1. OVR 설정

```csharp
void Awake()
{
    OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.High;
    OVRManager.useDynamicFixedFoveatedRendering = true;
    OVRManager.cpuLevel = 2;
    OVRManager.gpuLevel = 3;
}
```

### 10-2. GPU 인스턴싱

노트 머터리얼 `Enable GPU Instancing` 체크. 동시에 떠 있는 노트가 많을 때 드로우콜 절감.

### 10-3. 퍼포먼스 모니터

```csharp
public class PerformanceMonitor : MonoBehaviour
{
    void Update()
    {
        float frameTimeMs = Time.unscaledDeltaTime * 1000f;
        // OVRMetrics와 연동해 로그 또는 디버그 HUD
    }
}
```

목표: **Quest 2 기준 11.1ms/frame (90fps) 유지**

---

## 11. 리플레이 시스템 (선택)

```csharp
[Serializable]
public class ReplayData
{
    public string songId;
    public List<InputFrame> frames; // 양손 위치/회전 시계열
}
```

입력 시퀀스만 저장하면 용량 작고 재현 가능. 고스트 모드 또는 베스트 리플레이 공유.

---

## 12. 구현 순서 (Claude Code 작업 단위)

1. **프로젝트 셋업** - URP, XR Toolkit, Meta XR SDK, .gitignore + Git LFS 설정
2. **Conductor + 테스트 씬** - 박자에 맞춰 큐브 생성으로 동기화 검증
3. **NoteData/ChartData/SongLibrary** - JSON 로드 검증
4. **NoteSpawner + NotePool 워밍업 + NormalNote** - 정확한 박자 도달, 풀링, 미스 감지
5. **Saber 컨트롤러 + 충돌 + 슬라이스 + 터널링 방지** - VR 입력 연결, 트레일 레이캐스트
6. **색 매칭 + 속도 임계값 + 방향 회전**
7. **LongNote 구현** - 머리/꼬리/유지 로직
8. **점수/콤보/체력 시스템**
9. **HUD + Pause 시스템**
10. **SongSelect VR UI + 최고 점수 표시**
11. **Result 씬 + Save 시스템**
12. **Calibration 씬 (userOffset 측정)**
13. **Settings 씬**
14. **Tutorial 씬**
15. **ChartEditorWindow 제작**
16. **노래 2~3개 제작해서 풀 플레이 테스트**
17. **Quest 빌드 + OVR 최적화 + 퍼포먼스 측정**
18. **이펙트/사운드/햅틱 폴리싱**
19. **(선택) 리플레이 시스템**
20. **README 작성 + 시연 영상 녹화**

---

## 13. 버전 관리 / Git

### `.gitignore` 필수 항목
```
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/
*.csproj
*.unityproj
*.sln
*.suo
*.user
```

### Git LFS 추적 대상
```
*.psd, *.fbx, *.wav, *.mp3, *.ogg, *.png, *.jpg, *.exr, *.unity, *.prefab, *.asset
```

---

## 14. 포트폴리오 어필 포인트 (README에 강조)

1. **dspTime 기반 박자 동기화 정밀도** - 프레임 드랍 시에도 박자 안 밀리는 설계
2. **JSON 기반 모듈러 컨텐츠 확장성** - 폴더 드롭만으로 노래 추가
3. **오브젝트 풀링 + 위치 직접 계산** - Quest 90fps 유지
4. **세이버 속도/각도 기반 점수 판정** - 단순 충돌이 아닌 정교한 판정
5. **레이캐스트 기반 터널링 방지** - 빠른 스윙도 정확히 감지
6. **사용자별 레이턴시 캘리브레이션** - 디바이스 차이 보정
7. **커스텀 채보 에디터** - 컨텐츠 제작 워크플로우
8. **퍼포먼스 측정 결과 첨부** - 실측 데이터로 최적화 입증

---

## 15. 최종 체크리스트

- [ ] dspTime 기반 Conductor 동작 확인
- [ ] JSON 로드 정상
- [ ] 노트가 정확한 박자에 hitZ 도달
- [ ] 색 매칭 / 방향 / 속도 임계값 모두 작동
- [ ] 미스 처리 / 풀 반환 / 곡 종료 감지
- [ ] LongNote 머리→꼬리 홀드 점수
- [ ] Pause/Resume 후에도 박자 안 밀림
- [ ] 캘리브레이션으로 userOffset 저장
- [ ] 최고 점수 저장/표시
- [ ] Quest 2에서 90fps 유지
- [ ] 노래 폴더 추가 시 SongSelect에 자동 반영
- [ ] 시연 영상 녹화

---

이 설계서대로 Claude Code에 단계별로 작업 지시하면 됨. 1단계부터 순차 진행 권장.
