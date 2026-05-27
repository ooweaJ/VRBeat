using UnityEngine;

// 측면 레이저 빔 — 비트세이버 BBS/레이저 흉내.
// Conductor 박자에 맞춰 strobe(번쩍) → fade(감쇠). 짝/홀 비트로 두 그룹을 교대 점등해
// 좌우로 흐르는 움직임을 만든다. 색은 측면 고정(왼쪽 빨강 / 오른쪽 파랑).
public class SideLasers : MonoBehaviour
{
    [Header("두 그룹 (짝/홀 비트 교대)")]
    public Renderer[] groupA;
    public Renderer[] groupB;

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color litColor = new Color(4f, 0.25f, 0.25f);
    [ColorUsage(true, true)] public Color dimColor = new Color(0.12f, 0.02f, 0.02f);

    [Header("Strobe")]
    [Range(1f, 16f)] public float decay = 7f;   // 감쇠 속도

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    int   lastBeat = -1;
    float levelA, levelB;

    void Awake() => mpb = new MaterialPropertyBlock();

    void Update()
    {
        var c = Conductor.Instance;
        if (c != null && c.IsPlaying)
        {
            int beat = Mathf.FloorToInt(c.SongBeat);
            if (beat >= 0 && beat != lastBeat)
            {
                lastBeat = beat;
                if ((beat & 1) == 0) levelA = 1f; else levelB = 1f;
            }
        }

        levelA = Mathf.Lerp(levelA, 0f, Time.deltaTime * decay);
        levelB = Mathf.Lerp(levelB, 0f, Time.deltaTime * decay);

        Apply(groupA, levelA);
        Apply(groupB, levelB);
    }

    void Apply(Renderer[] group, float level)
    {
        if (group == null) return;
        Color col = Color.Lerp(dimColor, litColor, level);
        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == null) continue;
            group[i].GetPropertyBlock(mpb);
            mpb.SetColor(ColorProp, col);
            group[i].SetPropertyBlock(mpb);
        }
    }
}
