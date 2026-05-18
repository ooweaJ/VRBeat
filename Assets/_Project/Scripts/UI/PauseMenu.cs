using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;

    void Update()
    {
        // Quest menu button or keyboard Escape
        if (Input.GetKeyDown(KeyCode.Escape) || OVRInput.GetDown(OVRInput.Button.Start))
            Toggle();
    }

    public void Toggle()
    {
        bool paused = GameManager.Instance?.State == GameState.Paused;
        if (paused) Resume(); else Pause();
    }

    public void Pause()
    {
        GameManager.Instance?.PauseGame();
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        GameManager.Instance?.ResumeGame();
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void Quit()
    {
        Resume();
        GameManager.Instance?.GoToSongSelect();
    }
}
