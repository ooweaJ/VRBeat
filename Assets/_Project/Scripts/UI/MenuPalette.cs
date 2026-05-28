using UnityEngine;

// 비트세이버식 네온 UI 컬러 팔레트 (HDR).
public static class MenuPalette
{
    // 배경/패널 — 어두운 코발트
    public static readonly Color Background     = new Color(0.020f, 0.025f, 0.040f, 1f);
    public static readonly Color PanelDark      = new Color(0.040f, 0.055f, 0.090f, 0.92f);
    public static readonly Color PanelMid       = new Color(0.080f, 0.110f, 0.180f, 0.92f);
    public static readonly Color PanelDeep      = new Color(0.015f, 0.020f, 0.035f, 0.95f);

    // 강조 — HDR
    public static readonly Color AccentCyan     = new Color(0.30f, 1.60f, 6.80f, 1f);
    public static readonly Color AccentRed      = new Color(6.50f, 0.30f, 0.55f, 1f);
    public static readonly Color AccentYellow   = new Color(6.20f, 4.40f, 0.30f, 1f);
    public static readonly Color AccentMagenta  = new Color(5.20f, 0.55f, 4.80f, 1f);

    // 버튼 상태
    public static readonly Color ButtonNormal      = new Color(0.10f, 0.14f, 0.22f, 1f);
    public static readonly Color ButtonHover       = new Color(0.50f, 1.10f, 4.20f, 1f);  // HDR cyan glow
    public static readonly Color ButtonPressed     = new Color(4.20f, 0.45f, 0.50f, 1f);  // HDR red glow
    public static readonly Color ButtonSelected    = new Color(0.40f, 0.95f, 3.50f, 1f);
    public static readonly Color ButtonDisabled    = new Color(0.06f, 0.08f, 0.12f, 0.5f);

    // 텍스트
    public static readonly Color TitleColor     = new Color(0.60f, 1.80f, 6.80f, 1f);  // HDR cyan
    public static readonly Color TextBright     = new Color(1.30f, 1.45f, 1.60f, 1f);
    public static readonly Color TextDim        = new Color(0.55f, 0.60f, 0.72f, 1f);
    public static readonly Color TextAccentRed  = new Color(5.50f, 0.40f, 0.55f, 1f);

    // 아웃라인/글로우
    public static readonly Color OutlineCyan    = new Color(0.50f, 1.50f, 5.0f, 0.85f);
    public static readonly Color OutlineRed     = new Color(4.0f, 0.45f, 0.55f, 0.85f);
}
