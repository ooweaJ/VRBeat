using UnityEngine;

// 좌/우 벽의 막대들을 오디오 스펙트럼에 맞춰 실시간으로 늘림 (음향 반응 그래프).
// 진폭 낮음=파랑 → 높음=빨강 그라데이션 (HDR Emissive + Bloom).
// 막대 피벗은 바닥(부모 로컬 y=0)이라고 가정하고 위로 자라게 보정.
public class OscilloscopeWall : MonoBehaviour
{
    [Header("Bars (낮은 z → 높은 z 순서로 할당)")]
    public Transform[] bars;

    [Header("Spectrum")]
    public int   sampleCount = 512;   // 2의 거듭제곱
    public float heightScale = 6f;    // 진폭 → 높이(m)
    public float minHeight   = 0.05f;
    public float gain        = 140f;  // 스펙트럼 증폭
    [Range(1f, 30f)] public float smooth = 12f; // 높이 보간 속도

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color lowColor  = new Color(0.2f, 0.45f, 3.2f); // 파랑
    [ColorUsage(true, true)] public Color highColor = new Color(3.4f, 0.25f, 0.25f); // 빨강

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    float[]  spectrum;
    float[]  heights;
    Renderer[] rends;
    Vector3[]  baseScale;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        spectrum = new float[Mathf.Max(64, sampleCount)];
        mpb = new MaterialPropertyBlock();

        if (bars == null) return;
        int n = bars.Length;
        heights   = new float[n];
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
        for (int i = 0; i < n; i++)
        {
            var t = bars[i];
            if (t == null) continue;

            // 로그성 매핑: 저음이 한쪽에 몰리지 않게 + 고역 보정 게인
            float f   = (float)i / n;
            int   bin = Mathf.Clamp((int)(f * f * spectrum.Length), 0, spectrum.Length - 1);
            float amp = Mathf.Clamp01(spectrum[bin] * gain * (1f + bin * 0.05f));

            float target = minHeight + amp * heightScale;
            heights[i] = Mathf.Lerp(heights[i], target, Time.deltaTime * smooth);

            // 바닥에서 위로 자라게: y 스케일 + y 위치(절반) 보정
            float h = heights[i];
            t.localScale    = new Vector3(baseScale[i].x, h, baseScale[i].z);
            var p = t.localPosition;
            t.localPosition = new Vector3(p.x, h * 0.5f, p.z);

            if (rends[i] != null)
            {
                rends[i].GetPropertyBlock(mpb);
                mpb.SetColor(ColorProp, Color.Lerp(lowColor, highColor, amp));
                rends[i].SetPropertyBlock(mpb);
            }
        }
    }
}
