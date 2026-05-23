using TMPro;
using UnityEngine;

public class ResultUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI accuracyText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI rankText;
    [SerializeField] TextMeshProUGUI newRecordText;
    [SerializeField] GameObject      fullComboEffect;

    void Start()
    {
        // 점수는 GameManager가 Gameplay→Result 전환 직전에 스냅샷으로 담아 둔다.
        // (ScoreManager는 씬 단위 싱글톤이라 Result 씬에는 존재하지 않음)
        var result = GameManager.Instance?.LastResult;
        if (result == null) return;

        string rank = AccuracyCalc.GetRank(result.accuracy);

        if (scoreText    != null) scoreText.text    = result.score.ToString("N0");
        if (accuracyText != null) accuracyText.text = $"{result.accuracy * 100f:F1}%";
        if (comboText    != null) comboText.text    = $"Max Combo: {result.maxCombo}";
        if (rankText     != null) rankText.text     = rank;

        if (fullComboEffect != null) fullComboEffect.SetActive(result.fullCombo);
        if (newRecordText   != null) newRecordText.gameObject.SetActive(result.isNewRecord);
    }

    public void Retry()     => GameManager.Instance?.StartGame(
        GameManager.Instance.SelectedSong, GameManager.Instance.SelectedDifficulty);
    public void SongSelect() => GameManager.Instance?.GoToSongSelect();
}
