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

        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.1f);
        cam.nearClipPlane = 0.01f;
        camGo.transform.position = new Vector3(0, 1.7f, 0);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("SongLibrary").AddComponent<SongLibrary>();

        // World Space Canvas
        var cvGo = new GameObject("SongSelectCanvas");
        var cv = cvGo.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        cv.worldCamera = cam;
        cvGo.AddComponent<CanvasScaler>(); cvGo.AddComponent<GraphicRaycaster>();
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

    // ── Step 4 ────────────────────────────────────────────────────
    static void BuildResultScene(TMP_FontAsset font)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BasicLighting();

        var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f,0.04f,0.1f);
        cam.nearClipPlane = 0.01f;
        camGo.transform.position = new Vector3(0,1.7f,0);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();

        new GameObject("GameManager").AddComponent<GameManager>();

        var cvGo = new GameObject("ResultCanvas");
        var cv = cvGo.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace;
        cv.worldCamera = cam;
        cvGo.AddComponent<CanvasScaler>(); cvGo.AddComponent<GraphicRaycaster>();
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
