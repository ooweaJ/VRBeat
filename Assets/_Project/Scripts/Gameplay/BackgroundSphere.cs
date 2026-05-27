using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BackgroundSphere : MonoBehaviour
{
    [Header("Note Hit Flash")]
    [SerializeField] Color redNoteColor       = new Color(1f, 0.04f, 0.07f);
    [SerializeField] Color blueNoteColor      = new Color(0.04f, 0.12f, 1f);
    [SerializeField] float noteFlashIntensity = 5f;
    [SerializeField] float noteFlashDecay     = 3f;

    [Header("Ambient Light")]
    [SerializeField] bool  controlAmbient    = true;
    [SerializeField] Color neutralAmbient    = new Color(0.03f, 0.03f, 0.05f);
    [SerializeField] float ambientMultiplier = 0.2f;

    static readonly int PropEmission  = Shader.PropertyToID("_EmissionColor");
    static readonly int PropIntensity = Shader.PropertyToID("_Intensity");

    static BackgroundSphere instance;

    Material mat;
    Color    noteFlashColor;
    float    noteFlashValue;

    void Awake()
    {
        instance       = this;
        mat            = GetComponent<MeshRenderer>().material;
        noteFlashColor = Color.black;
    }

    void Update()
    {
        noteFlashValue = Mathf.MoveTowards(noteFlashValue, 0f, Time.deltaTime * noteFlashDecay);

        float intensity = noteFlashValue * noteFlashIntensity;
        mat.SetColor(PropEmission,  noteFlashColor);
        mat.SetFloat(PropIntensity, intensity);

        if (controlAmbient)
            RenderSettings.ambientLight = neutralAmbient + noteFlashColor * (intensity * ambientMultiplier);
    }

    public static void NoteHit(string noteColor)
    {
        if (instance == null) return;
        instance.noteFlashColor = noteColor == "blue" ? instance.blueNoteColor : instance.redNoteColor;
        instance.noteFlashValue = 1f;
    }
}
