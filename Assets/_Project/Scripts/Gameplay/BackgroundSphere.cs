using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BackgroundSphere : MonoBehaviour
{
    [Header("Club Colors")]
    [SerializeField] Color colorLeft  = new Color(1.0f, 0.05f, 0.15f);
    [SerializeField] Color colorRight = new Color(0.05f, 0.25f, 1.0f);

    [Header("Beat Response")]
    [SerializeField] float pulseIntensity = 2.5f;
    [SerializeField] float pulseDecay     = 5f;

    static readonly int PropEmission  = Shader.PropertyToID("_EmissionColor");
    static readonly int PropIntensity = Shader.PropertyToID("_Intensity");

    Material mat;
    float    lastBeat;
    float    pulseValue;
    int      beatCount;

    void Awake() => mat = GetComponent<MeshRenderer>().material;

    void Update()
    {
        var conductor = Conductor.Instance;
        if (conductor != null && conductor.IsPlaying)
        {
            float beat = conductor.SongBeat;
            if (Mathf.FloorToInt(beat) > Mathf.FloorToInt(lastBeat))
            {
                pulseValue = 1f;
                beatCount++;
            }
            lastBeat = beat;
        }

        pulseValue = Mathf.MoveTowards(pulseValue, 0f, Time.deltaTime * pulseDecay);

        Color clubColor = (beatCount % 2 == 0) ? colorLeft : colorRight;
        mat.SetColor(PropEmission,  clubColor);
        mat.SetFloat(PropIntensity, pulseValue * pulseIntensity);
    }
}
