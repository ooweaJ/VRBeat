using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI gradeText;
    [SerializeField] Slider          healthSlider;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 카메라 자식으로 붙이기 — 항상 시야에 고정
        var cam = Camera.main?.transform;
        if (cam != null)
        {
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0f, 0f, 0.5f); // 0.5m 앞
            transform.localRotation = Quaternion.identity;
        }
    }

    public void OnHit(HitGrade grade, int score, int combo)
    {
        UpdateScore(score);
        if (comboText != null) comboText.text = combo > 1 ? $"x{combo}" : "";
        if (gradeText  != null)
        {
            gradeText.text  = grade.ToString().ToUpper();
            gradeText.color = grade == HitGrade.Perfect ? Color.yellow :
                              grade == HitGrade.Great   ? Color.cyan   : Color.white;
        }
    }

    public void OnMiss(int score, int combo)
    {
        UpdateScore(score);
        if (comboText != null) comboText.text = "";
        if (gradeText  != null) { gradeText.text = "MISS"; gradeText.color = Color.red; }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }

    public void UpdateHealth(float normalized)
    {
        if (healthSlider != null) healthSlider.value = normalized;
    }
}
