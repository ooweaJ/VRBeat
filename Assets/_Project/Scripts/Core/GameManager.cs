using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.MainMenu;
    public GameSettings Settings { get; private set; }
    public SongData SelectedSong { get; set; }
    public DifficultyInfo SelectedDifficulty { get; set; }

    const string SceneMainMenu  = "SongSelect";
    const string SceneGameplay  = "Gameplay";
    const string SceneResult    = "Result";
    const string SceneCalib     = "Calibration";
    const string SceneTutorial  = "Tutorial";
    const string SceneSettings  = "Settings";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
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
        SceneLoader.Load(SceneResult);
    }

    public void GoToResult()
    {
        State = GameState.Result;
        SceneLoader.Load(SceneResult);
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
