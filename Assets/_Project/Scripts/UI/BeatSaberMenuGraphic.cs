using UnityEngine;
using UnityEngine.UI;

public class BeatSaberMenuGraphic : MaskableGraphic
{
    public enum Shape
    {
        Panel,
        GlassPanel,
        Button,
        PrimaryButton,
        DangerButton,
        CoverFrame,
        SliderTrack,
        SliderFill,
        ToggleBox,
        Rail,
        SaberBlade,
    }

    [SerializeField] Shape shape = Shape.Panel;
    [SerializeField] Color fillColor = new Color(0.02f, 0.05f, 0.10f, 0.82f);
    [SerializeField] Color lineColor = new Color(0.25f, 0.80f, 2.80f, 0.85f);
    [SerializeField] Color accentColor = new Color(4.00f, 0.24f, 0.34f, 0.80f);
    [SerializeField] float cut = 18f;
    [SerializeField] float border = 2f;
    [SerializeField] bool rightLean = true;
    [SerializeField] int scanlines = 0;

    public void Configure(Shape nextShape, Color fill, Color line, Color accent,
                          float cutSize, float borderSize, bool leanRight, int scanlineCount)
    {
        shape = nextShape;
        fillColor = fill;
        lineColor = line;
        accentColor = accent;
        cut = cutSize;
        border = borderSize;
        rightLean = leanRight;
        scanlines = scanlineCount;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        Color tint = color;
        switch (shape)
        {
            case Shape.Button:
            case Shape.PrimaryButton:
            case Shape.DangerButton:
                DrawButton(vh, r, tint);
                break;
            case Shape.CoverFrame:
                DrawPanel(vh, r, tint, true);
                DrawInnerVoid(vh, Inset(r, border * 3f), new Color(0f, 0f, 0f, 0.20f) * tint);
                break;
            case Shape.SliderTrack:
                DrawButton(vh, r, tint, thin: true);
                break;
            case Shape.SliderFill:
                DrawRail(vh, r, fillColor * tint, lineColor * tint);
                break;
            case Shape.ToggleBox:
                DrawPanel(vh, r, tint, true);
                break;
            case Shape.Rail:
                DrawRail(vh, r, fillColor * tint, lineColor * tint);
                break;
            case Shape.SaberBlade:
                DrawSaberBlade(vh, r, tint);
                break;
            default:
                DrawPanel(vh, r, tint, shape == Shape.GlassPanel);
                break;
        }
    }

    void DrawPanel(VertexHelper vh, Rect r, Color tint, bool glass)
    {
        float c = Mathf.Min(cut, r.width * 0.18f, r.height * 0.35f);
        Vector2[] pts =
        {
            new(r.xMin + c, r.yMax),
            new(r.xMax - c, r.yMax),
            new(r.xMax, r.yMax - c),
            new(r.xMax, r.yMin + c),
            new(r.xMax - c, r.yMin),
            new(r.xMin + c, r.yMin),
            new(r.xMin, r.yMin + c),
            new(r.xMin, r.yMax - c),
        };

        AddPolygon(vh, pts, fillColor * tint);

        DrawOutline(vh, pts, lineColor * tint, border);
    }

    void DrawButton(VertexHelper vh, Rect r, Color tint, bool thin = false)
    {
        float c = Mathf.Min(cut, r.width * 0.22f, r.height * 0.50f);
        Vector2[] pts;
        if (rightLean)
        {
            pts = new[]
            {
                new Vector2(r.xMin + c, r.yMax),
                new Vector2(r.xMax, r.yMax),
                new Vector2(r.xMax - c, r.yMin),
                new Vector2(r.xMin, r.yMin),
            };
        }
        else
        {
            pts = new[]
            {
                new Vector2(r.xMin, r.yMax),
                new Vector2(r.xMax - c, r.yMax),
                new Vector2(r.xMax, r.yMin),
                new Vector2(r.xMin + c, r.yMin),
            };
        }

        AddPolygon(vh, pts, fillColor * tint);
        DrawOutline(vh, pts, lineColor * tint, border);
    }

    void DrawRail(VertexHelper vh, Rect r, Color fill, Color line)
    {
        AddQuad(vh, r, fill);
        float t = Mathf.Max(border, Mathf.Min(r.width, r.height) * 0.18f);
        AddQuad(vh, new Rect(r.xMin, r.yMax - t, r.width, t), line);
        AddQuad(vh, new Rect(r.xMin, r.yMin, r.width, t), line * 0.55f);
    }

