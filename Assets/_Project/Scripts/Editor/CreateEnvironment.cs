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
    const string LaserBlueMatPath   = MatDir + "/EnvLaserBlue.mat";
    const string LaserRedMatPath    = MatDir + "/EnvLaserRed.mat";
    const string RibDarkMatPath     = MatDir + "/TunnelFrameBlack.mat";
    const string RibBlueMatPath     = MatDir + "/TunnelFrameBlueGlow.mat";
    const string RibRedMatPath      = MatDir + "/TunnelFrameRedGlow.mat";
    const string OscilloscopeMatPath = MatDir + "/OscilloscopeDim.mat";
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
        if (GameObject.Find("LightShow") != null)
        {
            Debug.Log("[CreateEnvironment] 'LightShow' 가 이미 씬에 있습니다.");
            return;
        }

        Material emissive = GetEmissiveMaterial();
        Material laserBlue = GetEmissiveMaterial(LaserBlueMatPath, new Color(0.55f, 1.35f, 5.2f, 1f));
        Material laserRed = GetEmissiveMaterial(LaserRedMatPath, new Color(5.8f, 0.35f, 0.35f, 1f));
        Material ribDark = GetMaterial(RibDarkMatPath, new Color(0.006f, 0.008f, 0.012f, 1f), false);
        Material ribBlue = GetEmissiveMaterial(RibBlueMatPath, new Color(0.28f, 1.0f, 5.2f, 1f));
        Material ribRed = GetEmissiveMaterial(RibRedMatPath, new Color(4.8f, 0.22f, 0.24f, 1f));
        var root = new GameObject("LightShow");
        if (UnityEngine.Object.FindFirstObjectByType<EnvColorManager>() == null)
            root.AddComponent<EnvColorManager>(); // 전역 색 동기화 + 슬라이스 색반응

        BuildSideLasers(root.transform, emissive);
        BuildRings(root.transform, ribDark, ribBlue, ribRed);
        BuildReferenceRearLasers(root.transform, laserBlue);
        BuildMovingUpLasers(root.transform, laserBlue, laserRed);
        BuildMirrorProbe(root.transform);
        UpgradeFloorToMirror();

        MarkDirty();
        Debug.Log("[CreateEnvironment] Light Show 생성 완료 — 비트세이버식 터널 링 + 후방 수렴 레이저 + 미러 프로브.");
    }

    static void BuildSideLasers(Transform parent, Material emissive)
    {
        // 양옆 세로 라이트 기둥 — 레퍼런스처럼 줄지어 서서 은은히 펄스(스윕 아님)
        var root = new GameObject("SideLights");
        root.transform.SetParent(parent, false);

        const int   perSide = 11;
        const float colX     = 3.0f;   // 레인 끝(1.2)에서 떨어진 옆
        const float colH     = 4.6f;   // 기둥 높이
        float       zStep    = (TrackEnd - TrackStart) / perSide;

        foreach (int side in new[] { -1, 1 })
        {
            var wall = new GameObject(side < 0 ? "Lights_Left" : "Lights_Right");
            wall.transform.SetParent(root.transform, false);

            var cols = new Renderer[perSide];
            for (int i = 0; i < perSide; i++)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.name = $"Column_{i}";
                col.transform.SetParent(wall.transform, false);
                col.transform.localScale    = new Vector3(0.12f, colH, 0.12f);
                col.transform.localPosition = new Vector3(side * colX, colH * 0.5f, TrackStart + zStep * (i + 0.5f));
                Object.DestroyImmediate(col.GetComponent<Collider>());
                var mr = col.GetComponent<MeshRenderer>();
                mr.sharedMaterial    = emissive;
                mr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows     = false;
                cols[i] = mr;
            }

            var sl = wall.AddComponent<SideLasers>();
            sl.columns       = cols;
            sl.baseColor     = new Color(0.15f, 0.45f, 2.4f);
            sl.beatColor     = new Color(0.45f, 1.4f, 6.0f);
            sl.wavePerColumn = side < 0 ? 0.5f : -0.5f;        // 좌우 반대 방향 물결
        }
    }

    static void BuildRings(Transform parent, Material ribDark, Material ribBlue, Material ribRed)
    {
        Material emissive = GetEmissiveMaterial();
        var ringsRoot = new GameObject("Rings");
        ringsRoot.transform.SetParent(parent, false);

        var rr = ringsRoot.AddComponent<RotatingRings>();
        const int count = 10;
        const float zNear = 8f, zFar = 36f, t = 0.06f;
        Color red = new Color(5.5f, 0.4f, 0.4f);
        Color blue = new Color(0.4f, 1.1f, 6f);
        var ringList = new RotatingRings.Ring[count];

        for (int i = 0; i < count; i++)
        {
            float u = (float)i / (count - 1);
            float z = Mathf.Lerp(zNear, zFar, u);
            // 멀리 가도 레인(폭 2.4)보다 항상 크게. 비트세이버스럽게 넉넉히.
            float w = Mathf.Lerp(5.0f, 3.2f, u);
            float h = w * 0.92f;

            var pivot = new GameObject($"Ring_{i}");
            pivot.transform.SetParent(ringsRoot.transform, false);
            // 중심 y=1.0 — 바닥바는 floor 아래로 숨고 윗바는 노트 위로
            pivot.transform.localPosition = new Vector3(0f, 1.0f, z);

            var renderers = new[]
            {
                MakeBar(pivot.transform, "Top",    new Vector3(0f,  h * 0.5f, 0f), new Vector3(w, t, t), emissive),
                MakeBar(pivot.transform, "Bottom", new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, t, t), emissive),
                MakeBar(pivot.transform, "Left",   new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, t), emissive),
                MakeBar(pivot.transform, "Right",  new Vector3( w * 0.5f, 0f, 0f), new Vector3(t, h, t), emissive),
            };

            ringList[i] = new RotatingRings.Ring
            {
                pivot     = pivot.transform,
                renderers = renderers,
                color     = (i & 1) == 0 ? red : blue,
            };
        }

        rr.rings = ringList;
    }

    static void BuildReferenceRearLasers(Transform parent, Material laserMat)
    {
        var root = new GameObject("ReferenceRearLasers");
        root.transform.SetParent(parent, false);
        Vector3 vanish = new Vector3(0f, 1.45f, 36.5f);

        foreach (int side in new[] { -1, 1 })
        {
            MakeBeam(root.transform, $"UpperFan_{side}_0", vanish + new Vector3(side * 0.20f,  0.15f, 0f), new Vector3(side * 13.5f, 6.7f,  8.5f), 0.035f, laserMat);
            MakeBeam(root.transform, $"UpperFan_{side}_1", vanish + new Vector3(side * 0.55f,  0.00f, -0.5f), new Vector3(side * 10.0f, 5.2f, 10.0f), 0.030f, laserMat);
            MakeBeam(root.transform, $"MidFan_{side}_0",   vanish + new Vector3(side * 0.35f, -0.25f, 0f), new Vector3(side *  8.5f, 2.8f,  6.2f), 0.030f, laserMat);
            MakeBeam(root.transform, $"LowRail_{side}_0",  new Vector3(side * 0.7f, 0.62f, 30f), new Vector3(side * 5.3f, 0.82f, 4.2f), 0.032f, laserMat);
            MakeBeam(root.transform, $"SideWall_{side}_0", new Vector3(side * 6.0f, 1.25f,  6f), new Vector3(side * 11.5f, 4.8f, 34f), 0.030f, laserMat);
        }

        MakeBeam(root.transform, "CenterChevron_Left",  new Vector3(-0.55f, 1.75f, 24f), new Vector3(0f, 2.10f, 27.3f), 0.035f, laserMat);
        MakeBeam(root.transform, "CenterChevron_Right", new Vector3( 0.55f, 1.75f, 24f), new Vector3(0f, 2.10f, 27.3f), 0.035f, laserMat);
    }

    static void BuildMovingUpLasers(Transform parent, Material blueLaser, Material redLaser)
    {
        var root = new GameObject("MovingUpLasers");
        root.transform.SetParent(parent, false);
        var cross = root.AddComponent<CrossLaserSystem>();
        cross.independentMode = true;
        cross.sweepAngle = 60f;
        cross.oscSpeed = 0.22f;
        cross.restColor = new Color(0.25f, 0.85f, 4.0f, 1f);
        cross.beatColor = new Color(0.65f, 1.7f, 7.5f, 1f);
        cross.impulseDecay = 3.0f;

        var left = new System.Collections.Generic.List<CrossLaserSystem.LaserBeam>();
        var right = new System.Collections.Generic.List<CrossLaserSystem.LaserBeam>();
        foreach (int side in new[] { -1, 1 })
        {
            for (int i = 0; i < 5; i++)
            {
                float z = 19.0f + i * 5.1f;
                float x = side * (4.45f + i * 0.72f);
                var pivot = new GameObject($"UpLaserPivot_{side}_{i}");
                pivot.transform.SetParent(root.transform, false);
                pivot.transform.localPosition = new Vector3(x, 0.05f, z);
                pivot.transform.localEulerAngles = new Vector3(0f, 0f, side * (24f + i * 4f));

                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = "Beam";
                beam.transform.SetParent(pivot.transform, false);
                beam.transform.localPosition = new Vector3(0f, 9f, 0f);
                beam.transform.localScale = new Vector3(0.11f, 18f, 0.11f);
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                var renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = side < 0 ? blueLaser : redLaser;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var laser = new CrossLaserSystem.LaserBeam { pivot = pivot.transform, renderer = renderer };
                if (side < 0) left.Add(laser); else right.Add(laser);
            }
        }

        cross.leftBeams = left.ToArray();
        cross.rightBeams = right.ToArray();
    }

    static Renderer MakeBeam(Transform parent, string name, Vector3 a, Vector3 b, float thickness, Material mat)
    {
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = name;
        beam.transform.SetParent(parent, false);
        Vector3 dir = b - a;
        beam.transform.localPosition = (a + b) * 0.5f;
        beam.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, dir.normalized);
        beam.transform.localScale = new Vector3(thickness, thickness, dir.magnitude);
        Object.DestroyImmediate(beam.GetComponent<Collider>());
        var mr = beam.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return mr;
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

    // ── 대각선 레이저 (위로 향하는 \\\/// 형태, 게임존 안 침범) ────
    // 좌/우 별도 GameObject로 분리: DiagonalLasers_Right (\\\), DiagonalLasers_Left (///).
    // 각각 3개 빔 × y층 (0, +3, +6). 빔 경로는 게임 플레이 영역 절대 통과하지 않음.
    [MenuItem("VRBeat/Create Diagonal Lasers")]
    public static void CreateDiagonalLasers()
    {
        foreach (var n in new[] { "DiagonalLasers", "DiagonalLasers_Right", "DiagonalLasers_Left" })
        {
            var existing = GameObject.Find(n);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        var lightShow = GameObject.Find("LightShow");
        Transform parent = lightShow != null ? lightShow.transform : null;
        Material laserMat = GetEmissiveMaterial(LaserBlueMatPath, new Color(0.55f, 1.35f, 5.2f, 1f));

        var sel = UnityEditor.Selection.activeGameObject;
        Vector3 originR = sel != null && Mathf.Abs(sel.transform.position.x) > 0.1f
            ? new Vector3(Mathf.Abs(sel.transform.position.x), sel.transform.position.y, sel.transform.position.z)
            : new Vector3(22f, 1.4f, 40f);

        Vector3 targetR = new Vector3(-originR.x * 0.45f, originR.y + 10.6f, 20f);
        Vector3 originL = new Vector3(-originR.x, originR.y, originR.z);
        Vector3 targetL = new Vector3(-targetR.x, targetR.y, targetR.z);
        float[] yOffsets = { 0f, 3f, 6f };

        // 우측 (\\\) — DiagonalLasers_Right
        var rootR = new GameObject("DiagonalLasers_Right");
        if (parent != null) rootR.transform.SetParent(parent, true);
        rootR.transform.position = Vector3.zero;
        BuildBeamLayers(rootR.transform, "DiagR", originR, targetR, yOffsets, 0.05f, laserMat);

        // 좌측 (///) — DiagonalLasers_Left
        var rootL = new GameObject("DiagonalLasers_Left");
        if (parent != null) rootL.transform.SetParent(parent, true);
        rootL.transform.position = Vector3.zero;
        BuildBeamLayers(rootL.transform, "DiagL", originL, targetL, yOffsets, 0.05f, laserMat);

        MarkDirty();
        Debug.Log($"[CreateEnvironment] DiagonalLasers 우/좌 분리 — origin x=±{originR.x}, target y up to +10.6.");
    }

    // ── Λ 부채꼴 (정중앙 상부 한 점에서 4방향 아래로 펼침) ─────────
    [MenuItem("VRBeat/Create Chevron Lasers")]
    public static void CreateChevronLasers()
    {
        var existing = GameObject.Find("ChevronLasers");
        if (existing != null) Object.DestroyImmediate(existing);

        var lightShow = GameObject.Find("LightShow");
        Transform parent = lightShow != null ? lightShow.transform : null;
        Material mat = GetEmissiveMaterial(LaserRedMatPath, new Color(5.8f, 0.35f, 0.35f, 1f));

        var root = new GameObject("ChevronLasers");
        if (parent != null) root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;

        Vector3 apex = new Vector3(0f, 14f, 28f); // 정중앙 상부
        Vector3[] feet =
        {
            new Vector3(-12f, 5f, 38f),
            new Vector3(-12f, 5f, 18f),
            new Vector3( 12f, 5f, 18f),
            new Vector3( 12f, 5f, 38f),
        };
        for (int i = 0; i < feet.Length; i++)
        {
            var beam = MakeBeam(root.transform, $"Chev_{i}", apex, feet[i], 0.045f, mat);
            beam.gameObject.AddComponent<EnvSyncedBeam>();
        }
        MarkDirty();
        Debug.Log("[CreateEnvironment] ChevronLasers — apex (0,14,28) → 4방향 아래.");
    }

    // ── X 크로스 (게임존 한참 위에서 교차) ─────────────────────────
    [MenuItem("VRBeat/Create Cross Lasers")]
    public static void CreateCrossLasers()
    {
        var existing = GameObject.Find("CrossLasers");
        if (existing != null) Object.DestroyImmediate(existing);

        var lightShow = GameObject.Find("LightShow");
        Transform parent = lightShow != null ? lightShow.transform : null;
        Material mat = GetEmissiveMaterial(LaserBlueMatPath, new Color(0.55f, 1.35f, 5.2f, 1f));

        var root = new GameObject("CrossLasers");
        if (parent != null) root.transform.SetParent(parent, true);
        root.transform.position = Vector3.zero;

        // 두 빔이 (0, 10, 20) 부근에서 교차, 모든 점 y ≥ 8
        Vector3[][] beams =
        {
            new[]{ new Vector3(-15f,  8f,  5f), new Vector3( 15f, 12f, 35f) }, // /
            new[]{ new Vector3( 15f,  8f,  5f), new Vector3(-15f, 12f, 35f) }, // \
        };
        for (int i = 0; i < beams.Length; i++)
        {
            var beam = MakeBeam(root.transform, $"Cross_{i}", beams[i][0], beams[i][1], 0.05f, mat);
            beam.gameObject.AddComponent<EnvSyncedBeam>();
        }
        MarkDirty();
        Debug.Log("[CreateEnvironment] CrossLasers — y=8~12 상부에서 X자 교차.");
    }

    static void BuildBeamLayers(Transform parent, string prefix, Vector3 from, Vector3 to, float[] yOffsets, float thickness, Material mat)
    {
        for (int i = 0; i < yOffsets.Length; i++)
        {
            Vector3 a = from + new Vector3(0f, yOffsets[i], 0f);
            Vector3 b = to   + new Vector3(0f, yOffsets[i], 0f);
            var beam = MakeBeam(parent, $"{prefix}_{i}", a, b, thickness, mat);
            beam.gameObject.AddComponent<EnvSyncedBeam>();
        }
    }

    // ── 사이드 이퀄라이저 (주변부, 어둑한 배경 음향 반응) ─────────
    // 비트세이버식 클래식 EQ: 멀리 바깥에 두고 Y축 15° 안쪽으로 돌려 플레이어 방향을 향하게.
    // 세그는 가로>세로 직사각, 컬럼·세그 사이 갭 또렷.
    [MenuItem("VRBeat/Create Side Equalizer")]
    public static void CreateSideEqualizer()
    {
        var existing = GameObject.Find("OscilloscopeWalls");
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject("OscilloscopeWalls");
        root.transform.SetParent(GameObject.Find("Environment")?.transform, false);
        Material oscMaterial = GetEmissiveMaterial(OscilloscopeMatPath, Color.black);

        const int   columns     = 50;
        const int   segments    = 16;      // 위로 더 쌓아 EQ 막대 키 큼
        const float zNear       = 2.8f;
        const float zFar        = 40.5f;
        const float wallX       = 13.2f;   // 멀리 바깥 — Y회전으로 안쪽으로 수렴
        const float wallY       = 0.55f;   // 살짝 내림 — 시야 안에 더 잘 들어옴
        const float wallZ       = 1.65f;
        const float yawDeg      = 15f;     // Y축 회전 — 플레이어 방향 가시성
        const float yBase       = 0.0f;
        const float yStep       = 0.28f;
        Vector3     segmentSize = new Vector3(0.10f, 0.20f, 0.60f); // X 얇게 / 가로>세로 직사각, 컬럼 빽빽

        foreach (int side in new[] { -1, 1 })
        {
            var wall = new GameObject(side < 0 ? "Wall_Left" : "Wall_Right");
            wall.transform.SetParent(root.transform, false);
            wall.transform.localPosition = new Vector3(side * wallX, wallY, wallZ);
            wall.transform.localRotation = Quaternion.Euler(0f, -side * yawDeg, 0f);

            var bars = new Transform[columns * segments];
            for (int col = 0; col < columns; col++)
            {
                float u = (float)col / (columns - 1);
                float z = Mathf.Lerp(zNear, zFar, u);

                for (int seg = 0; seg < segments; seg++)
                {
                    var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.name = $"Seg_{col}_{seg}";
                    bar.transform.SetParent(wall.transform, false);
                    bar.transform.localScale    = segmentSize;
                    bar.transform.localPosition = new Vector3(0f, yBase + seg * yStep, z);
                    bar.transform.localRotation = Quaternion.identity;
                    Object.DestroyImmediate(bar.GetComponent<Collider>());
                    var mr = bar.GetComponent<MeshRenderer>();
                    mr.sharedMaterial    = oscMaterial;
                    mr.enabled           = false;
                    mr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows     = false;
                    bars[col * segments + seg] = bar.transform;
                }
            }

            var ow = wall.AddComponent<OscilloscopeWall>();
            ow.bars              = bars;
            ow.segmentsPerColumn = segments;
            ow.gain              = 165f;
            ow.smooth            = 14f;
            ow.lowColor          = new Color(0.10f, 0.28f, 2.8f);
            ow.highColor         = new Color(3.2f, 0.18f, 0.18f);
            ow.dimColor          = Color.black;
        }

        MarkDirty();
        Debug.Log($"[CreateEnvironment] Side Equalizer 재생성 — 좌/우 {columns}x{segments}, Y축 {yawDeg}° 안쪽 회전.");
    }

    // ── 머티리얼 ──────────────────────────────────────────────────
    static Material GetEmissiveMaterial() => GetEmissiveMaterial(EmissiveMatPath, new Color(1.6f, 1.6f, 1.6f, 1f));

    static Material GetEmissiveMaterial(string path, Color color) => GetMaterial(path, color, true);

    static Material GetMaterial(string path, Color color, bool emissive)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = emissive ? AssetDatabase.LoadAssetAtPath<Shader>(EmissiveShaderPath) : null;
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("[CreateEnvironment] 머티리얼 셰이더를 찾을 수 없습니다: " + path);
            return null;
        }
        mat = new Material(shader);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.82f);
        SaveMat(mat, path);
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
