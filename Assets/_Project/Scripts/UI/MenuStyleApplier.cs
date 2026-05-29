using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class MenuStyleApplier : MonoBehaviour
{
    const string ChromeRootName = "_BeatSaberMenuChrome";

    [Header("Toggles")]
    public bool styleOnAwake = true;
    public bool normalizeCanvasPose = true;
    public bool addMenuChrome = true;

    void Awake()
    {
        if (styleOnAwake) Apply();
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            ApplyCanvas(canvas);
    }

    void ApplyCanvas(Canvas canvas)
    {
        if (canvas == null) return;

        RemoveLegacyStyleObjects(canvas);
        if (normalizeCanvasPose) NormalizeCanvas(canvas);
        if (addMenuChrome) BuildChrome(canvas);

        StylePanels(canvas);
        StyleButtons(canvas);
        StyleSliders(canvas);
        StyleToggles(canvas);
        StyleText(canvas);
    }

    void NormalizeCanvas(Canvas canvas)
    {
        if (canvas.renderMode != RenderMode.WorldSpace) return;
        var rt = canvas.GetComponent<RectTransform>();
        if (rt == null) return;

        if (canvas.worldCamera == null)
            canvas.worldCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        float height = Mathf.Max(1f, rt.rect.height);
        float targetHeight = height >= 760f ? 1.32f : 1.24f;
        canvas.transform.localScale = Vector3.one * (targetHeight / height);
        canvas.transform.position = new Vector3(0f, 1.42f, 2.75f);
        canvas.transform.rotation = Quaternion.identity;
    }

    void BuildChrome(Canvas canvas)
    {
        var root = EnsureRect(canvas.transform, ChromeRootName);
        ClearChildren(root);
        Stretch(root);
        root.SetAsFirstSibling();

        AddShape(root, "BackdropPlate", BeatSaberMenuGraphic.Shape.GlassPanel,
            new Color(0.004f, 0.010f, 0.020f, 0.54f), MenuPalette.OutlineDim,
            MenuPalette.AccentRed, 30f, 1.5f, true, 4,
            Vector2.zero, Vector2.one, new Vector2(18f, 12f), new Vector2(-18f, -12f));

        AddShape(root, "LeftSaberBlade", BeatSaberMenuGraphic.Shape.SaberBlade,
            MenuPalette.AccentRed * 0.28f, MenuPalette.AccentRed * 0.92f,
            MenuPalette.AccentCyan, 0f, 2f, false, 0,
            new Vector2(-0.125f, 0.030f), new Vector2(0.015f, 0.925f), Vector2.zero, Vector2.zero, -12f);

        AddShape(root, "RightSaberBlade", BeatSaberMenuGraphic.Shape.SaberBlade,
            MenuPalette.AccentCyan * 0.24f, MenuPalette.AccentCyan * 0.90f,
            MenuPalette.AccentRed, 0f, 2f, true, 0,
            new Vector2(0.985f, 0.030f), new Vector2(1.125f, 0.925f), Vector2.zero, Vector2.zero, 12f);
    }

    void StylePanels(Canvas canvas)
    {
        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (IsChrome(img.transform)) continue;
            if (img.GetComponent<Button>() != null) continue;
            if (img.GetComponentInParent<Button>() != null) continue;
            if (img.GetComponentInParent<Slider>() != null) continue;
            if (img.GetComponentInParent<Toggle>() != null) continue;

            string n = img.name.ToLowerInvariant();
            if (n.Contains("viewport"))
            {
                img.color = new Color(0f, 0f, 0f, 0.01f);
                continue;
            }

            BeatSaberMenuGraphic.Shape shape = n.Contains("cover")
                ? BeatSaberMenuGraphic.Shape.CoverFrame
                : n.Contains("background")
                    ? BeatSaberMenuGraphic.Shape.GlassPanel
                    : BeatSaberMenuGraphic.Shape.Panel;

            Color fill = n.Contains("cover")
                ? new Color(0.012f, 0.035f, 0.050f, 0.94f)
                : n.Contains("background")
                    ? new Color(0.004f, 0.012f, 0.026f, 0.78f)
                    : new Color(0.006f, 0.026f, 0.050f, 0.92f);

            var graphic = EnsureGraphic(img.gameObject, shape, fill, MenuPalette.OutlineCyan,
                MenuPalette.AccentRed, n.Contains("cover") ? 18f : 24f, 2.0f, true, n.Contains("panel") ? 7 : 4);
            graphic.raycastTarget = false;
            DisableImage(img);
        }
    }

    void StyleButtons(Canvas canvas)
    {
        foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
            StyleButton(btn);
    }

    void StyleButton(Button btn)
    {
        bool primary = IsPrimary(btn.name);
        bool danger = IsBack(btn.name);
        var shape = primary ? BeatSaberMenuGraphic.Shape.PrimaryButton :
                    danger ? BeatSaberMenuGraphic.Shape.DangerButton :
                    BeatSaberMenuGraphic.Shape.Button;

        Color fill = primary ? new Color(0.018f, 0.180f, 0.390f, 0.98f) :
                     danger ? new Color(0.120f, 0.015f, 0.030f, 0.96f) :
                     new Color(0.018f, 0.045f, 0.080f, 0.96f);

        Color line = primary ? MenuPalette.AccentCyan :
                     danger ? MenuPalette.AccentRed :
                     new Color(0.22f, 0.70f, 2.40f, 0.85f);

        var graphic = EnsureGraphic(btn.gameObject, shape, fill, line, MenuPalette.AccentRed,
            primary ? 22f : 15f, primary ? 2.6f : 2.0f, true, 0);
        graphic.raycastTarget = true;
        btn.targetGraphic = graphic;
        btn.transition = Selectable.Transition.ColorTint;

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary ? new Color(1.3f, 1.7f, 2.4f, 1f) : new Color(1.0f, 1.25f, 1.8f, 1f);
        colors.pressedColor = danger ? new Color(2.0f, 0.55f, 0.65f, 1f) : new Color(1.5f, 0.8f, 0.6f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.40f, 0.55f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        foreach (var img in btn.GetComponents<Image>())
            if (img != null) DisableImage(img);

        foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.color = primary ? Color.white : MenuPalette.TextBright;
            tmp.fontStyle |= FontStyles.Bold;
            tmp.characterSpacing = 0f;
            tmp.raycastTarget = false;
            EnsureOutline(tmp.gameObject, primary ? MenuPalette.OutlineCyan : MenuPalette.OutlineDim, new Vector2(1f, -1f));
        }
    }

    void StyleSliders(Canvas canvas)
    {
        foreach (var slider in canvas.GetComponentsInChildren<Slider>(true))
        {
            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                EnsureGraphic(bg.gameObject, BeatSaberMenuGraphic.Shape.SliderTrack,
                    new Color(0.015f, 0.030f, 0.060f, 0.92f), MenuPalette.OutlineDim,
                    MenuPalette.AccentRed, 8f, 1.5f, true, 0);
                DisableImage(bg);
            }

            if (slider.fillRect != null)
            {
                var fill = EnsureGraphic(slider.fillRect.gameObject, BeatSaberMenuGraphic.Shape.SliderFill,
                    MenuPalette.AccentCyan * 0.22f, MenuPalette.AccentCyan * 0.62f,
                    MenuPalette.AccentCyan, 2f, 1.5f, true, 0);
                fill.raycastTarget = false;
                var img = slider.fillRect.GetComponent<Image>();
                if (img != null) DisableImage(img);
            }

            if (slider.handleRect != null)
            {
                slider.handleRect.sizeDelta = new Vector2(18f, slider.handleRect.sizeDelta.y);
                var handle = EnsureGraphic(slider.handleRect.gameObject, BeatSaberMenuGraphic.Shape.PrimaryButton,
                    MenuPalette.AccentYellow * 0.28f, MenuPalette.AccentYellow * 0.75f,
                    MenuPalette.AccentCyan, 4f, 1.5f, true, 0);
                handle.raycastTarget = true;
                var img = slider.handleRect.GetComponent<Image>();
                if (img != null) DisableImage(img);
                slider.targetGraphic = handle;
            }
        }
    }

    void StyleToggles(Canvas canvas)
    {
        foreach (var toggle in canvas.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle.targetGraphic != null)
            {
                var bg = EnsureGraphic(toggle.targetGraphic.gameObject, BeatSaberMenuGraphic.Shape.ToggleBox,
                    new Color(0.015f, 0.035f, 0.070f, 0.92f), MenuPalette.OutlineCyan,
                    MenuPalette.AccentRed, 8f, 1.8f, true, 2);
                bg.raycastTarget = true;
                toggle.targetGraphic = bg;
                var img = bg.GetComponent<Image>();
                if (img != null) DisableImage(img);
            }

            if (toggle.graphic != null)
            {
                var check = EnsureGraphic(toggle.graphic.gameObject, BeatSaberMenuGraphic.Shape.Rail,
                    MenuPalette.AccentCyan * 0.32f, MenuPalette.AccentCyan,
                    MenuPalette.AccentYellow, 0f, 1.0f, true, 0);
                check.raycastTarget = false;
                toggle.graphic = check;
                var img = check.GetComponent<Image>();
                if (img != null) DisableImage(img);
            }
        }
    }

    void StyleText(Canvas canvas)
    {
        foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (IsChrome(tmp.transform)) continue;
            tmp.characterSpacing = 0f;
            tmp.raycastTarget = false;

            var ownerButton = tmp.GetComponentInParent<Button>();
            if (ownerButton != null)
            {
                bool primaryButton = IsPrimary(ownerButton.name);
                tmp.color = primaryButton ? Color.white : new Color(0.76f, 0.92f, 1.0f, 1f);
                tmp.fontStyle |= FontStyles.Bold;
                EnsureOutline(tmp.gameObject, primaryButton ? MenuPalette.OutlineCyan : MenuPalette.OutlineDim, new Vector2(1f, -1f));
                continue;
            }

            string n = tmp.name.ToLowerInvariant();
            bool title = n.Contains("header") || n.Contains("title") || tmp.fontSize >= 58f;
            bool score = n.Contains("score") || n.Contains("rank") || n.Contains("record") ||
                         n.Contains("combo") || n.Contains("accuracy");
            bool muted = n.Contains("hint") || n.Contains("offset") || n.Contains("instruction");

            if (title)
            {
                tmp.color = MenuPalette.TitleColor;
                tmp.fontStyle |= FontStyles.Bold;
                EnsureOutline(tmp.gameObject, MenuPalette.OutlineCyan, new Vector2(2f, -2f));
            }
            else if (score)
            {
                tmp.color = MenuPalette.AccentYellow;
                tmp.fontStyle |= FontStyles.Bold;
            }
            else if (muted)
            {
                tmp.color = MenuPalette.TextDim;
            }
            else if (tmp.text.Contains("빨간") || tmp.text.ToLowerInvariant().Contains("red"))
            {
                tmp.color = new Color(1f, 0.34f, 0.42f, 1f);
                tmp.fontStyle |= FontStyles.Bold;
            }
            else if (tmp.text.Contains("파란") || tmp.text.ToLowerInvariant().Contains("blue"))
            {
                tmp.color = new Color(0.45f, 0.78f, 1f, 1f);
                tmp.fontStyle |= FontStyles.Bold;
            }
            else
            {
                tmp.color = MenuPalette.TextBright;
            }
        }
    }

    public static void StyleSelectableGraphic(GameObject go, bool selected)
    {
        if (go == null) return;

        var shape = selected ? BeatSaberMenuGraphic.Shape.PrimaryButton : BeatSaberMenuGraphic.Shape.Button;
        Color fill = selected ? new Color(0.640f, 0.380f, 0.040f, 0.98f)
                              : new Color(0.018f, 0.045f, 0.080f, 0.96f);
        Color line = selected ? MenuPalette.AccentYellow : new Color(0.22f, 0.70f, 2.40f, 0.85f);
        var graphic = EnsureGraphic(go, shape, fill, line, MenuPalette.AccentRed, selected ? 18f : 14f, 2.0f, true, 0);
        graphic.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        if (btn != null) btn.targetGraphic = graphic;

        var img = go.GetComponent<Image>();
        if (img != null) DisableImage(img);
    }

    static BeatSaberMenuGraphic EnsureGraphic(GameObject go, BeatSaberMenuGraphic.Shape shape,
        Color fill, Color line, Color accent, float cut, float border, bool leanRight, int scanlines)
    {
        var graphic = go.GetComponent<BeatSaberMenuGraphic>();
        if (graphic == null)
        {
            var host = EnsureRect(go.transform, "_BeatSaberGraphic");
            Stretch(host);
            host.SetAsFirstSibling();
            if (host.GetComponent<CanvasRenderer>() == null)
                host.gameObject.AddComponent<CanvasRenderer>();
            graphic = host.GetComponent<BeatSaberMenuGraphic>();
            if (graphic == null) graphic = host.gameObject.AddComponent<BeatSaberMenuGraphic>();

            foreach (var img in go.GetComponents<Image>())
                if (img != null) DisableImage(img);
        }
        else if (graphic.GetComponent<CanvasRenderer>() == null)
        {
            graphic.gameObject.AddComponent<CanvasRenderer>();
        }

        graphic.color = Color.white;
        graphic.transform.SetAsFirstSibling();
        graphic.Configure(shape, fill, line, accent, cut, border, leanRight, scanlines);
        return graphic;
    }

    static void AddShape(RectTransform parent, string name, BeatSaberMenuGraphic.Shape shape,
        Color fill, Color line, Color accent, float cut, float border, bool leanRight, int scanlines,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, float rotation = 0f)
    {
        var rt = EnsureRect(parent, name);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localEulerAngles = new Vector3(0f, 0f, rotation);
        var graphic = EnsureGraphic(rt.gameObject, shape, fill, line, accent, cut, border, leanRight, scanlines);
        graphic.raycastTarget = false;
    }

    static RectTransform EnsureRect(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            var rt = existing.GetComponent<RectTransform>();
            return rt != null ? rt : existing.gameObject.AddComponent<RectTransform>();
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localEulerAngles = Vector3.zero;
    }

    static void ClearChildren(RectTransform rt)
    {
        for (int i = rt.childCount - 1; i >= 0; i--)
        {
            var child = rt.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    static void RemoveLegacyStyleObjects(Canvas canvas)
    {
        var transforms = canvas.GetComponentsInChildren<Transform>(true);
        for (int i = transforms.Length - 1; i >= 0; i--)
        {
            var t = transforms[i];
            if (t == null || t == canvas.transform) continue;
            if (t.name != "LeftEdge" && t.name != "RightEdge") continue;

            var go = t.gameObject;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    static bool IsChrome(Transform t)
    {
        while (t != null)
        {
            if (t.name == ChromeRootName) return true;
            t = t.parent;
        }
        return false;
    }

    static bool IsPrimary(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("play") || n.Contains("start") || n.Contains("tap") ||
               n.Contains("save") || n.Contains("retry") || n.Contains("confirm");
    }

    static bool IsBack(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("back") || n.Contains("cancel") || n.Contains("songselect");
    }

    static void DisableImage(Image img)
    {
        img.enabled = false;
        img.raycastTarget = false;
    }

    static void EnsureOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }
}
