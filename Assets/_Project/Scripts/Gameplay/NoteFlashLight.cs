using UnityEngine;

public class NoteFlashLight : MonoBehaviour
{
    Material mat;
    float    timer;
    float    duration;

    public static void Spawn(string noteColor)
    {
        var cfg    = LightPillarConfig.Instance;
        bool isBlue = noteColor == "blue";

        // 설정값 — cfg 없으면 기본값
        float spawnZ   = cfg != null ? cfg.spawnZ      : 30f;
        float y        = cfg != null ? cfg.pillarY     : 8f;
        float offsetX  = cfg != null ? cfg.laneOffsetX : 0.9f;
        float radius   = cfg != null ? cfg.radiusScale : 0.6f;
        float height   = cfg != null ? cfg.heightScale : 8f;
        float radPow   = cfg != null ? cfg.radialPower     : 0.8f;
        float hFadePow = cfg != null ? cfg.heightFadePower : 1.5f;
        float dur      = cfg != null ? cfg.duration    : 0.7f;
        Color col      = cfg != null
            ? (isBlue ? cfg.blueColor : cfg.redColor)
            : (isBlue ? new Color(0.1f, 0.2f, 1.8f) : new Color(1.8f, 0.1f, 0.1f));

        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "LightPillar";
        go.transform.position   = new Vector3(isBlue ? offsetX : -offsetX, y, spawnZ);
        go.transform.localScale = new Vector3(radius, height, radius);
        Object.Destroy(go.GetComponent<CapsuleCollider>());

        var shader = Shader.Find("VRBeat/LightPillar");
        if (shader == null) { Object.Destroy(go); return; }

        var mr  = go.GetComponent<MeshRenderer>();
        var mat = new Material(shader);
        mat.SetColor("_Color",           col);
        mat.SetFloat("_Intensity",       1f);
        mat.SetFloat("_RadialPower",     radPow);
        mat.SetFloat("_HeightFadePower", hFadePow);
        mr.material = mat;

        var p      = go.AddComponent<NoteFlashLight>();
        p.mat      = mat;
        p.duration = dur;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t        = Mathf.Clamp01(timer / duration);
        float envelope = t < 0.06f
            ? t / 0.06f
            : Mathf.Pow(1f - (t - 0.06f) / 0.94f, 1.8f);
        mat.SetFloat("_Intensity", envelope);

        if (timer >= duration)
        {
            Destroy(mat);
            Destroy(gameObject);
        }
    }
}
