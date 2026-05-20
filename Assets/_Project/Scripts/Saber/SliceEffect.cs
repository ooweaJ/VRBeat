using UnityEngine;

public class SliceEffect : MonoBehaviour
{
    static SliceEffect instance;

    [SerializeField] GameObject redParticlePrefab;
    [SerializeField] GameObject blueParticlePrefab;

    void Awake() => instance = this;

    public static void Play(Vector3 position, Vector3 direction, string noteColor)
    {
        if (instance == null) return;

        GameObject prefab = noteColor == "blue" ? instance.blueParticlePrefab : instance.redParticlePrefab;
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, position, Quaternion.LookRotation(direction));

        // CFXR 파티클이 Stop Action으로 자동 제거되지 않는 경우 폴백
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(fx, duration + 0.5f);
        }
        else
        {
            Destroy(fx, 3f);
        }
    }
}
