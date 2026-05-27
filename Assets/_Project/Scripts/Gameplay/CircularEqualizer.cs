using UnityEngine;

// 원형 이퀄라이저 — 막대들이 원형으로 배치되어 음향 스펙트럼에 맞춰 방사형(바깥)으로 뻗는다.
// 각 막대는 회전된 pivot의 자식이라 로컬 +Y가 곧 방사 방향. 길이=스펙트럼 진폭.
// 좌우 대칭 매핑이라 원형으로 보기 좋게 펄스. 진폭 낮음=파랑 → 높음=빨강.
public class CircularEqualizer : MonoBehaviour
{
    [Header("Bars (각도 순서로 할당, 로컬 +Y가 방사 방향)")]
    public Transform[] bars;

    [Header("Geometry")]
    public float radius = 1.9f;     // 안쪽 반지름(이 안은 비어 시야 확보)

    [Header("Spectrum")]
    public int   sampleCount = 512;
    public float lengthScale = 2.6f;
    public float minLength   = 0.12f;
    public float gain        = 150f;
    [Range(1f, 30f)] public float smooth = 16f;

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color lowColor  = new Color(0.25f, 0.65f, 3.5f); // 파랑(세이버)
    [ColorUsage(true, true)] public Color highColor = new Color(3.0f, 0.25f, 0.25f); // 빨강(세이버)

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    float[]  spectrum;
    float[]  lengths;
    Renderer[] rends;
    Vector3[]  baseScale;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        spectrum = new float[Mathf.Max(64, sampleCount)];
        mpb = new MaterialPropertyBlock();
        if (bars == null) return;

        int n = bars.Length;
        lengths   = new float[n];
        rends     = new Renderer[n];
        baseScale = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            if (bars[i] == null) continue;
            rends[i]     = bars[i].GetComponent<Renderer>();
            baseScale[i] = bars[i].localScale;
        }
    }

    void Update()
    {
        if (bars == null || bars.Length == 0) return;
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        int n = bars.Length;
        int half = n / 2;
        for (int i = 0; i < n; i++)
        {
            var t = bars[i];
            if (t == null) continue;

            // 좌우 대칭: 위(0)→아래(half) 로 갈수록 고역 → 원형으로 대칭
            int sym = i <= half ? i : n - i;
            float f   = (float)sym / Mathf.Max(1, half);
            int   bin = Mathf.Clamp((int)(f * f * spectrum.Length), 0, spectrum.Length - 1);
            float amp = Mathf.Clamp01(spectrum[bin] * gain * (1f + bin * 0.05f));

            float target = minLength + amp * lengthScale;
            lengths[i] = Mathf.Lerp(lengths[i], target, Time.deltaTime * smooth);

            float len = lengths[i];
            t.localScale    = new Vector3(baseScale[i].x, len, baseScale[i].z);
            t.localPosition = new Vector3(0f, radius + len * 0.5f, 0f); // 안쪽 반지름에서 바깥으로

            if (rends[i] != null)
            {
                rends[i].GetPropertyBlock(mpb);
                mpb.SetColor(ColorProp, Color.Lerp(lowColor, highColor, amp));
                rends[i].SetPropertyBlock(mpb);
            }
        }
    }
}
