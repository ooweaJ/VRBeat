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
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        float accuracy = sm.Accuracy;
        string rank    = AccuracyCalc.GetRank(accuracy);

        if (scoreText    != null) scoreText.text    = sm.Score.ToString("N0");
        if (accuracyText != null) accuracyText.text = $"{accuracy * 100f:F1}%";
        if (comboText    != null) comboText.text    = $"Max Combo: {sm.MaxCombo}";
        if (rankText     != null) rankText.text     = rank;

        if (fullComboEffect != null) fullComboEffect.SetActive(sm.MissedHits == 0);

        SaveResult(sm, accuracy);
    }

    void SaveResult(ScoreManager sm, float accuracy)
    {
        var gm = GameManager.Instance;
        if (gm?.SelectedSong == null || gm.SelectedDifficulty == null) return;

        string songId = gm.SelectedSong.info.songId;
        string diff   = gm.SelectedDifficulty.name;

        var prev    = SaveSystem.LoadRecord(songId, diff);
        bool newRec = prev == null || sm.Score > prev.highScore;

        SaveSystem.SaveRecord(sm.BuildRecord(songId, diff));

        if (newRecordText != null) newRecordText.gameObject.SetActive(newRec);
    }

    public void Retry()     => GameManager.Instance?.StartGame(
        GameManager.Instance.SelectedSong, GameManager.Instance.SelectedDifficulty);
    public void SongSelect() => GameManager.Instance?.GoToSongSelect();
}
