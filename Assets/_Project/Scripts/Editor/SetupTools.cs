using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class SetupTools : EditorWindow
{
    [MenuItem("VRBeat/Setup Tools")]
    public static void ShowWindow()
    {
        GetWindow<SetupTools>("VRBeat Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Infrastructure", EditorStyles.boldLabel);
        if (GUILayout.Button("1. Generate GameConfig Asset"))
        {
            GenerateGameConfig();
        }

        if (GUILayout.Button("2. Create Basic Prefabs"))
        {
            CreateBasicPrefabs();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Scene Setup", EditorStyles.boldLabel);
        if (GUILayout.Button("3. Setup Gameplay Scene"))
        {
            SetupGameplayScene();
        }
        
        if (GUILayout.Button("4. Create and Wire HUD"))
        {
            CreateAndWireHUD();
        }
    }

    static void GenerateGameConfig()
    {
        string dir = "Assets/_Project/Resources";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, "GameConfig.asset");
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
        
        // Default values (matching GameConfig.cs fields)
        config.spawnDistance = 30f;
        config.hitDistance = 0f;
        config.despawnDistance = -2f;
        config.laneWidth = 0.6f;
        config.rowHeight = 0.6f;
        config.baseHeight = 0.8f;
        config.poolWarmupCount = 64;

        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[VRBeat] GameConfig generated at {path}");
    }

    static void CreateBasicPrefabs()
    {
        // 1. Normal Note
        GameObject noteObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        noteObj.name = "NormalNote";
        noteObj.transform.localScale = Vector3.one * 0.4f; // Sane default size
        noteObj.AddComponent<NormalNote>();
        var rb = noteObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        noteObj.GetComponent<BoxCollider>().isTrigger = true;
        
        string noteDir = "Assets/_Project/Prefabs/Notes";
        if (!Directory.Exists(noteDir)) Directory.CreateDirectory(noteDir);
        PrefabUtility.SaveAsPrefabAsset(noteObj, Path.Combine(noteDir, "NormalNote.prefab"));
        DestroyImmediate(noteObj);

        // 2. Long Note (Basic shell)
        GameObject longNoteObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        longNoteObj.name = "LongNote";
        longNoteObj.transform.localScale = Vector3.one * 0.4f;
        var longNote = longNoteObj.AddComponent<LongNote>();
        var rbLong = longNoteObj.AddComponent<Rigidbody>();
        rbLong.isKinematic = true;
        longNoteObj.GetComponent<BoxCollider>().isTrigger = true;

        // Add visual parts for LongNote
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(longNoteObj.transform);
        head.transform.localPosition = Vector3.zero;
        head.transform.localScale = Vector3.one;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(longNoteObj.transform);
        body.transform.localPosition = new Vector3(0, 0, 0.5f);
        body.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tail.name = "Tail";
        tail.transform.SetParent(longNoteObj.transform);
        tail.transform.localPosition = new Vector3(0, 0, 1f);
        tail.transform.localScale = Vector3.one;

        // Wire LongNote (assuming fields exist in LongNote.cs)
        var serializedLongNote = new SerializedObject(longNote);
        var headProp = serializedLongNote.FindProperty("head");
        if (headProp != null) headProp.objectReferenceValue = head.transform;
        var bodyProp = serializedLongNote.FindProperty("body");
        if (bodyProp != null) bodyProp.objectReferenceValue = body.transform;
        var tailProp = serializedLongNote.FindProperty("tail");
        if (tailProp != null) tailProp.objectReferenceValue = tail.transform;
        serializedLongNote.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(longNoteObj, Path.Combine(noteDir, "LongNote.prefab"));
        DestroyImmediate(longNoteObj);

        // 3. Saber
        GameObject saberObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        saberObj.name = "Saber";
        saberObj.transform.localScale = new Vector3(0.05f, 0.5f, 0.05f); // Thin and long
        saberObj.transform.rotation = Quaternion.Euler(90, 0, 0); // Pointing forward
        
        var controller = saberObj.AddComponent<SaberController>();
        saberObj.GetComponent<CapsuleCollider>().isTrigger = true;

        // Add Tip and Root for SaberController
        GameObject tip = new GameObject("Tip");
        tip.transform.SetParent(saberObj.transform);
        tip.transform.localPosition = new Vector3(0, 1, 0); // Top of cylinder

        GameObject root = new GameObject("Root");
        root.transform.SetParent(saberObj.transform);
        root.transform.localPosition = new Vector3(0, -1, 0); // Bottom of cylinder

        // Wire controller (using serialized object because fields might be private)
        var serializedSaber = new SerializedObject(controller);
        serializedSaber.FindProperty("tip").objectReferenceValue = tip.transform;
        serializedSaber.FindProperty("root").objectReferenceValue = root.transform;
        serializedSaber.ApplyModifiedProperties();

        string saberDir = "Assets/_Project/Prefabs/Sabers";
        if (!Directory.Exists(saberDir)) Directory.CreateDirectory(saberDir);
        PrefabUtility.SaveAsPrefabAsset(saberObj, Path.Combine(saberDir, "Saber.prefab"));
        DestroyImmediate(saberObj);

        Debug.Log("[VRBeat] Basic Prefabs created in Assets/_Project/Prefabs/");
    }

    static void SetupGameplayScene()
    {
        // 1. Managers
        GameObject managers = new GameObject("[Managers]");
        managers.AddComponent<GameManager>();
        
        var conductorObj = new GameObject("Conductor");
        var conductor = conductorObj.AddComponent<Conductor>();
        conductorObj.transform.SetParent(managers.transform);
        var audioSource = conductorObj.AddComponent<AudioSource>();

        // Wire Conductor to AudioSource
        var serializedConductor = new SerializedObject(conductor);
        serializedConductor.FindProperty("audioSource").objectReferenceValue = audioSource;
        serializedConductor.ApplyModifiedProperties();

        var scoreManager = new GameObject("ScoreManager").AddComponent<ScoreManager>();
        scoreManager.transform.SetParent(managers.transform);

        var healthSystem = new GameObject("HealthSystem").AddComponent<HealthSystem>();
        healthSystem.transform.SetParent(managers.transform);

        // 2. Gameplay
        GameObject gameplay = new GameObject("[Gameplay]");
        
        var pool = new GameObject("NotePool").AddComponent<NotePool>();
        pool.transform.SetParent(gameplay.transform);

        var spawner = new GameObject("NoteSpawner").AddComponent<NoteSpawner>();
        spawner.transform.SetParent(gameplay.transform);

        // Wiring Spawner to Pool and Config
        var serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("pool").objectReferenceValue = pool;
        
        var configAsset = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Resources/GameConfig.asset");
        if (configAsset != null)
            serializedSpawner.FindProperty("config").objectReferenceValue = configAsset;
            
        serializedSpawner.ApplyModifiedProperties();

        // Wiring Pool Prefabs (if they exist)
        var normalNotePrefab = AssetDatabase.LoadAssetAtPath<NoteBase>("Assets/_Project/Prefabs/Notes/NormalNote.prefab");
        var longNotePrefab = AssetDatabase.LoadAssetAtPath<NoteBase>("Assets/_Project/Prefabs/Notes/LongNote.prefab");
        
        var serializedPool = new SerializedObject(pool);
        if (normalNotePrefab != null)
            serializedPool.FindProperty("normalNotePrefab").objectReferenceValue = normalNotePrefab;
        if (longNotePrefab != null)
            serializedPool.FindProperty("longNotePrefab").objectReferenceValue = longNotePrefab;
        
        serializedPool.ApplyModifiedProperties();

        Debug.Log("[VRBeat] Gameplay Scene Setup Complete. Don't forget to add XR Rig and HUD!");
    }

    static void CreateAndWireHUD()
    {
        // 1. Create Canvas
        GameObject canvasObj = new GameObject("HUD");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1000, 1000); 
        canvasRect.localScale = Vector3.one * 0.001f;  
        canvasRect.position = new Vector3(0, 1.2f, 1.5f); 

        var hud = canvasObj.AddComponent<HUD>();

        // 2. Create Score Text
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(canvasObj.transform);
        scoreObj.transform.localPosition = Vector3.zero; // Reset Z offset
        var scoreRT = scoreObj.AddComponent<RectTransform>();
        scoreRT.sizeDelta = new Vector2(600, 100); 
        var scoreTMP = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = "0";
        scoreTMP.fontSize = 80;
        scoreTMP.alignment = TextAlignmentOptions.Center;
        scoreRT.anchoredPosition = new Vector2(0, 150);
        scoreRT.localScale = Vector3.one;

        // 3. Create Combo Text
        GameObject comboObj = new GameObject("ComboText");
        comboObj.transform.SetParent(canvasObj.transform);
        comboObj.transform.localPosition = Vector3.zero; // Reset Z offset
        var comboRT = comboObj.AddComponent<RectTransform>();
        comboRT.sizeDelta = new Vector2(400, 80); 
        var comboTMP = comboObj.AddComponent<TextMeshProUGUI>();
        comboTMP.text = "";
        comboTMP.fontSize = 60;
        comboTMP.alignment = TextAlignmentOptions.Center;
        comboRT.anchoredPosition = new Vector2(0, 50);
        comboRT.localScale = Vector3.one;

        // 4. Create Grade Text
        GameObject gradeObj = new GameObject("GradeText");
        gradeObj.transform.SetParent(canvasObj.transform);
        gradeObj.transform.localPosition = Vector3.zero; // Reset Z offset
        var gradeRT = gradeObj.AddComponent<RectTransform>();
        gradeRT.sizeDelta = new Vector2(400, 100); 
        var gradeTMP = gradeObj.AddComponent<TextMeshProUGUI>();
        gradeTMP.text = "";
        gradeTMP.fontSize = 70;
        gradeTMP.alignment = TextAlignmentOptions.Center;
        gradeRT.anchoredPosition = new Vector2(0, -50);
        gradeRT.localScale = Vector3.one;

        // 5. Create Health Slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(canvasObj.transform);
        sliderObj.transform.localPosition = Vector3.zero; // Reset Z offset
        var sliderRT = sliderObj.AddComponent<RectTransform>();
        var slider = sliderObj.AddComponent<Slider>();
        sliderRT.sizeDelta = new Vector2(500, 40); 
        sliderRT.anchoredPosition = new Vector2(0, -150);
        sliderRT.localScale = Vector3.one;

        // Create slider visual structure
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform);
        bg.transform.localPosition = Vector3.zero;
        var bgRT = bg.AddComponent<RectTransform>();
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bgRT.localScale = Vector3.one;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        fillArea.transform.localPosition = Vector3.zero;
        var faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.sizeDelta = new Vector2(-10, -10); 
        faRT.localScale = Vector3.one;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        fill.transform.localPosition = Vector3.zero;
        var fRT = fill.AddComponent<RectTransform>();
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = Color.green;
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.sizeDelta = Vector2.zero;
        fRT.localScale = Vector3.one;

        slider.fillRect = fRT;
        slider.targetGraphic = fillImg;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;

        // 6. Wire everything to HUD component
        var serializedHUD = new SerializedObject(hud);
        serializedHUD.FindProperty("scoreText").objectReferenceValue = scoreTMP;
        serializedHUD.FindProperty("comboText").objectReferenceValue = comboTMP;
        serializedHUD.FindProperty("gradeText").objectReferenceValue = gradeTMP;
        serializedHUD.FindProperty("healthSlider").objectReferenceValue = slider;
        serializedHUD.ApplyModifiedProperties();

        Debug.Log("[VRBeat] HUD Scaled and Z-Positions reset to zero.");
    }
}
