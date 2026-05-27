using UnityEngine;
using UnityEditor;
using System.IO;

// Beat Saber 스타일 환경 비주얼 빌더.
//  - VRBeat/Create Environment Floor   : 어두운 광택 바닥 + 발광 레인 구분선(박자 펄스)
//  - VRBeat/Create Oscilloscope Walls  : 좌우 오디오 리액티브 막대 벽
// 좌표계: 노트는 z 0→30 으로 날아오고, 4레인(폭 0.6) → x [-1.2, 1.2]. 바닥 y=0.
public static class CreateEnvironment
{
    const string EmissiveShaderPath = "Assets/_Project/Shaders/Emissive.shader";
    const string MatDir             = "Assets/_Project/Materials";
    const string EmissiveMatPath    = MatDir + "/EnvEmissive.mat";
    const string FloorMatPath       = MatDir + "/HighwayFloor.mat";

    const float HalfWidth  = 1.2f;   // 레인 끝 (x = ±1.2)
    const float TrackStart = -2f;    // 바닥 시작 z
    const float TrackEnd   = 32f;    // 바닥 끝 z

    // ── 바닥 ──────────────────────────────────────────────────────
    [MenuItem("VRBeat/Create Environment Floor")]
    public static void CreateFloor()
    {
        if (GameObject.Find("Environment") != null)
        {
            Debug.Log("[CreateEnvironment] 'Environment' 가 이미 씬에 있습니다.");
            return;
        }

        var root = new GameObject("Environment");

        // 바닥면: Plane(10x10, +Y 노멀) 을 스케일
        float length = TrackEnd - TrackStart;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "HighwayFloor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3((HalfWidth * 2f + 0.4f) / 10f, 1f, length / 10f);
        floor.transform.localPosition = new Vector3(0f, 0f, (TrackStart + TrackEnd) * 0.5f);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.GetComponent<MeshRenderer>().sharedMaterial = GetFloorMaterial();

        // 레인 구분선 5개: x = -1.2, -0.6, 0, 0.6, 1.2
        Material emissive = GetEmissiveMaterial();
        float[] xs = { -1.2f, -0.6f, 0f, 0.6f, 1.2f };
        var renderers = new Renderer[xs.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"LaneLine_{i}";
            line.transform.SetParent(root.transform, false);
            line.transform.localScale    = new Vector3(0.05f, 0.02f, length);
            line.transform.localPosition = new Vector3(xs[i], 0.012f, (TrackStart + TrackEnd) * 0.5f);
            Object.DestroyImmediate(line.GetComponent<Collider>());
            var mr = line.GetComponent<MeshRenderer>();
            mr.sharedMaterial = emissive;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            renderers[i] = mr;
        }

        var hf = root.AddComponent<HighwayFloor>();
        hf.lineRenderers = renderers;

        MarkDirty();
        Debug.Log("[CreateEnvironment] Floor 생성 완료 — 바닥 + 레인 구분선 5개(박자 펄스).");
    }

    // ── 라이트쇼 (레이저 + 링 + 미러 프로브) ──────────────────────
    [MenuItem("VRBeat/Create Light Show")]
    public static void CreateLightShow()
    {
        // 1) 구버전 오실로스코프 벽 제거(FFT 막대 → 비트 구동으로 대체)
        var oldWalls = GameObject.Find("OscilloscopeWalls");
        if (oldWalls != null) Object.DestroyImmediate(oldWalls);

        if (GameObject.Find("LightShow") != null)
        {
            Debug.Log("[CreateEnvironment] 'LightShow' 가 이미 씬에 있습니다.");
            return;
        }

        Material emissive = GetEmissiveMaterial();
        var root = new GameObject("LightShow");

        BuildSideLasers(root.transform, emissive);
        BuildRings(root.transform, emissive);
        BuildMirrorProbe(root.transform);
        UpgradeFloorToMirror();

        MarkDirty();
        Debug.Log("[CreateEnvironment] Light Show 생성 완료 — 측면 레이저 + 회전 링 + 미러 프로브.");
    }

    static void BuildSideLasers(Transform parent, Material emissive)
    {
        const int perSide   = 10;
        float     zStep      = (TrackEnd - TrackStart - 4f) / (perSide - 1);
        Color     redLit     = new Color(4f, 0.25f, 0.25f);
        Color     blueLit    = new Color(0.3f, 0.6f, 4f);

        foreach (int side in new[] { -1, 1 })
        {
            var laserRoot = new GameObject(side < 0 ? "Lasers_Left" : "Lasers_Right");
            laserRoot.transform.SetParent(parent, false);

            var gA = new System.Collections.Generic.List<Renderer>();
            var gB = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < perSide; i++)
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = $"Beam_{i}";
                beam.transform.SetParent(laserRoot.transform, false);
                beam.transform.localScale    = new Vector3(0.04f, 5f, 0.04f);
                beam.transform.localPosition = new Vector3(side * 1.55f, 2.5f, TrackStart + 2f + zStep * i);
                beam.transform.localRotation = Quaternion.Euler(0f, 0f, side * 8f); // 바깥쪽 살짝 기울임
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                var mr = beam.GetComponent<MeshRenderer>();
                mr.sharedMaterial   = emissive;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                ((i & 1) == 0 ? gA : gB).Add(mr);
            }

