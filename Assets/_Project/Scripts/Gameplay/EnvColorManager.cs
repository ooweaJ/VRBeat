using UnityEngine;

// 환경 조명 색 동기화 — 링 / 레이저 / 바닥 네온이 동일 색으로 함께 펄스.
// switchEveryBeats 마다 빨강↔파랑 전환 (여유있게 기본 8박자).
// minFlashInterval 로 연속 노트에서 펄스가 너무 빠르게 깜빡이는 것 방지.
public class EnvColorManager : MonoBehaviour
{
    public static EnvColorManager Instance { get; private set; }

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] public Color redRest  = new Color(2.5f,  0.08f, 0.08f);
    [ColorUsage(true, true)] public Color redBeat  = new Color(8f,    0.3f,  0.3f);
    [ColorUsage(true, true)] public Color blueRest = new Color(0.08f, 0.15f, 2.5f);
    [ColorUsage(true, true)] public Color blueBeat = new Color(0.3f,  0.5f,  8f);

    [Header("Timing")]
    [Range(1, 32)]   public int   switchEveryBeats   = 8;    // N박자마다 색 전환
    [Range(0.3f, 2f)] public float minFlashInterval  = 0.75f; // 최소 펄스 간격(초) — 다다다 노트에서 너무 빠른 깜빡임 방지
    [Range(1f, 10f)] public float impulseDecay       = 3.2f;

    public Color RestColor        { get; private set; }
    public Color BeatColor        { get; private set; }
    public float ImpulseLevel     { get; private set; }
    public bool  NewBeatThisFrame { get; private set; }

    int   lastBeat       = -1;
    int   beatCount      = 0;
    bool  isRed          = true;
    float lastFlashTime  = -999f;

    void Awake()
    {
        Instance = this;
        Refresh();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        NewBeatThisFrame = false;

        var c = Conductor.Instance;
        if (c != null && c.IsPlaying)
        {
            int beat = Mathf.FloorToInt(c.SongBeat);
            if (beat >= 0 && beat != lastBeat)
            {
                lastBeat = beat;
                beatCount++;

                // 색 전환 (switchEveryBeats 마다 — 시간 기준 무관)
                if (switchEveryBeats > 0 && beatCount % switchEveryBeats == 0)
                {
                    isRed = !isRed;
                    Refresh();
                }

                // 펄스 플래시: 최소 간격 지켜야 트리거
                if (Time.time - lastFlashTime >= minFlashInterval)
                {
                    lastFlashTime    = Time.time;
                    NewBeatThisFrame = true;
                    ImpulseLevel     = 1f;
                }
            }
        }

        ImpulseLevel *= Mathf.Exp(-impulseDecay * Time.deltaTime);
    }

    void Refresh()
    {
        RestColor = isRed ? redRest : blueRest;
        BeatColor = isRed ? redBeat : blueBeat;
    }

    // 현재 임펄스 반영 최종 색
    public Color GetCurrentColor() => Color.Lerp(RestColor, BeatColor, ImpulseLevel);
}
