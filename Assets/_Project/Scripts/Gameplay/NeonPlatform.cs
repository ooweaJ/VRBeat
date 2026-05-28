using UnityEngine;

// 플레이어 발판 + 트랙 레일 네온 에지 애니메이션.
// Conductor 박자마다 흰-파랑 베이스 → 마젠타 펄스 → 복귀.
public class NeonPlatform : MonoBehaviour
{
    [Header("Renderers")]
    public Renderer[] stageEdges;   // 플레이어 발판 테두리 4변
    public Renderer[] trackRails;   // 노트 레인 사이드 레일 2개
    public Renderer[] crossBars;    // 트랙 크로스바 (원근감)
    public Renderer[] sideStepEdges;// 사이드 스텝 블록 상면 에지

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color baseColor = new Color(1.2f, 1.5f, 2.8f); // 흰-파랑 베이스
    [ColorUsage(true, true)] public Color beatColor = new Color(5.5f, 0.3f, 5f);   // 마젠타 박자 강조

    [Range(2f, 15f)] public float decay = 7f;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    int   lastBeat = -1;
    float level    = 0f;

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

        Color col = Color.Lerp(baseColor, beatColor, level);
        Apply(stageEdges,    col);
        Apply(trackRails,    col);
        Apply(crossBars,     col);
        Apply(sideStepEdges, col);
    }

    void Apply(Renderer[] arr, Color col)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) continue;
            arr[i].GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, col);
            arr[i].SetPropertyBlock(mpb);
        }
    }
}
