using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.MainMenu;
    public GameSettings Settings { get; private set; }
    public SongData SelectedSong { get; set; }
    public DifficultyInfo SelectedDifficulty { get; set; }

    /// <summary>가장 최근 플레이 결과. Result 씬의 ResultUI가 이 값을 읽어 표시한다.</summary>
    public GameResult LastResult { get; private set; }

    const string SceneMainMenu  = "SongSelect";
    const string SceneGameplay  = "Gameplay";
    const string SceneResult    = "Result";
    const string SceneCalib     = "Calibration";
    const string SceneTutorial  = "Tutorial";
    const string SceneSettings  = "Settings";

    void Awake()
    {
        // A persistent GameManager already exists (carried over via DontDestroyOnLoad
        // from a previous scene). Each scene also carries its own GameManager so it can
        // be played standalone, but here it's a duplicate. Destroy only THIS component —
        // not the GameObject — because in gameplay scenes the GameManager shares its
        // GameObject ([Managers]) with scene-only managers like Conductor/ScoreManager,
        // and destroying the GameObject would take those children down with it.
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Settings = GameSettings.Load();
    }

    public void StartGame(SongData song, DifficultyInfo difficulty)
    {
        SelectedSong = song;
        SelectedDifficulty = difficulty;
        State = GameState.Playing;
        SceneLoader.Load(SceneGameplay);
    }

    public void PauseGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Conductor.Instance?.Pause();
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Conductor.Instance?.Resume();
    }

    public void GameOver()
    {
        State = GameState.GameOver;
        CaptureResult();
        SceneLoader.Load(SceneResult);
    }

    public void GoToResult()
    {
        State = GameState.Result;
        CaptureResult();
        SceneLoader.Load(SceneResult);
    }

    /// <summary>
    /// Result 씬으로 넘어가기 직전(아직 Gameplay 씬, ScoreManager가 살아있음)에
    /// 점수를 스냅샷으로 떠서 LastResult에 담고, 최고점수를 저장한다.
    /// </summary>
    void CaptureResult()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) { LastResult = null; return; }

        var result = new GameResult
        {
            score       = sm.Score,
            maxCombo    = sm.MaxCombo,
            totalHits   = sm.TotalHits,
            perfectHits = sm.PerfectHits,
            missedHits  = sm.MissedHits,
            accuracy    = sm.Accuracy,
            fullCombo   = sm.MissedHits == 0,
        };

        if (SelectedSong != null && SelectedDifficulty != null)
        {
            string songId = SelectedSong.info.songId;
            string diff   = SelectedDifficulty.name;

            var prev = SaveSystem.LoadRecord(songId, diff);
            result.isNewRecord = prev == null || sm.Score > prev.highScore;
            SaveSystem.SaveRecord(sm.BuildRecord(songId, diff));
        }

        LastResult = result;
    }

    public void GoToSongSelect()
    {
        State = GameState.MainMenu;
        SceneLoader.Load(SceneMainMenu);
    }

    public void GoToCalibration() => SceneLoader.Load(SceneCalib);
    public void GoToTutorial()    => SceneLoader.Load(SceneTutorial);
    public void GoToSettings()    => SceneLoader.Load(SceneSettings);

    public void SaveSettings()
    {
        Settings.Save();
    }
}
