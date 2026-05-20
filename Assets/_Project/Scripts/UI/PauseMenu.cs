using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject pausePanel;
    [SerializeField] InputActionProperty menuAction;

    void OnEnable()
    {
        menuAction.action.Enable();
        menuAction.action.performed += OnMenuPressed;
    }

    void OnDisable()
    {
        menuAction.action.performed -= OnMenuPressed;
    }

    void OnMenuPressed(InputAction.CallbackContext context)
    {
        Toggle();
    }

    void Update()
    {
        // Keyboard Escape for testing
        if (Input.GetKeyDown(KeyCode.Escape))
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
