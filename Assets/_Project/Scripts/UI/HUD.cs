using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Left Panel — Combo / Score / Rank")]
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI scoreText;

    [Header("Right Panel — Progress")]
    [SerializeField] TextMeshProUGUI multiplierText;
    [SerializeField] Slider          progressSlider;
    [SerializeField] TextMeshProUGUI progressTimeText;

    [Header("Health")]
    [SerializeField] Slider healthSlider;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // 월드 고정 — 카메라 추적 없음
    }

    void Update()
    {
        RefreshMultiplier();
        RefreshProgress();
        RefreshRank();
    }

    void RefreshMultiplier()
    {
        if (multiplierText == null || ScoreManager.Instance == null) return;
        int combo = ScoreManager.Instance.Combo;
        int mult  = combo >= 32 ? 8 : combo >= 16 ? 4 : combo >= 8 ? 2 : 1;
        multiplierText.text = $"×{mult}";
    }

    void RefreshProgress()
    {
        if (Conductor.Instance == null) return;
        float total   = Conductor.Instance.SongDuration;
        float current = Mathf.Max(0f, (float)Conductor.Instance.SongTime);
        if (total <= 0f) return;

        if (progressSlider != null)
            progressSlider.value = Mathf.Clamp01(current / total);
        if (progressTimeText != null)
            progressTimeText.text = $"{FormatTime(current)} / {FormatTime(total)}";
    }

    void RefreshRank()
    {
        if (rankText == null || ScoreManager.Instance == null) return;
        string rank = AccuracyCalc.GetRank(ScoreManager.Instance.Accuracy);
        rankText.text = rank;
        rankText.color = rank switch
        {
            "SS" => new Color(1f, 0.88f, 0f),
            "S"  => new Color(1f, 0.60f, 0f),
            "A"  => new Color(0.35f, 1f, 0.35f),
            "B"  => new Color(0.3f, 0.8f, 1f),
            "C"  => Color.white,
            _    => new Color(0.5f, 0.5f, 0.5f),
        };
    }

    public void OnHit(HitGrade grade, int score, int combo)
    {
        UpdateScore(score);
        if (comboText != null) comboText.text = combo > 1 ? combo.ToString() : "";
    }

    public void OnMiss(int score, int combo)
    {
        UpdateScore(score);
        if (comboText != null) comboText.text = "";
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }

    public void UpdateHealth(float normalized)
    {
        if (healthSlider != null) healthSlider.value = normalized;
    }

    static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"{m}:{s:00}";
    }
}
