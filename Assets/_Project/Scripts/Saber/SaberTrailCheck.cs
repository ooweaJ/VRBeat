using System.Collections.Generic;
using UnityEngine;

// 루트~팁 전체를 잇는 리본 메시 트레일. TrailRenderer 불필요.
public class SaberTrailCheck : MonoBehaviour
{
    [SerializeField] SaberController saber;
    [SerializeField] Transform trailTip;
    [SerializeField] Transform trailRoot;
    [SerializeField] float trailThreshold = 1.5f;
    [SerializeField] float trailTime      = 0.15f;
    [SerializeField] Material trailMaterial;

    struct Segment { public Vector3 tip, root; public float time; }
    readonly List<Segment> segments = new();

    GameObject   trailObj;
    MeshFilter   mf;
    MeshRenderer mr;
    Mesh         mesh;

    static readonly Color kRed  = new Color(1f,   0.2f, 0.2f);
    static readonly Color kBlue = new Color(0.2f, 0.55f, 1f);

    void Awake()
    {
        if (saber == null) saber = GetComponentInParent<SaberController>();

        // tip/root 미지정 시 SaberController에서 자동으로 가져옴
        if (trailTip  == null && saber != null) trailTip  = saber.Tip;
        if (trailRoot == null && saber != null) trailRoot = saber.Root;

        // 트레일 메시는 월드 원점 고정 오브젝트에 붙임 (세이버와 같이 움직이면 안 됨)
        trailObj = new GameObject("SaberTrailMesh");
        mf = trailObj.AddComponent<MeshFilter>();
        mr = trailObj.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        mesh = new Mesh { name = "SaberTrail" };
        mf.mesh = mesh;

        if (trailMaterial != null)
        {
            mr.material = trailMaterial;
        }
        else
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Additive")
                    ?? Shader.Find("Sprites/Default");
            if (s != null)
            {
                Color c = saber != null && saber.color == SaberColor.Blue ? kBlue : kRed;
                var mat = new Material(s);
                mat.SetColor("_BaseColor", c);
                mat.SetColor("_Color",     c);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mr.material = mat;
            }
        }
    }

    void LateUpdate()
    {
        float now    = Time.time;
        bool  active = saber != null && saber.Speed >= trailThreshold
                       && trailTip != null && trailRoot != null;

        if (active)
        {
            segments.Add(new Segment
            {
                tip  = trailTip.position,
                root = trailRoot.position,
                time = now
            });
        }

        // 오래된 세그먼트 제거
        while (segments.Count > 0 && now - segments[0].time > trailTime)
            segments.RemoveAt(0);

        BuildMesh(now);
    }

    void BuildMesh(float now)
    {
        int n = segments.Count;
        if (n < 2) { mesh.Clear(); return; }

        var verts  = new Vector3[n * 2];
        var uvs    = new Vector2[n * 2];
        var colors = new Color[n * 2];
        var tris   = new int[(n - 1) * 6];

        Color baseColor = saber != null && saber.color == SaberColor.Blue ? kBlue : kRed;

        for (int i = 0; i < n; i++)
        {
            float age   = now - segments[i].time;
            float alpha = Mathf.Clamp01(1f - age / trailTime);
            float u     = (float)i / (n - 1);
            Color c     = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            verts [i * 2]     = segments[i].root;
            verts [i * 2 + 1] = segments[i].tip;
            uvs   [i * 2]     = new Vector2(u, 0f);
            uvs   [i * 2 + 1] = new Vector2(u, 1f);
            colors[i * 2]     = c;
            colors[i * 2 + 1] = c;
        }

        for (int i = 0; i < n - 1; i++)
        {
            int v = i * 2, t = i * 6;
            tris[t]     = v;
            tris[t + 1] = v + 1;
            tris[t + 2] = v + 2;
            tris[t + 3] = v + 1;
            tris[t + 4] = v + 3;
            tris[t + 5] = v + 2;
        }

        mesh.Clear();
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.colors    = colors;
        mesh.triangles = tris;
    }

    void OnDestroy()
    {
        if (trailObj != null) Destroy(trailObj);
        if (mesh     != null) Destroy(mesh);
    }
}
