using UnityEngine;

// 크기 조절 가능한 네온 테두리 발판 블록.
// Body(검은 큐브)의 localScale 을 바꾸면 상면 테두리 4변이 자동으로 모서리에 맞춰짐.
// [ExecuteAlways] : Edit 모드와 Play 모드 양쪽에서 실시간 반영.
[ExecuteAlways]
public class NeonBlock : MonoBehaviour
{
    [Header("Parts (Prefab 에서 자동 연결)")]
    public Transform body;
    public Transform edgeFront;
    public Transform edgeBack;
    public Transform edgeLeft;
    public Transform edgeRight;

    [Header("Edge")]
    [Range(0.01f, 0.2f)] public float thickness  = 0.05f;
    [Range(-0.1f, 0.1f)] public float topOffset  = 0.003f; // 상면 위 살짝 띄우기

    [Header("Color (HDR) — Play 모드에서 박자 펄스")]
    [ColorUsage(true, true)] public Color baseColor = new Color(4f, 0.15f, 0.5f);
    [ColorUsage(true, true)] public Color beatColor = new Color(8f, 0.2f, 0.3f);
    [Range(2f, 15f)] public float decay = 7f;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    int   lastBeat = -1;
    float level    = 0f;

    void Awake()      => mpb = new MaterialPropertyBlock();
    void OnValidate() => FitEdges();

    void Update()
    {
        FitEdges();

        if (!Application.isPlaying) return;

        var c = Conductor.Instance;
        if (c != null && c.IsPlaying)
        {
            int beat = Mathf.FloorToInt(c.SongBeat);
            if (beat >= 0 && beat != lastBeat) { lastBeat = beat; level = 1f; }
        }
        level = Mathf.Lerp(level, 0f, Time.deltaTime * decay);
        PulseEdges(Color.Lerp(baseColor, beatColor, level));
    }

    // Body 스케일/위치 → 4개 에지 자동 배치
    void FitEdges()
    {
        if (body == null) return;

        Vector3 bs = body.localScale;
        Vector3 bp = body.localPosition;
        float   et = thickness;
        float topY = bp.y + bs.y * 0.5f + topOffset;
        float hW   = bs.x * 0.5f;
        float hD   = bs.z * 0.5f;

        Fit(edgeFront, new Vector3(bp.x,      topY, bp.z - hD), new Vector3(bs.x, et, et));
        Fit(edgeBack,  new Vector3(bp.x,      topY, bp.z + hD), new Vector3(bs.x, et, et));
        Fit(edgeLeft,  new Vector3(bp.x - hW, topY, bp.z),      new Vector3(et,   et, bs.z));
        Fit(edgeRight, new Vector3(bp.x + hW, topY, bp.z),      new Vector3(et,   et, bs.z));
    }

    static void Fit(Transform t, Vector3 pos, Vector3 scale)
    {
        if (t == null) return;
        t.localPosition = pos;
        t.localScale    = scale;
    }

    void PulseEdges(Color col)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        Renderer[] rds = {
            edgeFront?.GetComponent<Renderer>(),
            edgeBack? .GetComponent<Renderer>(),
            edgeLeft? .GetComponent<Renderer>(),
            edgeRight?.GetComponent<Renderer>()
        };
        foreach (var r in rds)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, col);
            r.SetPropertyBlock(mpb);
        }
    }
}
