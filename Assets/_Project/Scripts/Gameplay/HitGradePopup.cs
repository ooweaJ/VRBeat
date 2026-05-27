using UnityEngine;
using TMPro;

public class HitGradePopup : MonoBehaviour
{
    TextMeshPro label;
    float timer;
    Color baseColor;

    const float Duration = 1.1f;
    static readonly Vector3 Drift = new Vector3(0, 2.2f, 0);

    // Inspector 또는 코드에서 조정 가능한 설정값
    public static float  FontSize      = 3.0f;
    public static Color  ColorPerfect  = new Color(1f, 0.88f, 0f);
    public static Color  ColorGreat    = new Color(0f, 0.88f, 1f);
    public static Color  ColorGood     = new Color(0.8f, 0.8f, 0.8f);
    public static Color  ColorMiss     = new Color(1f, 0.22f, 0.22f);

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / Duration);

        transform.position += Drift * Time.deltaTime * (1f - t * t);

        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(
                transform.position - Camera.main.transform.position);

        if (label != null)
        {
            var c = baseColor;
            c.a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.3f, 1f, t));
            label.color = c;
        }

        if (timer >= Duration) Destroy(gameObject);
    }

    public static void Spawn(HitGrade grade, Vector3 worldPos)
    {
        Color col;
        string text;
        switch (grade)
        {
            case HitGrade.Perfect:
                text = "PERFECT";
                col  = ColorPerfect;
                break;
            case HitGrade.Great:
                text = "GREAT";
                col  = ColorGreat;
                break;
            default:
                text = "GOOD";
                col  = ColorGood;
                break;
        }
        SpawnInternal(text, col, worldPos);
    }

    public static void SpawnMiss(Vector3 worldPos)
    {
        SpawnInternal("MISS", ColorMiss, worldPos);
    }

    static void SpawnInternal(string text, Color col, Vector3 worldPos)
    {
        var go = new GameObject("GradePopup");
        go.transform.position = worldPos + Vector3.up * 0.15f;

        var tmp          = go.AddComponent<TextMeshPro>();
        tmp.text         = text;
        tmp.fontSize     = FontSize;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.fontStyle    = FontStyles.Bold;
        tmp.color        = col;
        tmp.sortingOrder = 100;

        var popup       = go.AddComponent<HitGradePopup>();
        popup.label     = tmp;
        popup.baseColor = col;
    }
}