    void DrawSaberBlade(VertexHelper vh, Rect r, Color tint)
    {
        float cx = (r.xMin + r.xMax) * 0.5f;
        float handleH = Mathf.Max(28f, r.height * 0.16f);
        float y0 = r.yMin + handleH * 0.70f;
        float y1 = r.yMax - Mathf.Max(10f, r.height * 0.025f);
        float w = Mathf.Max(10f, r.width);

        Color blade = lineColor * tint;
        Color glow = fillColor * tint;
        Color whiteCore = new Color(1.0f, 1.0f, 1.0f, 0.88f) * tint;

        AddBladeBeam(vh, cx, y0, y1, w * 0.96f, w * 0.30f, WithAlpha(glow, 0.12f));
        AddBladeBeam(vh, cx, y0 + handleH * 0.05f, y1, w * 0.70f, w * 0.21f, WithAlpha(glow, 0.22f));
        AddBladeBeam(vh, cx, y0 + handleH * 0.12f, y1, w * 0.42f, w * 0.12f, WithAlpha(blade, 0.70f));
        AddBladeBeam(vh, cx, y0 + handleH * 0.20f, y1 - handleH * 0.06f, w * 0.16f, w * 0.045f, whiteCore);

        float guardY = r.yMin + handleH * 0.47f;
        AddSlantedBar(vh, new Rect(cx - w * 0.42f, guardY, w * 0.84f, handleH * 0.12f),
            WithAlpha(whiteCore, 0.45f), rightLean);
        AddSlantedBar(vh, new Rect(cx - w * 0.34f, r.yMin + handleH * 0.08f, w * 0.68f, handleH * 0.38f),
            new Color(0.015f, 0.018f, 0.030f, 0.92f) * tint, rightLean);
        AddSlantedBar(vh, new Rect(cx - w * 0.24f, r.yMin + handleH * 0.16f, w * 0.48f, handleH * 0.08f),
            new Color(0.42f, 0.52f, 0.60f, 0.48f) * tint, !rightLean);
        AddSlantedBar(vh, new Rect(cx - w * 0.24f, r.yMin + handleH * 0.31f, w * 0.48f, handleH * 0.08f),
            new Color(0.12f, 0.17f, 0.22f, 0.82f) * tint, !rightLean);
    }

    void DrawScanlines(VertexHelper vh, Rect r, Color tint)
    {
        int lines = Mathf.Clamp(scanlines, 0, 18);
        if (lines == 0) return;

        float step = r.height / (lines + 1);
        for (int i = 1; i <= lines; i++)
        {
            float y = r.yMin + i * step;
            AddQuad(vh, new Rect(r.xMin, y, r.width, Mathf.Max(1f, border * 0.45f)),
                    new Color(0.45f, 0.85f, 1.0f, 0.055f) * tint);
        }
    }

    void DrawInnerVoid(VertexHelper vh, Rect r, Color c)
    {
        AddQuad(vh, r, c);
    }

    void DrawOutline(VertexHelper vh, Vector2[] pts, Color c, float thickness)
    {
        float t = Mathf.Max(1f, thickness);
        for (int i = 0; i < pts.Length; i++)
            AddLine(vh, pts[i], pts[(i + 1) % pts.Length], t, c);
    }

    static Rect Inset(Rect r, float amount)
    {
        return new Rect(r.xMin + amount, r.yMin + amount,
                        Mathf.Max(0f, r.width - amount * 2f),
                        Mathf.Max(0f, r.height - amount * 2f));
    }

    static void AddPolygon(VertexHelper vh, Vector2[] pts, Color c)
    {
        int start = vh.currentVertCount;
        for (int i = 0; i < pts.Length; i++)
            AddVert(vh, pts[i], c);

        for (int i = 1; i < pts.Length - 1; i++)
            vh.AddTriangle(start, start + i, start + i + 1);
    }

    static void AddQuad(VertexHelper vh, Rect r, Color c)
    {
        int start = vh.currentVertCount;
        AddVert(vh, new Vector2(r.xMin, r.yMin), c);
        AddVert(vh, new Vector2(r.xMin, r.yMax), c);
        AddVert(vh, new Vector2(r.xMax, r.yMax), c);
        AddVert(vh, new Vector2(r.xMax, r.yMin), c);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    static void AddBladeBeam(VertexHelper vh, float cx, float y0, float y1,
                             float baseWidth, float tipWidth, Color c)
    {
        float tipH = Mathf.Max(8f, (y1 - y0) * 0.055f);
        Vector2[] pts =
        {
            new(cx - baseWidth * 0.5f, y0),
            new(cx + baseWidth * 0.5f, y0),
            new(cx + tipWidth * 0.5f, y1 - tipH),
            new(cx, y1),
            new(cx - tipWidth * 0.5f, y1 - tipH),
        };
        AddPolygon(vh, pts, c);
    }

    static void AddSlantedBar(VertexHelper vh, Rect r, Color c, bool leanRight)
    {
        float slant = Mathf.Min(r.width * 0.18f, r.height * 0.75f);
        Vector2[] pts = leanRight
            ? new[]
            {
                new Vector2(r.xMin + slant, r.yMax),
                new Vector2(r.xMax, r.yMax),
                new Vector2(r.xMax - slant, r.yMin),
                new Vector2(r.xMin, r.yMin),
            }
            : new[]
            {
                new Vector2(r.xMin, r.yMax),
                new Vector2(r.xMax - slant, r.yMax),
                new Vector2(r.xMax, r.yMin),
                new Vector2(r.xMin + slant, r.yMin),
            };
        AddPolygon(vh, pts, c);
    }

    static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color c)
    {
        Vector2 dir = (b - a).normalized;
        if (dir.sqrMagnitude < 0.001f) return;
        Vector2 n = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

        int start = vh.currentVertCount;
        AddVert(vh, a - n, c);
        AddVert(vh, a + n, c);
        AddVert(vh, b + n, c);
        AddVert(vh, b - n, c);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    static void AddVert(VertexHelper vh, Vector2 p, Color c)
    {
        UIVertex v = UIVertex.simpleVert;
        v.position = p;
        v.color = c;
        vh.AddVert(v);
    }
}
