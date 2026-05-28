using UnityEngine;

// 세그먼트 방식 오실로스코프 — 로딩바처럼 ▓▓▓▓▓ 작은 큐브들이 쌓임.
// bars[] 순서: column0_seg0, column0_seg1 … column1_seg0 …
// 오디오 레벨에 따라 아래 세그먼트부터 순서대로 켜짐.
public class OscilloscopeWall : MonoBehaviour
{
    [Header("Segments (col0_seg0, col0_seg1 … col1_seg0 …)")]
    public Transform[] bars;
    public int segmentsPerColumn = 8;

    [Header("Spectrum")]
    public int   sampleCount = 512;
    public float gain        = 140f;
    [Range(1f, 30f)] public float smooth = 12f;

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color lowColor  = new Color(0.2f,  0.45f, 3.2f);
    [ColorUsage(true, true)] public Color highColor = new Color(3.4f,  0.25f, 0.25f);
    [ColorUsage(true, true)] public Color dimColor  = new Color(0.02f, 0.02f, 0.05f);

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    float[]            spectrum;
    float[]            heights;
    Renderer[]         rends;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        spectrum = new float[Mathf.Max(64, sampleCount)];
        mpb      = new MaterialPropertyBlock();
        if (bars == null) return;

        int segsPerCol = Mathf.Max(1, segmentsPerColumn);
        int numCols    = bars.Length / segsPerCol;
        heights = new float[numCols];
        rends   = new Renderer[bars.Length];
        for (int i = 0; i < bars.Length; i++)
            if (bars[i] != null)
                rends[i] = bars[i].GetComponent<Renderer>();
    }

    void Update()
    {
        if (bars == null || bars.Length == 0) return;
        int segsPerCol = Mathf.Max(1, segmentsPerColumn);
        int numCols    = bars.Length / segsPerCol;
        if (numCols == 0) return;

        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        for (int col = 0; col < numCols; col++)
        {
            float f   = (float)col / numCols;
            int   bin = Mathf.Clamp((int)(f * f * spectrum.Length), 0, spectrum.Length - 1);
            float amp = Mathf.Clamp01(spectrum[bin] * gain * (1f + bin * 0.05f));
            heights[col] = Mathf.Lerp(heights[col], amp, Time.deltaTime * smooth);

            int activeSegs = Mathf.RoundToInt(heights[col] * segsPerCol);

            for (int seg = 0; seg < segsPerCol; seg++)
            {
                int idx = col * segsPerCol + seg;
                if (idx >= bars.Length || rends[idx] == null) continue;
                rends[idx].GetPropertyBlock(mpb);
                mpb.SetColor(ColorProp, seg < activeSegs
                    ? Color.Lerp(lowColor, highColor, (float)seg / segsPerCol)
                    : dimColor);
                rends[idx].SetPropertyBlock(mpb);
            }
        }
    }
}
