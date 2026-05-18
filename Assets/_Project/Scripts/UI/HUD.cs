using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI gradeText;
    [SerializeField] Slider          healthSlider;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
