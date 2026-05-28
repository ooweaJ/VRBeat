using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 씬의 UI(Image/Button/Text/Slider/Toggle) 전체를 비트세이버 톤으로 자동 리스타일.
// 이름 기반 휴리스틱: 'Title' 큰 글자, 'Panel'/'Background' 패널, 'Button' 버튼, 그 외 기본 텍스트.
// 모든 버튼에 Outline 글로우 부착. 씬 시작 시 Awake에서 1회 적용.
[DefaultExecutionOrder(-100)]
public class MenuStyleApplier : MonoBehaviour
{
    [Header("Toggles")]
    public bool styleOnAwake   = true;
    public bool stylePanels    = true;
    public bool styleButtons   = true;
    public bool styleText      = true;
    public bool styleSliders   = true;
    public bool addButtonOutline = true;

    [Header("Manual Override")]
    public Image[]    forceTitleAccents;       // 강제로 cyan 강조
    public Button[]   forcePrimaryButtons;     // 강조 색 버튼 (Play 등)

    void Awake()
    {
        if (styleOnAwake) Apply();
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            ApplyToCanvas(canvas);

        if (forceTitleAccents != null)
            foreach (var img in forceTitleAccents) if (img != null) img.color = MenuPalette.AccentCyan;

        if (forcePrimaryButtons != null)
            foreach (var btn in forcePrimaryButtons) if (btn != null) StyleButton(btn, primary: true);
    }

    void ApplyToCanvas(Canvas canvas)
    {
        if (stylePanels)  StylePanels(canvas);
        if (styleButtons) StyleButtons(canvas);
        if (styleText)    StyleTexts(canvas);
        if (styleSliders) StyleSliders(canvas);
    }

    void StylePanels(Canvas canvas)
    {
        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img.GetComponent<Button>() != null) continue;        // 버튼은 따로 처리
            if (img.GetComponentInParent<Slider>() != null) continue; // 슬라이더 부품은 따로
            string n = img.gameObject.name.ToLower();

            if (n.Contains("background") || n.Contains("backdrop"))
                img.color = MenuPalette.Background;
            else if (n.Contains("deep") || n.Contains("dark"))
                img.color = MenuPalette.PanelDeep;
            else if (n.Contains("panel") || n.Contains("container") || n.Contains("box") || n.Contains("frame"))
                img.color = MenuPalette.PanelDark;
            else if (n.Contains("highlight") || n.Contains("accent"))
                img.color = MenuPalette.AccentCyan;
        }
    }

    void StyleButtons(Canvas canvas)
    {
        foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
            StyleButton(btn, primary: false);
    }

    void StyleButton(Button btn, bool primary)
    {
        var img = btn.GetComponent<Image>();
        if (img != null && !primary)
            img.color = MenuPalette.ButtonNormal;
        else if (img != null && primary)
            img.color = MenuPalette.AccentCyan * 0.4f;

        var cols = btn.colors;
        cols.normalColor       = primary ? Color.white : Color.white;
        cols.highlightedColor  = primary ? new Color(1.8f, 1.4f, 0.6f, 1f) : new Color(0.7f, 1.4f, 4f, 1f);
        cols.pressedColor      = primary ? new Color(3f, 1.2f, 0.5f, 1f)   : new Color(3.5f, 0.55f, 0.6f, 1f);
        cols.selectedColor     = primary ? new Color(1.6f, 1.2f, 0.5f, 1f) : new Color(0.5f, 1.2f, 3.5f, 1f);
        cols.disabledColor     = new Color(0.4f, 0.4f, 0.45f, 0.5f);
        cols.colorMultiplier   = 1f;
        btn.colors = cols;

        if (addButtonOutline)
        {
            var outline = btn.GetComponent<Outline>();
            if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
            outline.effectColor    = primary ? MenuPalette.AccentYellow : MenuPalette.OutlineCyan;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        // 버튼 내부 텍스트는 항상 밝게
        foreach (var tmp in btn.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.color     = primary ? new Color(0.10f, 0.05f, 0.0f, 1f) : MenuPalette.TextBright;
            tmp.fontStyle |= FontStyles.Bold;
        }
    }

    void StyleTexts(Canvas canvas)
    {
        foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            // 버튼 안 텍스트는 StyleButton에서 처리하므로 건너뜀
            if (tmp.GetComponentInParent<Button>() != null) continue;

            string n = tmp.gameObject.name.ToLower();
            bool isTitle = n.Contains("title") || n.Contains("header") || tmp.fontSize >= 60f;
            bool isLabel = n.Contains("label") || n.Contains("name");
            bool isScore = n.Contains("score") || n.Contains("rank") || n.Contains("record") || n.Contains("highscore");
            bool isHint  = n.Contains("hint") || n.Contains("offset") || n.Contains("description");

            if (isTitle)
            {
                tmp.color = MenuPalette.TitleColor;
                tmp.fontStyle |= FontStyles.Bold;
                AddTextGlow(tmp, MenuPalette.AccentCyan);
            }
            else if (isScore)
            {
                tmp.color = MenuPalette.AccentYellow;
                tmp.fontStyle |= FontStyles.Bold;
            }
            else if (isHint)
            {
                tmp.color = MenuPalette.TextDim;
            }
            else if (isLabel)
            {
                tmp.color = MenuPalette.TextBright;
            }
            else
            {
                tmp.color = MenuPalette.TextBright;
            }
        }
    }

    void AddTextGlow(TextMeshProUGUI tmp, Color glow)
    {
        // TMP에 글로우는 머티리얼 단위라 런타임에서 인스턴스 머티리얼 생성해서 underlay 켜기
        if (tmp.fontMaterial == null) return;
        var mat = tmp.fontMaterial; // instance
        if (mat.HasProperty(TMPro.ShaderUtilities.ID_UnderlayColor))
        {
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(TMPro.ShaderUtilities.ID_UnderlayColor, glow);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlayDilate, 0.4f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_UnderlaySoftness, 0.6f);
        }
    }

    void StyleSliders(Canvas canvas)
    {
        foreach (var slider in canvas.GetComponentsInChildren<Slider>(true))
        {
            if (slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null) fillImg.color = MenuPalette.AccentCyan * 0.5f;
            }
            if (slider.handleRect != null)
            {
                var handleImg = slider.handleRect.GetComponent<Image>();
                if (handleImg != null) handleImg.color = MenuPalette.AccentCyan;
            }
            // 배경(슬라이더 트랙)
            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = MenuPalette.PanelDark;
        }
    }
}
