using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "VRBeat/GameConfig")]
public class GameConfig : ScriptableObject
{
    public float spawnDistance = 20f;
    public float hitDistance = 20f;
    public float despawnDistance = -2f;
    public float noteApproachDist = 8f; // 이 구간에서만 0.1→0.4 스케일 (작을수록 가까이서 확 커짐)
    public float laneWidth = 0.6f;
    public float rowHeight = 0.6f;
    public float baseHeight = 0.8f;
    public int poolWarmupCount = 64;
}
