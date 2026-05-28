using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "VRBeat/GameConfig")]
public class GameConfig : ScriptableObject
{
    public float spawnDistance = 40f;
    public float noteSyncStartDistance = 20f;
    public float notePreSyncDuration = 0.5f;
    public float hitDistance = 0f;
    public float despawnDistance = -2f;
    public float noteSpawnScale = 0.25f; // prefab scale 0.4 기준 실제 0.1
    public float noteSyncScale = 1f; // prefab scale 0.4 기준 실제 0.4
    public float laneWidth = 0.6f;
    public float rowHeight = 0.6f;
    public float baseHeight = 0.8f;
    public int poolWarmupCount = 64;
}
