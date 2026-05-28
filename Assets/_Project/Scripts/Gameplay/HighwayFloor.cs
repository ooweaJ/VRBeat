using UnityEngine;

// 바닥 레인 구분선의 발광색을 Conductor 박자에 맞춰 펄스.
// 평소 흰색 → 정수 비트마다 빨강↔파랑 교대로 번쩍 → 흰색으로 서서히 복귀.
public class HighwayFloor : MonoBehaviour
{
    [Header("Edge / Lane Line Renderers")]
    public Renderer[] lineRenderers;

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] public Color baseColor = new Color(1.6f, 1.6f, 1.6f); // 흰색
    [ColorUsage(true, true)] public Color redColor  = new Color(4f, 0.25f, 0.25f); // 왼손
    [ColorUsage(true, true)] public Color blueColor = new Color(0.25f, 0.55f, 4f); // 오른손

    [Header("Pulse")]
    [Range(0.5f, 12f)] public float decay = 4f;          // 흰색 복귀 속도
    [Range(0f, 1f)]    public float pulseStrength = 1f;  // 펄스 세기

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    Color current;
    int   lastBeat = -1;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        current = baseColor;
        Apply();
    }

    void Update()
    {
        var c = Conductor.Instance;
        if (c != null && c.IsPlaying)
        {
            int beat = Mathf.FloorToInt(c.SongBeat);
            if (beat >= 0 && beat != lastBeat)
            {
                lastBeat = beat;
                Color pulse = (beat % 2 == 0) ? redColor : blueColor; // 짝/홀 비트 교대
                current = Color.Lerp(baseColor, pulse, pulseStrength);
            }
        }

        current = Color.Lerp(current, baseColor, Time.deltaTime * decay);

        // 슬라이스 색반응 — 노트 베면 그 색으로 잠깐 덮어쓰기
        var ec = EnvColorManager.Instance;
        Color applied = current;
        if (ec != null && ec.SliceLevel > 0.01f)
            applied = Color.Lerp(applied, ec.SliceTint, ec.SliceLevel);

        Apply(applied);
    }

    void Apply() => Apply(current);

    void Apply(Color col)
    {
        if (lineRenderers == null) return;
        for (int i = 0; i < lineRenderers.Length; i++)
        {
            var r = lineRenderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorProp, col);
            r.SetPropertyBlock(mpb);
        }
    }
}
