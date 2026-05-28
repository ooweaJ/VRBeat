using UnityEngine;

// 회전 링 — 비트세이버 ring 요소 흉내.
// 트랙을 감싸는 사각 링들이 z축으로 천천히 회전(idle)하고, 박자마다 회전 임펄스 + 발광 펄스.
// 링은 인덱스별로 빨강/파랑 고정, 박자에 밝기만 펄스. 멀어질수록 작아져 터널감.
public class RotatingRings : MonoBehaviour
{
    [System.Serializable]
    public class Ring
    {
        public Transform  pivot;       // z축 회전 대상
        public Renderer[] renderers;   // 사각 4변
        [ColorUsage(true, true)] public Color color = new Color(4f, 0.25f, 0.25f);
        [HideInInspector] public float spin;   // 현재 각속도(deg/s)
        [HideInInspector] public float level;  // 발광 레벨
    }

    public Ring[] rings;

    [Header("Spin")]
    public float idleSpin   = 12f;   // 평상시 각속도(deg/s)
    public float beatImpulse = 90f;  // 박자당 추가 각속도
    [Range(0.5f, 6f)] public float spinDamp = 2f; // 임펄스 감쇠

    [Header("Glow")]
    [Range(0f, 0.5f)] public float idleGlow = 0.18f; // 박자 사이 기본 밝기(자기 색 비율)
    [Range(1f, 12f)] public float glowDecay = 5f;

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    int lastBeat = -1;

    void Awake() => mpb = new MaterialPropertyBlock();

    void Update()
    {
        if (rings == null) return;

        var c = Conductor.Instance;
        bool beatHit = false;
        if (c != null && c.IsPlaying)
        {
            int beat = Mathf.FloorToInt(c.SongBeat);
            if (beat >= 0 && beat != lastBeat) { lastBeat = beat; beatHit = true; }
        }

        var ec = EnvColorManager.Instance;
        for (int i = 0; i < rings.Length; i++)
        {
            var r = rings[i];
            if (r == null) continue;

            if (beatHit)
            {
                // 인접 링은 반대 방향으로 임펄스 → 교차 회전
                r.spin  += beatImpulse * ((i & 1) == 0 ? 1f : -1f);
                r.level  = 1f;
            }

            // 회전: idle 기준선 + 임펄스(감쇠)
            float target = idleSpin * ((i & 1) == 0 ? 1f : -1f);
            r.spin = Mathf.Lerp(r.spin, target, Time.deltaTime * spinDamp);
            if (r.pivot != null)
                r.pivot.Rotate(0f, 0f, r.spin * Time.deltaTime, Space.Self);

            // 발광 펄스
            r.level = Mathf.Lerp(r.level, 0f, Time.deltaTime * glowDecay);
            Color col = Color.Lerp(r.color * idleGlow, r.color, r.level);

            // 슬라이스 색반응 — 세이버 색으로 전체 링 잠깐 덮어쓰기
            if (ec != null && ec.SliceLevel > 0.01f)
                col = Color.Lerp(col, ec.SliceTint, ec.SliceLevel);

            if (r.renderers != null)
                for (int k = 0; k < r.renderers.Length; k++)
                {
                    if (r.renderers[k] == null) continue;
                    // SliceLightShow가 제어 중인 링은 자동 색 안 건드림
                    if (SliceLightShow.Instance != null && SliceLightShow.Instance.IsControlled(r.renderers[k])) continue;
                    r.renderers[k].GetPropertyBlock(mpb);
                    mpb.SetColor(ColorProp, col);
                    r.renderers[k].SetPropertyBlock(mpb);
                }
        }
    }
}