            var sl = laserRoot.AddComponent<SideLasers>();
            sl.groupA  = gA.ToArray();
            sl.groupB  = gB.ToArray();
            sl.litColor = side < 0 ? redLit : blueLit;
            sl.dimColor = side < 0 ? new Color(0.12f, 0.02f, 0.02f) : new Color(0.02f, 0.04f, 0.12f);
        }
    }

    static void BuildRings(Transform parent, Material emissive)
    {
        const int count = 6;
        float[] zs = { 4f, 9f, 14f, 19f, 24f, 29f };
        float   w = 3.4f, h = 3.2f, t = 0.06f;
        Color   red  = new Color(4f, 0.25f, 0.25f);
        Color   blue = new Color(0.3f, 0.6f, 4f);

        var ringsRoot = new GameObject("Rings");
        ringsRoot.transform.SetParent(parent, false);
        var rr = ringsRoot.AddComponent<RotatingRings>();
        var ringList = new RotatingRings.Ring[count];

        for (int i = 0; i < count; i++)
        {
            var pivot = new GameObject($"Ring_{i}");
            pivot.transform.SetParent(ringsRoot.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.5f, zs[i]);

            // 사각 4변
            var bars = new[]
            {
                MakeBar(pivot.transform, "Top",    new Vector3(0f,  h * 0.5f, 0f), new Vector3(w, t, t), emissive),
                MakeBar(pivot.transform, "Bottom", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, t), emissive),
                MakeBar(pivot.transform, "Left",   new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, t), emissive),
                MakeBar(pivot.transform, "Right",  new Vector3( w * 0.5f, 0f, 0f), new Vector3(t, h, t), emissive),
            };

            ringList[i] = new RotatingRings.Ring
            {
                pivot     = pivot.transform,
                renderers = bars,
                color     = (i & 1) == 0 ? red : blue,
            };
        }
        rr.rings = ringList;
    }

    static Renderer MakeBar(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = pos;
        bar.transform.localScale    = scale;
        Object.DestroyImmediate(bar.GetComponent<Collider>());
        var mr = bar.GetComponent<MeshRenderer>();
        mr.sharedMaterial   = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;
        return mr;
    }

    static void BuildMirrorProbe(Transform parent)
    {
        var go = new GameObject("MirrorProbe");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, 10f);

        var probe = go.AddComponent<ReflectionProbe>();
        probe.size            = new Vector3(7f, 8f, 34f);
        probe.boxProjection   = true;
        probe.resolution      = 128;
        probe.hdr             = true;
        probe.clearFlags      = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
        probe.cullingMask     = ~0;
        probe.importance      = 1;
        go.AddComponent<MirrorFloorProbe>();
    }

    static void UpgradeFloorToMirror()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);
        if (mat == null) return;
        mat.SetFloat("_Smoothness", 0.95f);
        mat.SetFloat("_Metallic",   0.7f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
    }

    // ── 머티리얼 ──────────────────────────────────────────────────
    static Material GetEmissiveMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(EmissiveMatPath);
        if (mat != null) return mat;

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(EmissiveShaderPath);
        if (shader == null)
        {
            Debug.LogError("[CreateEnvironment] Emissive.shader 를 찾을 수 없습니다: " + EmissiveShaderPath);
            return null;
        }
        mat = new Material(shader);
        mat.SetColor("_Color", new Color(1.6f, 1.6f, 1.6f, 1f));
        SaveMat(mat, EmissiveMatPath);
        return mat;
    }

    static Material GetFloorMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(FloorMatPath);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[CreateEnvironment] URP/Lit 셰이더를 찾을 수 없습니다.");
            return null;
        }
        mat = new Material(shader);
        mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.035f, 1f)); // 거의 검정
        mat.SetFloat("_Smoothness", 0.88f);  // 광택 ↑ (반사감)
        mat.SetFloat("_Metallic",   0.55f);
        SaveMat(mat, FloorMatPath);
        return mat;
    }

    static void SaveMat(Material mat, string path)
    {
        Directory.CreateDirectory(MatDir);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
    }

    static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
