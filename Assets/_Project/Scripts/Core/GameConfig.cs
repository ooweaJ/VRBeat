using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "VRBeat/GameConfig")]
public class GameConfig : ScriptableObject
{
    public float spawnDistance = 30f;
    public float hitDistance = 0f;
    public float despawnDistance = -2f;
    public float laneWidth = 0.6f;
    public float rowHeight = 0.6f;
    public float baseHeight = 0.8f;
    public int poolWarmupCount = 64;
}
