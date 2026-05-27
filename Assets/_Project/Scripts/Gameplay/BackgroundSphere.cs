using UnityEngine;

// VideoSkybox _Tint 색상을 제어해 배경 분위기를 바꿈
// 씬의 BackgroundSphere 메시는 비활성화, 이 스크립트만 사용
public class BackgroundSphere : MonoBehaviour
{
    [Header("Tint Colors")]
    [SerializeField] Color neutralTint  = new Color(1f, 1f, 1f, 0.502f);
    [SerializeField] Color redTint      = new Color(1f, 0.25f, 0.25f, 0.502f);
    [SerializeField] Color blueTint     = new Color(0.25f, 0.25f, 1f, 0.502f);

    [Header("Spawn Flash")]
    [SerializeField] float spawnDecay   = 3.5f;

    [Header("Hit Flash")]
    [SerializeField] float hitDecay     = 5.5f;

    static readonly int TintProp = Shader.PropertyToID("_Tint");
    static BackgroundSphere instance;

    Material skyboxMat;
    Color    currentTint;
    float    currentDecay;

    void Awake()
    {
        instance = this;

        // 구체 메시가 있으면 숨김
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // 인스턴스 생성 — 원본 에셋 머티리얼을 직접 수정하지 않기 위해
        if (RenderSettings.skybox != null)
        {
            skyboxMat = new Material(RenderSettings.skybox);
            RenderSettings.skybox = skyboxMat;
        }
        currentTint  = neutralTint;
        currentDecay = spawnDecay;

        if (skyboxMat != null)
            skyboxMat.SetColor(TintProp, neutralTint);
    }

    void Update()
    {
        if (skyboxMat == null) return;
        currentTint = Color.Lerp(currentTint, neutralTint, Time.deltaTime * currentDecay);
        skyboxMat.SetColor(TintProp, currentTint);
    }

    // 노트 스폰 시 — 은은한 틴트 + 발광 오브
    public static void NoteSpawn(string noteColor)
    {
        if (instance != null)
        {
            instance.currentTint  = noteColor == "blue" ? instance.blueTint : instance.redTint;
            instance.currentDecay = instance.spawnDecay;
        }
        NoteFlashLight.Spawn(noteColor);
    }

    // 노트 히트 시 — 강한 틴트
    public static void NoteHit(string noteColor)
    {
        if (instance == null) return;
        Color target = noteColor == "blue" ? instance.blueTint : instance.redTint;
        instance.currentTint  = Color.Lerp(instance.currentTint, target, 0.8f);
        instance.currentDecay = instance.hitDecay;
    }
}
