using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int   Score      { get; private set; }
    public int   Combo      { get; private set; }
    public int   MaxCombo   { get; private set; }
    public int   TotalHits  { get; private set; }
    public int   PerfectHits{ get; private set; }
    public int   MissedHits { get; private set; }
    public int   HoldScore  { get; private set; }

    public float Accuracy => TotalHits + MissedHits == 0 ? 1f :
                             (float)TotalHits / (TotalHits + MissedHits);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterHit(NoteBase note, bool correctDir, float velocity)
    {
        float beatDiff = Mathf.Abs(note.Data.beat - Conductor.Instance.SongBeat);
        float secDiff  = beatDiff * Conductor.Instance.SecondsPerBeat;

        HitGrade grade = secDiff < 0.05f ? HitGrade.Perfect :
                         secDiff < 0.12f ? HitGrade.Great   : HitGrade.Good;

        int baseScore = grade == HitGrade.Perfect ? 100 :
                        grade == HitGrade.Great   ? 70  : 40;
        if (!correctDir) baseScore /= 2;

        int multiplier = Mathf.Max(1, Combo / 8);
        Score += baseScore * multiplier;

        Combo++;
        MaxCombo = Mathf.Max(MaxCombo, Combo);
        TotalHits++;
        if (grade == HitGrade.Perfect) PerfectHits++;

        float heal = grade == HitGrade.Perfect ? 5f :
                     grade == HitGrade.Great   ? 3f : 1f;
        HealthSystem.Instance?.Heal(heal);

        HitGradePopup.Spawn(grade, note.transform.position);
        BackgroundSphere.NoteHit(note.Data.color);
        SfxManager.Get().PlayHit();
        HUD.Instance?.OnHit(grade, Score, Combo);
    }

    public void RegisterMiss(NoteBase note)
    {
        Combo = 0;
        MissedHits++;
        HitGradePopup.SpawnMiss(note.transform.position);
        HealthSystem.Instance?.TakeDamage(10);
        SfxManager.Get().PlayMiss();
        HUD.Instance?.OnMiss(Score, Combo);
    }

    public void RegisterWrongColor(NoteBase note)
    {
        Combo = 0;
        MissedHits++;
        HitGradePopup.SpawnMiss(note.transform.position);
        HealthSystem.Instance?.TakeDamage(8);
        SfxManager.Get().PlayWrongColor();
        HUD.Instance?.OnMiss(Score, Combo);
    }

    public void AddHoldScore(int points)
    {
        Score     += points;
        HoldScore += points;
        HUD.Instance?.UpdateScore(Score);
    }

    public SaveSystem.SongRecord BuildRecord(string songId, string difficulty)
    {
        bool fc = MissedHits == 0;
        return new SaveSystem.SongRecord
        {
            songId     = songId,
            difficulty = difficulty,
            highScore  = Score,
            accuracy   = Accuracy,
            fullCombo  = fc,
        };
    }
}
