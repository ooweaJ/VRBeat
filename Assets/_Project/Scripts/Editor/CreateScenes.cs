using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEditor.Events;

public static class CreateScenes
{
    [MenuItem("VRBeat/Create All Scenes")]
    public static void CreateAllScenes()
    {
        var font = TMP_Settings.defaultFontAsset;
        RenameGameScene();
        CreatePrefabs(font);
        BuildSongSelectScene(font);
        BuildResultScene(font);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateScenes] Done! SongSelect.unity, Result.unity created.");
    }

    // ── Step 1 ────────────────────────────────────────────────────
    static void RenameGameScene()
    {
        const string path = "Assets/_Project/Scenes/Game.unity";
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            string err = AssetDatabase.RenameAsset(path, "Gameplay");
            Debug.Log(string.IsNullOrEmpty(err) ? "Renamed Game.unity -> Gameplay.unity" : "Rename error: " + err);
        }
        else Debug.LogWarning("Game.unity not found, skipping rename");
    }

    // ── Step 2 ────────────────────────────────────────────────────
    static void CreatePrefabs(TMP_FontAsset font)
    {
        MakeButtonPrefab("Assets/_Project/Prefabs/SongItem.prefab", "SongItem",
            new Vector2(360, 60), 24, "Song Name", new Color(0.18f, 0.18f, 0.22f), font);
        MakeButtonPrefab("Assets/_Project/Prefabs/DifficultyButton.prefab", "DifficultyButton",
            new Vector2(150, 50), 22, "Normal", new Color(0.1f, 0.35f, 0.75f), font);
    }

    static void MakeButtonPrefab(string savePath, string goName, Vector2 size,
                                   float fontSize, string label, Color bgColor, TMP_FontAsset font)
    {
        if (AssetDatabase.LoadMainAssetAtPath(savePath) != null) { Debug.Log(savePath + " already exists"); return; }
        var root = new GameObject(goName);
        root.AddComponent<RectTransform>().sizeDelta = size;
        var img = root.AddComponent<Image>(); img.color = bgColor;
        var btn = root.AddComponent<Button>(); btn.targetGraphic = img;
        var lgo = new GameObject("Label"); lgo.transform.SetParent(root.transform, false);
        Stretch(lgo);
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;
        if (font) tmp.font = font;
        PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);
        Debug.Log("Created prefab: " + savePath);
    }

    // ── Step 3 ────────────────────────────────────────────────────
    static void BuildSongSelectScene(TMP_FontAsset font)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BasicLighting();

        // XR Rig (컨트롤러 포함) — 없으면 기본 카메라 폴백
        Camera cam = null;
        var xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/XR Origin (XR Rig).prefab");
        if (xrPrefab != null)
        {
            var xrGo = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
            cam = xrGo.GetComponentInChildren<Camera>();
            Debug.Log("SongSelect: XR Rig instantiated");
        }
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
            cam.nearClipPlane = 0.01f;
            camGo.transform.position = new Vector3(0, 1.7f, 0);
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("SongLibrary").AddComponent<SongLibrary>();

        // World Space Canvas
        var cvGo = new GameObject("SongSelectCanvas");
        var cv = cvGo.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        cv.worldCamera = cam;
        cvGo.AddComponent<CanvasScaler>();
        cvGo.AddComponent<GraphicRaycaster>();
        cvGo.AddComponent<TrackedDeviceRaycaster>();
        RT(cvGo).sizeDelta = new Vector2(800, 600);
        cvGo.transform.position = new Vector3(0, 1.5f, 2.5f);
        cvGo.transform.localScale = Vector3.one * 0.002f;

        Img("Background", cvGo.transform, V2(0,0), V2(1,1), new Color(0.05f,0.05f,0.12f,0.95f));
        Label("Header", cvGo.transform, V2(0,0.91f), V2(1,1), "SONG SELECT", 38, TextAlignmentOptions.Center, Color.white, font);

        // Left panel: song list
        var leftImg = Img("LeftPanel", cvGo.transform, V2(0.01f,0.02f), V2(0.48f,0.89f), new Color(0.08f,0.08f,0.14f,0.9f));

        var sv = new GameObject("ScrollView"); sv.transform.SetParent(leftImg.transform, false); Stretch(sv);
        sv.AddComponent<Image>().color = new Color(0,0,0,0.01f);
        var scroll = sv.AddComponent<ScrollRect>(); scroll.horizontal = false;

        var vp = new GameObject("Viewport"); vp.transform.SetParent(sv.transform, false); Stretch(vp);
        vp.AddComponent<Image>().color = new Color(0,0,0,0.01f);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content"); content.transform.SetParent(vp.transform, false);
        var crt = content.AddComponent<RectTransform>();
        crt.anchorMin = V2(0,1); crt.anchorMax = V2(1,1); crt.pivot = V2(0.5f,1); crt.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5; vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.padding = new RectOffset(6,6,6,6);
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = vp.GetComponent<RectTransform>(); scroll.content = crt;

        // Right panel: song info
        var rightImg = Img("RightPanel", cvGo.transform, V2(0.52f,0.02f), V2(0.99f,0.89f), new Color(0.08f,0.08f,0.14f,0.9f));

        var cover = Img("CoverImage", rightImg.transform, V2(0.1f,0.62f), V2(0.9f,0.98f), new Color(0.15f,0.15f,0.2f,1f));
        cover.preserveAspect = true;

        var titleTmp  = Label("TitleText",     rightImg.transform, V2(0.05f,0.50f), V2(0.95f,0.62f), "Song Title", 26, TextAlignmentOptions.Center, Color.white, font);
        var artistTmp = Label("ArtistText",    rightImg.transform, V2(0.05f,0.41f), V2(0.95f,0.50f), "Artist",     20, TextAlignmentOptions.Center, new Color(0.8f,0.8f,0.8f), font);
        var hsTmp     = Label("HighScoreText", rightImg.transform, V2(0.05f,0.33f), V2(0.95f,0.41f), "No record",  18, TextAlignmentOptions.Center, new Color(1f,0.85f,0.2f),   font);

        var diffGo = new GameObject("DifficultyPanel"); diffGo.transform.SetParent(rightImg.transform, false);
        Anchors(diffGo, V2(0.05f,0.2f), V2(0.95f,0.32f));
        var hlg = diffGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8; hlg.childControlWidth = false; hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false; hlg.childAlignment = TextAnchor.MiddleCenter;

        var playBtn = MakeBtn("PlayButton", rightImg.transform, V2(0.15f,0.04f), V2(0.85f,0.17f), "PLAY", 28, new Color(0.1f,0.7f,0.2f), font);

        // Wire SongSelectUI
        var ui = cvGo.AddComponent<SongSelectUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("songListParent").objectReferenceValue         = content.transform;
        so.FindProperty("songItemPrefab").objectReferenceValue         = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/SongItem.prefab");
        so.FindProperty("difficultyButtonPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/DifficultyButton.prefab");
        so.FindProperty("titleText").objectReferenceValue              = titleTmp;
        so.FindProperty("artistText").objectReferenceValue             = artistTmp;
        so.FindProperty("highScoreText").objectReferenceValue          = hsTmp;
        so.FindProperty("coverImage").objectReferenceValue             = cover;
        so.FindProperty("difficultyParent").objectReferenceValue       = diffGo.transform;
        so.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(playBtn.onClick, ui.Play);

        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/SongSelect.unity");
        Debug.Log("SongSelect.unity saved");
    }

    // ── Calibration 씬 (단독 생성) ─────────────────────────────────
    [MenuItem("VRBeat/Create Calibration Scene")]
    public static void CreateCalibrationScene()
    {
        var font = TMP_Settings.defaultFontAsset;
        BuildCalibrationScene(font);
        AddSceneToBuildSettings("Assets/_Project/Scenes/Calibration.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateScenes] Calibration.unity created.");
    }

    static void BuildCalibrationScene(TMP_FontAsset font)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BasicLighting();

        Camera cam = null;
        var xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/XR Origin (XR Rig).prefab");
        if (xrPrefab != null)
        {
            var xrGo = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
            cam = xrGo.GetComponentInChildren<Camera>();
        }
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f,0.04f,0.1f);
            cam.nearClipPlane = 0.01f;
            camGo.transform.position = new Vector3(0,1.7f,0);
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        new GameObject("GameManager").AddComponent<GameManager>();

        var cvGo = new GameObject("CalibrationCanvas");
        var cv = cvGo.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        cv.worldCamera = cam;
        cvGo.AddComponent<CanvasScaler>();
        cvGo.AddComponent<GraphicRaycaster>();
        cvGo.AddComponent<TrackedDeviceRaycaster>();
        RT(cvGo).sizeDelta = new Vector2(700, 600);
        cvGo.transform.position = new Vector3(0,1.5f,2.5f);
        cvGo.transform.localScale = Vector3.one * 0.002f;

        Img("Background", cvGo.transform, V2(0,0), V2(1,1), new Color(0.05f,0.05f,0.12f,0.95f));
        Label("Header", cvGo.transform, V2(0,0.88f), V2(1,1), "CALIBRATION", 40, TextAlignmentOptions.Center, Color.white, font);

        var instrTmp = Label("InstructionText", cvGo.transform, V2(0.05f,0.46f), V2(0.95f,0.86f),
            "START를 누른 뒤,\n클릭 박자에 맞춰\nTAP 또는 Space를 누르세요.", 24, TextAlignmentOptions.Center, Color.white, font);
        var offTmp   = Label("OffsetText", cvGo.transform, V2(0.05f,0.36f), V2(0.95f,0.45f),
            "현재 보정값: 0 ms", 24, TextAlignmentOptions.Center, new Color(1f,0.85f,0.2f), font);

        var tapBtn   = MakeBtn("TapButton",   cvGo.transform, V2(0.15f,0.04f), V2(0.85f,0.20f), "TAP",   38, new Color(0.1f,0.4f,0.8f), font);
        var startBtn = MakeBtn("StartButton", cvGo.transform, V2(0.05f,0.22f), V2(0.47f,0.33f), "START", 24, new Color(0.1f,0.7f,0.2f), font);
        var backBtn  = MakeBtn("BackButton",  cvGo.transform, V2(0.53f,0.22f), V2(0.95f,0.33f), "BACK",  24, new Color(0.3f,0.3f,0.4f), font);

        var ui = cvGo.AddComponent<CalibrationController>();
        var so = new SerializedObject(ui);
        so.FindProperty("instructionText").objectReferenceValue = instrTmp;
        so.FindProperty("offsetText").objectReferenceValue      = offTmp;
        so.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(startBtn.onClick, ui.StartCalibration);
        UnityEventTools.AddPersistentListener(tapBtn.onClick,   ui.Tap);
        UnityEventTools.AddPersistentListener(backBtn.onClick,  ui.Back);

        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Calibration.unity");
        Debug.Log("Calibration.unity saved");
    }

    // ── Settings 씬 (단독 생성) ────────────────────────────────────
    [MenuItem("VRBeat/Create Settings Scene")]
    public static void CreateSettingsScene()
    {
        var font = TMP_Settings.defaultFontAsset;
        BuildSettingsScene(font);
        AddSceneToBuildSettings("Assets/_Project/Scenes/Settings.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateScenes] Settings.unity created.");
    }

    static void BuildSettingsScene(TMP_FontAsset font)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BasicLighting();

        Camera cam = null;
        var xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/XR Origin (XR Rig).prefab");
        if (xrPrefab != null)
        {
            var xrGo = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
            cam = xrGo.GetComponentInChildren<Camera>();
        }
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f,0.04f,0.1f);
            cam.nearClipPlane = 0.01f;
            camGo.transform.position = new Vector3(0,1.7f,0);
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        new GameObject("GameManager").AddComponent<GameManager>();

        var cvGo = new GameObject("SettingsCanvas");
        var cv = cvGo.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        cv.worldCamera = cam;
        cvGo.AddComponent<CanvasScaler>();
        cvGo.AddComponent<GraphicRaycaster>();
        cvGo.AddComponent<TrackedDeviceRaycaster>();
        RT(cvGo).sizeDelta = new Vector2(720, 820);
        cvGo.transform.position = new Vector3(0,1.5f,2.5f);
        cvGo.transform.localScale = Vector3.one * 0.002f;

        Img("Background", cvGo.transform, V2(0,0), V2(1,1), new Color(0.05f,0.05f,0.12f,0.95f));
        Label("Header", cvGo.transform, V2(0,0.92f), V2(1,1), "SETTINGS", 40, TextAlignmentOptions.Center, Color.white, font);

        var defaults = new GameSettings();
        Color lblCol = new Color(0.85f,0.85f,0.9f);

        // 각 행: 왼쪽 라벨 + 오른쪽 컨트롤
        void RowLabel(string text, float y0, float y1) =>
            Label(text + "Label", cvGo.transform, V2(0.05f,y0), V2(0.45f,y1), text, 22, TextAlignmentOptions.Left, lblCol, font);

        RowLabel("Note Speed",   0.82f, 0.89f);
        var noteSpeedSlider = MakeSlider("NoteSpeedSlider",  cvGo.transform, V2(0.48f,0.82f), V2(0.95f,0.89f), 5f, 20f, defaults.noteSpeed);
        RowLabel("Master Volume",0.73f, 0.80f);
        var masterSlider    = MakeSlider("MasterVolSlider",  cvGo.transform, V2(0.48f,0.73f), V2(0.95f,0.80f), 0f, 1f, defaults.masterVolume);
        RowLabel("Music Volume", 0.64f, 0.71f);
        var musicSlider     = MakeSlider("MusicVolSlider",   cvGo.transform, V2(0.48f,0.64f), V2(0.95f,0.71f), 0f, 1f, defaults.musicVolume);
        RowLabel("SFX Volume",   0.55f, 0.62f);
        var sfxSlider       = MakeSlider("SfxVolSlider",     cvGo.transform, V2(0.48f,0.55f), V2(0.95f,0.62f), 0f, 1f, defaults.sfxVolume);
        RowLabel("Left Handed",  0.46f, 0.53f);
        var leftToggle      = MakeToggle("LeftHandedToggle", cvGo.transform, V2(0.48f,0.46f), V2(0.95f,0.53f), defaults.leftHandedMode);

        // Offset 행: 라벨 + 값 + [-]/[+]
        RowLabel("Offset", 0.35f, 0.43f);
        var offTmp   = Label("OffsetText", cvGo.transform, V2(0.40f,0.35f), V2(0.66f,0.43f), "Offset: 0 ms", 20, TextAlignmentOptions.Center, new Color(1f,0.85f,0.2f), font);
        var minusBtn = MakeBtn("MinusButton", cvGo.transform, V2(0.68f,0.35f), V2(0.81f,0.43f), "-", 28, new Color(0.3f,0.3f,0.4f), font);
        var plusBtn  = MakeBtn("PlusButton",  cvGo.transform, V2(0.83f,0.35f), V2(0.96f,0.43f), "+", 28, new Color(0.3f,0.3f,0.4f), font);

        var saveBtn = MakeBtn("SaveButton", cvGo.transform, V2(0.05f,0.05f), V2(0.47f,0.18f), "SAVE", 26, new Color(0.1f,0.7f,0.2f), font);
        var backBtn = MakeBtn("BackButton", cvGo.transform, V2(0.53f,0.05f), V2(0.95f,0.18f), "BACK", 26, new Color(0.3f,0.3f,0.4f), font);

        var ui = cvGo.AddComponent<SettingsUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("noteSpeedSlider").objectReferenceValue    = noteSpeedSlider;
        so.FindProperty("leftHandedToggle").objectReferenceValue   = leftToggle;
        so.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
        so.FindProperty("musicVolumeSlider").objectReferenceValue  = musicSlider;
        so.FindProperty("sfxVolumeSlider").objectReferenceValue    = sfxSlider;
        so.FindProperty("offsetText").objectReferenceValue         = offTmp;
        so.ApplyModifiedProperties();

        UnityEventTools.AddPersistentListener(noteSpeedSlider.onValueChanged, ui.OnNoteSpeedChanged);
        UnityEventTools.AddPersistentListener(masterSlider.onValueChanged,    ui.OnMasterVolumeChanged);
        UnityEventTools.AddPersistentListener(musicSlider.onValueChanged,     ui.OnMusicVolumeChanged);
        UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged,       ui.OnSfxVolumeChanged);
        UnityEventTools.AddPersistentListener(leftToggle.onValueChanged,      ui.OnLeftHandedChanged);
        UnityEventTools.AddPersistentListener(minusBtn.onClick, ui.DecreaseOffset);
        UnityEventTools.AddPersistentListener(plusBtn.onClick,  ui.IncreaseOffset);
        UnityEventTools.AddPersistentListener(saveBtn.onClick,  ui.Save);
        UnityEventTools.AddPersistentListener(backBtn.onClick,  ui.Back);

        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Settings.unity");
        Debug.Log("Settings.unity saved");
    }

    // ── Step 4 ────────────────────────────────────────────────────
    static void BuildResultScene(TMP_FontAsset font)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BasicLighting();

        Camera cam = null;
        var xrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/XR Origin (XR Rig).prefab");
        if (xrPrefab != null)
        {
            var xrGo = (GameObject)PrefabUtility.InstantiatePrefab(xrPrefab);
            cam = xrGo.GetComponentInChildren<Camera>();
            Debug.Log("Result: XR Rig instantiated");
        }
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f,0.04f,0.1f);
            cam.nearClipPlane = 0.01f;
            camGo.transform.position = new Vector3(0,1.7f,0);
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        new GameObject("GameManager").AddComponent<GameManager>();

        var cvGo = new GameObject("ResultCanvas");
        var cv = cvGo.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        cv.worldCamera = cam;
        cvGo.AddComponent<CanvasScaler>();
        cvGo.AddComponent<GraphicRaycaster>();
        cvGo.AddComponent<TrackedDeviceRaycaster>();
        RT(cvGo).sizeDelta = new Vector2(600, 700);
        cvGo.transform.position = new Vector3(0,1.5f,2.5f);
        cvGo.transform.localScale = Vector3.one * 0.002f;

        Img("Background", cvGo.transform, V2(0,0), V2(1,1), new Color(0.05f,0.05f,0.12f,0.95f));
        Label("Header", cvGo.transform, V2(0,0.88f), V2(1,1), "RESULT", 45, TextAlignmentOptions.Center, Color.white, font);

        var rankTmp  = Label("RankText",      cvGo.transform, V2(0.25f,0.65f), V2(0.75f,0.88f), "S",            90, TextAlignmentOptions.Center, Color.yellow, font);
        rankTmp.fontStyle = FontStyles.Bold;
        var scoreTmp = Label("ScoreText",     cvGo.transform, V2(0.05f,0.52f), V2(0.95f,0.65f), "0",            42, TextAlignmentOptions.Center, Color.white, font);
        var accTmp   = Label("AccuracyText",  cvGo.transform, V2(0.05f,0.42f), V2(0.95f,0.52f), "100.0%",       28, TextAlignmentOptions.Center, Color.white, font);
        var comboTmp = Label("ComboText",     cvGo.transform, V2(0.05f,0.34f), V2(0.95f,0.42f), "Max Combo: 0", 22, TextAlignmentOptions.Center, new Color(0.8f,0.8f,0.8f), font);
        var newRTmp  = Label("NewRecordText", cvGo.transform, V2(0.1f,0.27f),  V2(0.9f,0.34f),  "NEW RECORD!",  22, TextAlignmentOptions.Center, new Color(1f,0.85f,0f), font);

        var fcGo = new GameObject("FullComboEffect"); fcGo.transform.SetParent(cvGo.transform, false);
        Anchors(fcGo, V2(0.1f,0.19f), V2(0.9f,0.27f));
        var fcTmp = fcGo.AddComponent<TextMeshProUGUI>();
        fcTmp.text = "FULL COMBO"; fcTmp.fontSize = 22;
        fcTmp.color = new Color(0.4f,1f,0.5f); fcTmp.alignment = TextAlignmentOptions.Center;
        if (font) fcTmp.font = font;

        var retryBtn = MakeBtn("RetryButton",   cvGo.transform, V2(0.05f,0.04f), V2(0.47f,0.16f), "RETRY",       26, new Color(0.2f,0.5f,1f),   font);
        var selBtn   = MakeBtn("SongSelectBtn", cvGo.transform, V2(0.53f,0.04f), V2(0.95f,0.16f), "SONG SELECT", 20, new Color(0.3f,0.3f,0.4f), font);

        // Wire ResultUI
        var ui = cvGo.AddComponent<ResultUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("scoreText").objectReferenceValue      = scoreTmp;
        so.FindProperty("accuracyText").objectReferenceValue   = accTmp;
        so.FindProperty("comboText").objectReferenceValue      = comboTmp;
        so.FindProperty("rankText").objectReferenceValue       = rankTmp;
        so.FindProperty("newRecordText").objectReferenceValue  = newRTmp;
        so.FindProperty("fullComboEffect").objectReferenceValue = fcGo;
        so.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(retryBtn.onClick, ui.Retry);
        UnityEventTools.AddPersistentListener(selBtn.onClick, ui.SongSelect);

        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Result.unity");
        Debug.Log("Result.unity saved");
    }

    // ── Step 5 ────────────────────────────────────────────────────
    static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/_Project/Scenes/SongSelect.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Gameplay.unity",   true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Result.unity",     true),
        };
        Debug.Log("Build Settings updated: SongSelect(0) Gameplay(1) Result(2)");
    }

    /// <summary>빌드 세팅에 씬을 추가(이미 있으면 무시).</summary>
    static void AddSceneToBuildSettings(string scenePath, bool enabled = true)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == scenePath))
        {
            Debug.Log("Build Settings already contains " + scenePath);
            return;
        }
        list.Add(new EditorBuildSettingsScene(scenePath, enabled));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("Build Settings += " + scenePath + " (index " + (list.Count - 1) + ")");
    }

    // ── Helpers ──────────────────────────────────────────────────
    static void BasicLighting()
    {
        RenderSettings.ambientLight = new Color(0.3f,0.3f,0.5f);
        var lg = new GameObject("Directional Light");
        var l = lg.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1f;
        lg.transform.rotation = Quaternion.Euler(50f,-30f,0f);
    }

    static RectTransform RT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }
    static Vector2 V2(float x, float y) => new Vector2(x, y);

    static void Anchors(GameObject go, Vector2 min, Vector2 max)
    { var rt = RT(go); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }

    static void Stretch(GameObject go) => Anchors(go, Vector2.zero, Vector2.one);

    static Image Img(string n, Transform p, Vector2 amin, Vector2 amax, Color col)
    { var go = new GameObject(n); go.transform.SetParent(p, false); Anchors(go, amin, amax);
      var img = go.AddComponent<Image>(); img.color = col; return img; }

    static TextMeshProUGUI Label(string n, Transform p, Vector2 amin, Vector2 amax,
                                  string text, float size, TextAlignmentOptions align, Color col, TMP_FontAsset font)
    { var go = new GameObject(n); go.transform.SetParent(p, false); Anchors(go, amin, amax);
      var tmp = go.AddComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size;
      tmp.alignment = align; tmp.color = col; if (font) tmp.font = font; return tmp; }

    static Slider MakeSlider(string n, Transform p, Vector2 amin, Vector2 amax, float min, float max, float val)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false); Anchors(go, amin, amax);
        var slider = go.AddComponent<Slider>();

        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        var bgrt = bg.AddComponent<RectTransform>();
        bgrt.anchorMin = V2(0,0.3f); bgrt.anchorMax = V2(1,0.7f); bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.2f,0.2f,0.25f);

        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(go.transform, false);
        var fart = fillArea.AddComponent<RectTransform>();
        fart.anchorMin = V2(0,0.3f); fart.anchorMax = V2(1,0.7f); fart.offsetMin = V2(5,0); fart.offsetMax = V2(-15,0);
        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillrt = fill.AddComponent<RectTransform>();
        fillrt.anchorMin = Vector2.zero; fillrt.anchorMax = V2(0,1); fillrt.sizeDelta = V2(10,0);
        fill.AddComponent<Image>().color = new Color(0.2f,0.6f,1f);

        var handleArea = new GameObject("Handle Slide Area"); handleArea.transform.SetParent(go.transform, false);
        var hart = handleArea.AddComponent<RectTransform>();
        hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one; hart.offsetMin = V2(10,0); hart.offsetMax = V2(-10,0);
        var handle = new GameObject("Handle"); handle.transform.SetParent(handleArea.transform, false);
        var hrt = handle.AddComponent<RectTransform>();
        hrt.sizeDelta = V2(20,0); hrt.anchorMin = V2(0,0); hrt.anchorMax = V2(0,1);
        var handleImg = handle.AddComponent<Image>(); handleImg.color = Color.white;

        slider.fillRect = fillrt; slider.handleRect = hrt; slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min; slider.maxValue = max; slider.value = val;
        return slider;
    }

    static Toggle MakeToggle(string n, Transform p, Vector2 amin, Vector2 amax, bool on)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false); Anchors(go, amin, amax);
        var toggle = go.AddComponent<Toggle>();
        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        var bgrt = bg.AddComponent<RectTransform>();
        bgrt.anchorMin = V2(0,0.5f); bgrt.anchorMax = V2(0,0.5f); bgrt.sizeDelta = V2(34,34); bgrt.anchoredPosition = V2(20,0);
        var bgImg = bg.AddComponent<Image>(); bgImg.color = new Color(0.2f,0.2f,0.25f);
        var check = new GameObject("Checkmark"); check.transform.SetParent(bg.transform, false); Stretch(check);
        var checkImg = check.AddComponent<Image>(); checkImg.color = new Color(0.2f,0.85f,0.35f);
        toggle.targetGraphic = bgImg; toggle.graphic = checkImg; toggle.isOn = on;
        return toggle;
    }

    static Button MakeBtn(string n, Transform p, Vector2 amin, Vector2 amax,
                            string text, float size, Color bgCol, TMP_FontAsset font)
    { var go = new GameObject(n); go.transform.SetParent(p, false); Anchors(go, amin, amax);
      var img = go.AddComponent<Image>(); img.color = bgCol;
      var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
      var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false); Stretch(lgo);
      var tmp = lgo.AddComponent<TextMeshProUGUI>(); tmp.text = text; tmp.fontSize = size;
      tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white; if (font) tmp.font = font;
      return btn; }
}
