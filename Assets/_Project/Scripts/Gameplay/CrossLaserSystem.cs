using UnityEngine;

// V자/X자 크로스 레이저 시스템.
// EnvColorManager 존재 시 전역 색 동기화, 없으면 자체 fallback 색 사용.
public class CrossLaserSystem : MonoBehaviour
{
    [System.Serializable]
    public class LaserBeam
    {
        public Transform pivot;
        public Renderer  renderer;
        [HideInInspector] public float phaseOffset;
    }

    [Header("Laser Groups")]
    public LaserBeam[] leftBeams;
    public LaserBeam[] rightBeams;

    [Header("Mode")]
    public bool independentMode = false; // true: 좌우 동일 방향 스윕(독립), false: 좌우 반전(크로스)

    [Header("Angle")]
    [Range(10f, 80f)]  public float sweepAngle  = 60f;
    [Range(0.05f, 1f)] public float oscSpeed    = 0.35f;

    [Header("Color (HDR) — fallback: EnvColorManager 없을 때")]
    [ColorUsage(true, true)] public Color restColor = new Color(2.5f, 0.08f, 0.08f);
    [ColorUsage(true, true)] public Color beatColor = new Color(8f,   0.3f,  0.3f);
    [Range(1f, 10f)] public float impulseDecay = 3.2f;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    int   lastBeat     = -1;
    float impulseLevel = 0f;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        AssignPhase(leftBeams);
        AssignPhase(rightBeams);
    }

    void AssignPhase(LaserBeam[] g)
    {
        if (g == null) return;
        float step = independentMode ? 1.8f : 0.65f; // 독립 모드는 더 넓게 퍼뜨려 확연한 차이
        for (int i = 0; i < g.Length; i++)
            if (g[i] != null) g[i].phaseOffset = i * step;
    }

    void Update()
    {
        var ec = EnvColorManager.Instance;

        if (ec != null)
        {
            impulseLevel = ec.ImpulseLevel;
        }
        else
        {
            var c = Conductor.Instance;
            if (c != null && c.IsPlaying)
            {
                int beat = Mathf.FloorToInt(c.SongBeat);
                if (beat >= 0 && beat != lastBeat) { lastBeat = beat; impulseLevel = 1f; }
            }
            impulseLevel *= Mathf.Exp(-impulseDecay * Time.deltaTime);
        }

        float t   = Time.time;
        Color col = ec != null ? ec.GetCurrentColor() : Color.Lerp(restColor, beatColor, impulseLevel);
        SetColor(leftBeams,  col);
        SetColor(rightBeams, col);
        RotateGroup(leftBeams,  +1f, t);
        RotateGroup(rightBeams, independentMode ? +1f : -1f, t);
    }

    public void TriggerCross() => impulseLevel = 1f;

    void RotateGroup(LaserBeam[] g, float sign, float t)
    {
        if (g == null) return;
        for (int i = 0; i < g.Length; i++)
        {
            if (g[i]?.pivot == null) continue;
            float phase = t * oscSpeed * Mathf.PI * 2f + g[i].phaseOffset;
            float angle = Mathf.Sin(phase) * sweepAngle;
            g[i].pivot.localEulerAngles = new Vector3(0f, 0f, sign * angle);
        }
    }

    void SetColor(LaserBeam[] g, Color col)
    {
        if (g == null) return;
        for (int i = 0; i < g.Length; i++)
        {
            if (g[i]?.renderer == null) continue;
            g[i].renderer.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, col);
            g[i].renderer.SetPropertyBlock(mpb);
        }
    }
}
