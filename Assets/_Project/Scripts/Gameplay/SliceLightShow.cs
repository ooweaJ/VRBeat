using System.Collections.Generic;
using UnityEngine;

// 슬라이스 판정 기반 라이트쇼.
// - 등록된 라이트들은 평소엔 OFF (발광 없음).
// - 슬라이스 성공 시 그 색으로 짧게 켰다가 페이드아웃 → OFF.
// - 위 라이트: 순차 인덱스 회전.
// - 뒤 라이트: 랜덤 N개.
// - 링: 노트 색에 맞는 그룹만 점등.
// - 미스(슬라이스 미발생) 시 아무것도 안 켜짐.
public class SliceLightShow : MonoBehaviour
{
    public static SliceLightShow Instance { get; private set; }

    [Header("Upper Lasers (sequential, 순차로 하나씩)")]
    public Renderer[] upperBeams;

    [Header("Rear Lasers (random, 랜덤 N개)")]
    public Renderer[] rearBeams;
    [Range(1, 8)] public int rearLightUpCount = 2;

    [Header("Rings — 색에 맞는 그룹만")]
    public Renderer[] redRings;
    public Renderer[] blueRings;

    [Header("Timing")]
    [Range(0.2f, 1.5f)] public float flashHold   = 0.55f;
    [Range(0.05f, 1f)]  public float flashFade   = 0.30f;

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] public Color redColor  = new Color(8f,   0.40f, 0.55f);
    [ColorUsage(true, true)] public Color blueColor = new Color(0.50f, 1.30f, 8f);

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    readonly HashSet<Renderer> controlled = new HashSet<Renderer>();
    readonly Dictionary<Renderer, FlashState> active = new Dictionary<Renderer, FlashState>();
    readonly List<Renderer> doneScratch = new List<Renderer>();
    int upperIdx = 0;

    struct FlashState { public Color color; public float remaining; }

    void Awake()
    {
        Instance = this;
        mpb = new MaterialPropertyBlock();
        RegisterAll();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void RegisterAll()
    {
        controlled.Clear();
        AddRange(controlled, upperBeams);
        AddRange(controlled, rearBeams);
        AddRange(controlled, redRings);
        AddRange(controlled, blueRings);
    }

    void Start()
    {
        // 평소엔 모두 OFF
        foreach (var r in controlled) SetRendererColor(r, Color.black);
    }

    void OnEnable()  { EnvColorManager.OnSliceEvent += HandleSlice; }
    void OnDisable() { EnvColorManager.OnSliceEvent -= HandleSlice; }

    public bool IsControlled(Renderer r) => r != null && controlled.Contains(r);

    void HandleSlice(SaberColor saberColor)
    {
        Color flashCol = saberColor == SaberColor.Red ? redColor : blueColor;

        // 위 라이트: 순차로 하나씩
        if (upperBeams != null && upperBeams.Length > 0)
        {
            var beam = upperBeams[upperIdx % upperBeams.Length];
            upperIdx = (upperIdx + 1) % upperBeams.Length;
            TriggerFlash(beam, flashCol);
        }

        // 뒤 라이트: 랜덤으로 N개 중복 없이
        if (rearBeams != null && rearBeams.Length > 0)
        {
            int pickCount = Mathf.Min(rearLightUpCount, rearBeams.Length);
            // 가벼운 Fisher-Yates 일부 추출
            int[] pool = new int[rearBeams.Length];
            for (int i = 0; i < pool.Length; i++) pool[i] = i;
            for (int n = 0; n < pickCount; n++)
            {
                int swapIdx = Random.Range(n, pool.Length);
                (pool[n], pool[swapIdx]) = (pool[swapIdx], pool[n]);
                TriggerFlash(rearBeams[pool[n]], flashCol);
            }
        }

        // 링: 색 매칭
        var rings = saberColor == SaberColor.Red ? redRings : blueRings;
        if (rings != null)
            for (int i = 0; i < rings.Length; i++) TriggerFlash(rings[i], flashCol);
    }

    void TriggerFlash(Renderer r, Color c)
    {
        if (r == null) return;
        active[r] = new FlashState { color = c, remaining = flashHold + flashFade };
    }

    void Update()
    {
        if (active.Count == 0) return;
        doneScratch.Clear();
        var keys = new List<Renderer>(active.Keys);
        foreach (var r in keys)
        {
            var st = active[r];
            st.remaining -= Time.deltaTime;
            if (st.remaining <= 0f)
            {
                SetRendererColor(r, Color.black);
                doneScratch.Add(r);
            }
            else
            {
                // hold 구간은 풀 강도, fade 구간은 선형 감쇠
                float fadeT = st.remaining < flashFade ? st.remaining / flashFade : 1f;
                SetRendererColor(r, st.color * fadeT);
                active[r] = st;
            }
        }
        for (int i = 0; i < doneScratch.Count; i++) active.Remove(doneScratch[i]);
    }

    void SetRendererColor(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(mpb);
        mpb.SetColor(ColorProp, c);
        r.SetPropertyBlock(mpb);
    }

    static void AddRange(HashSet<Renderer> set, Renderer[] arr)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++) if (arr[i] != null) set.Add(arr[i]);
    }
}
