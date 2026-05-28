using UnityEngine;

// 양옆 세로 라이트 기둥 — 비트세이버 측면 스트립 조명처럼 줄지어 서서 은은히 빛남.
// 스윕(스포트라이트) 아님. 박자마다 밝아졌다 사라지는 펄스가 z를 따라 물결치며 흐른다.
public class SideLasers : MonoBehaviour
{
    [Header("Columns (z 순서로 할당)")]
    public Renderer[] columns;

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color baseColor = new Color(0.9f, 0.08f, 0.6f);  // 항상 켜진 글로우
    [ColorUsage(true, true)] public Color beatColor = new Color(4.5f, 0.5f, 3.5f);   // 박자 강조

    [Header("Pulse")]
    [Range(1f, 16f)] public float decay = 5f;     // 펄스 감쇠
    public float waveSpeed = 5f;                  // 펄스가 z를 따라 흐르는 속도
    public float wavePerColumn = 0.5f;            // 기둥 간 위상차

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    int   lastBeat = -1;
    float level;

    void Awake() => mpb = new MaterialPropertyBlock();

    void Update()
    {
        var c = Conductor.Instance;
        if (c != null && c.IsPlaying)
        {
            int beat = Mathf.FloorToInt(c.SongBeat);
            if (beat >= 0 && beat != lastBeat) { lastBeat = beat; level = 1f; }
        }
        level = Mathf.Lerp(level, 0f, Time.deltaTime * decay);

        if (columns == null) return;
        float t = Time.time;
        var ec = EnvColorManager.Instance;
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] == null) continue;
            // 박자 펄스에 z방향 물결을 섞어 생동감
            float wave  = 0.5f + 0.5f * Mathf.Sin(t * waveSpeed - i * wavePerColumn);
            float pulse = level * Mathf.Lerp(0.4f, 1f, wave);
            Color col   = Color.Lerp(baseColor, beatColor, pulse);

            // 슬라이스 색반응
            if (ec != null && ec.SliceLevel > 0.01f)
                col = Color.Lerp(col, ec.SliceTint, ec.SliceLevel);

            columns[i].GetPropertyBlock(mpb);
            mpb.SetColor(ColorProp, col);
            columns[i].SetPropertyBlock(mpb);
        }
    }
}
