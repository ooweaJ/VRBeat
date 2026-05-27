using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

public static class SetupBeatSaberHUD
{
    [MenuItem("VRBeat/Setup Beat Saber HUD")]
    public static void Run()
    {
        var hud = Object.FindFirstObjectByType<HUD>();
        if (hud == null)
        {
            var go = new GameObject("HUD");
            hud = go.AddComponent<HUD>();
        }
        var hudGo = hud.gameObject;

        // 이전에 남은 모든 자식 제거 (옛날 UI 잔재 포함)
        for (int i = hudGo.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(hudGo.transform.GetChild(i).gameObject);

        // 루트에 남아있는 Canvas / GraphicRaycaster 제거
        foreach (var comp in hudGo.GetComponents<UnityEngine.UI.Graphic>())
            Object.DestroyImmediate(comp);
        var oldGR = hudGo.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (oldGR != null) Object.DestroyImmediate(oldGR);

        // ── World Space Canvas ─────────────────────────────────
        var canvasGo = MakeGO("HUD_Canvas", hudGo.transform);
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        var cvr = canvasGo.GetComponent<RectTransform>();
        cvr.sizeDelta      = new Vector2(780f, 280f);
        cvr.localScale     = Vector3.one * 0.001f;
        cvr.localPosition  = Vector3.zero;
        cvr.localRotation  = Quaternion.identity;

        // ── 좌측 패널: 랭크 / COMBO / 점수 ──────────────────────
        var L = MakePanel("LeftPanel", canvasGo.transform, -292f, 0f, 170f, 258f);

        // 랭크 (큰 문자)
        var rankTxt = MakeTMP("RankText", L.transform, 0f, 80f, 150f, 70f, 52f, new Color(1f,0.88f,0f,1f));
        rankTxt.text      = "SS";
        rankTxt.fontStyle = FontStyles.Bold;

        // COMBO 라벨
        var comboLabel = MakeTMP("ComboLabel", L.transform, 0f, 28f, 150f, 26f, 13f,
            new Color(0.60f, 0.65f, 0.92f, 1f));
        comboLabel.text      = "COMBO";
        comboLabel.fontStyle = FontStyles.Bold;

        // 콤보 수
        var comboTxt = MakeTMP("ComboText", L.transform, 0f, -10f, 150f, 52f, 42f, Color.white);
        comboTxt.text      = "0";
        comboTxt.fontStyle = FontStyles.Bold;

        // 점수
        var scoreTxt = MakeTMP("ScoreText", L.transform, 0f, -52f, 150f, 30f, 18f,
            new Color(0.88f, 0.88f, 0.88f, 1f));
        scoreTxt.text = "0";

        // 체력 바
        var healthSlider = MakeSlider("HealthSlider", L.transform, 0f, -90f, 148f, 10f,
            new Color(0.12f, 0.12f, 0.18f, 1f), new Color(0.92f, 0.20f, 0.22f, 1f));
        healthSlider.value = 1f;

        // ── 우측 패널: 배율 / 진행 시간 ────────────────────────
        var R = MakePanel("RightPanel", canvasGo.transform, 292f, 0f, 170f, 258f);

        var multTxt = MakeTMP("MultiplierText", R.transform, 0f, 65f, 150f, 90f, 62f, Color.white);
        multTxt.text      = "×1";
        multTxt.fontStyle = FontStyles.Bold;

        var progSlider = MakeSlider("ProgressSlider", R.transform, 0f, -18f, 148f, 12f,
            new Color(0.12f, 0.12f, 0.18f, 1f), new Color(0.25f, 0.60f, 1.00f, 1f));
        progSlider.value = 0f;

        var progTimeTxt = MakeTMP("ProgressTimeText", R.transform, 0f, -50f, 150f, 24f, 12f,
            new Color(0.50f, 0.50f, 0.65f, 1f));
        progTimeTxt.text = "0:00 / 0:00";

        // ── HUD 필드 연결 ──────────────────────────────────────
        var so = new SerializedObject(hud);
        so.FindProperty("rankText").objectReferenceValue          = rankTxt;
        so.FindProperty("comboText").objectReferenceValue         = comboTxt;
        so.FindProperty("scoreText").objectReferenceValue         = scoreTxt;
        so.FindProperty("multiplierText").objectReferenceValue    = multTxt;
        so.FindProperty("progressSlider").objectReferenceValue    = progSlider;
        so.FindProperty("progressTimeText").objectReferenceValue  = progTimeTxt;
        so.FindProperty("healthSlider").objectReferenceValue      = healthSlider;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(hudGo);
        EditorSceneManager.MarkSceneDirty(hudGo.scene);
        Debug.Log("[VRBeat] Beat Saber HUD 재생성 완료 (랭크/콤보/점수 | 진행시간) — Ctrl+S로 저장하세요");
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject MakePanel(string name, Transform parent, float ax, float ay, float w, float h)
    {
        var go  = MakeGO(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = new Color(0.05f, 0.05f, 0.10f, 0.80f);
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(ax, ay);
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent,
        float ax, float ay, float w, float h, float fs, Color col)
    {
        var go  = MakeGO(name, parent);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize      = fs;
        txt.color         = col;
        txt.alignment     = TextAlignmentOptions.Center;
        txt.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(ax, ay);
        rt.sizeDelta        = new Vector2(w, h);
        return txt;
    }

    static Slider MakeSlider(string name, Transform parent,
        float ax, float ay, float w, float h, Color bgCol, Color fillCol)
    {
        var root    = MakeGO(name, parent);
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0,0,0,0);
        rootImg.raycastTarget = false;
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchoredPosition = new Vector2(ax, ay);
        rootRect.sizeDelta        = new Vector2(w, h);

        var bg     = MakeGO("Background", root.transform);
        var bgImg  = bg.AddComponent<Image>();
        bgImg.color = bgCol; bgImg.raycastTarget = false;
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        var fa     = MakeGO("Fill Area", root.transform);
        var faImg  = fa.AddComponent<Image>();
        faImg.color = new Color(0,0,0,0); faImg.raycastTarget = false;
        var faRect = fa.GetComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.offsetMin = new Vector2(2,0); faRect.offsetMax = new Vector2(-2,0);

        var fill    = MakeGO("Fill", fa.transform);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillCol; fillImg.raycastTarget = false;
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        var slider         = root.AddComponent<Slider>();
        slider.fillRect    = fillRect;
        slider.direction   = Slider.Direction.LeftToRight;
        slider.minValue    = 0f; slider.maxValue = 1f; slider.value = 1f;
        slider.interactable = false;
        slider.transition  = Selectable.Transition.None;
        return slider;
    }
}
