using UnityEngine;

// 씬에 배치 — 인스펙터에서 기둥 크기/색/밝기 실시간 조정
public class LightPillarConfig : MonoBehaviour
{
    public static LightPillarConfig Instance { get; private set; }

    [Header("Position")]
    public float spawnZ      = 30f;
    public float pillarY     = 8f;
    public float laneOffsetX = 0.9f;  // 스폰 레인 X 오프셋

    [Header("Pillar Size")]
    public float radiusScale = 0.6f;  // 두께
    public float heightScale = 8f;    // 높이 (실제 높이 = heightScale * 2)

    [Header("Color (HDR)")]
    [ColorUsage(true, true)] public Color redColor  = new Color(1.8f, 0.1f, 0.1f);
    [ColorUsage(true, true)] public Color blueColor = new Color(0.1f, 0.2f, 1.8f);

    [Header("Shader")]
    [Range(0.2f, 4f)] public float radialPower     = 0.8f;
    [Range(0.3f, 4f)] public float heightFadePower = 1.5f;

    [Header("Animation")]
    [Range(0.2f, 2f)] public float duration = 0.7f;

    void Awake() => Instance = this;
}
