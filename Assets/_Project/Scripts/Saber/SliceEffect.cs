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

        BackgroundSphere.NoteHit(noteColor);

        // 프리팹 기반 파티클
        GameObject prefab = noteColor == "blue" ? instance.blueParticlePrefab : instance.redParticlePrefab;
        if (prefab != null)
        {
            GameObject fx = Instantiate(prefab, position, Quaternion.LookRotation(direction));
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

        // 보조 스파클 파티클 (항상 생성)
        Color sparkColor = noteColor == "blue"
            ? new Color(0.3f, 0.6f, 3f)
            : new Color(3f, 0.3f, 0.3f);
        SpawnSparkles(position, sparkColor);
    }

    static void SpawnSparkles(Vector3 pos, Color color)
    {
        var go = new GameObject("NoteHitSparkle");
        go.transform.position = pos;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.4f;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var burst = new ParticleSystem.Burst
        {
            time = 0f,
            count = new ParticleSystem.MinMaxCurve(55f),
            cycleCount = 1,
            repeatInterval = 0.01f,
            probability = 1f
        };
        emission.SetBursts(new[] { burst });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        // 수명에 따라 크기 줄어듦
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        ApplyAdditiveMaterial(renderer, color);

        ps.Play();
        Object.Destroy(go, 1.5f);
    }

    static void ApplyAdditiveMaterial(ParticleSystemRenderer renderer, Color color)
    {
        // 런타임에 가산 블렌딩 파티클 머티리얼 생성
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
        if (shader == null) return;

        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f);
        mat.SetFloat("_SrcBlend", 5f);
        mat.SetFloat("_DstBlend", 10f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetColor("_BaseColor", color * 1.5f);
        mat.renderQueue = 3000;

        renderer.material = mat;
    }
}
