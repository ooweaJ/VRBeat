using System.Collections.Generic;
using UnityEngine;

public class SaberTrailCheck : MonoBehaviour
{
    [SerializeField] SaberController saber;
    [SerializeField] Transform trailTip;
    [SerializeField] Transform trailRoot;
    [SerializeField] float trailThreshold = 0.4f;
    [SerializeField] float trailTime      = 0.5f;
    [SerializeField] float widthExpand    = 0.05f;
    [SerializeField] Material trailMaterial;

    struct Segment { public Vector3 tip, root; public float time; }
    readonly List<Segment> segments = new();

    GameObject   trailObj;
    MeshFilter   mf;
    MeshRenderer mr;
    Mesh         mesh;

    static readonly Color kRed  = new Color(1.0f, 0.15f, 0.15f);
    static readonly Color kBlue = new Color(0.15f, 0.5f, 1.0f);

    void Awake()
    {
        if (saber == null) saber = GetComponentInParent<SaberController>();
        if (trailTip  == null && saber != null) trailTip  = saber.Tip;
        if (trailRoot == null && saber != null) trailRoot = saber.Root;

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
            bool isBlue = saber != null && saber.color == SaberColor.Blue;
            Shader s = Shader.Find("VRBeat/SaberTrail") ?? Shader.Find("Sprites/Default");
            if (s != null)
            {
                var mat = new Material(s);
                mat.SetColor("_BaseColor", isBlue ? new Color(0.25f, 0.65f, 3.5f) : new Color(3.0f, 0.25f, 0.25f));
                var tex = Resources.Load<Texture2D>("SaberTrail_Gradient");
                if (tex != null) mat.SetTexture("_MainTex", tex);
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
            Vector3 tip  = trailTip.position;
            Vector3 root = trailRoot.position;
            Vector3 axis = (tip - root).normalized;
            tip  += axis * widthExpand;
            root -= axis * widthExpand;
            segments.Add(new Segment { tip = tip, root = root, time = now });
        }

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

        bool isBlue   = saber != null && saber.color == SaberColor.Blue;
        Color saberCol = isBlue ? kBlue : kRed;

        for (int i = 0; i < n; i++)
        {
            // u=0 tail(oldest), u=1 head(newest)
            float u        = (float)i / (n - 1);
            // alpha: ease-out curve — solid near head, fade at tail
            float alpha    = Mathf.Pow(u, 0.6f);
            // slight brightness boost at head for the "hot" look
            float bright   = Mathf.Lerp(0.85f, 1.0f, u);

            Color c = new Color(
                saberCol.r * bright,
                saberCol.g * bright,
                saberCol.b * bright,
                alpha
            );

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
