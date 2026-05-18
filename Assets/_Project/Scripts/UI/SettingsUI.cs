using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] Slider          noteSpeedSlider;
    [SerializeField] Toggle          leftHandedToggle;
    [SerializeField] Slider          masterVolumeSlider;
    [SerializeField] Slider          musicVolumeSlider;
    [SerializeField] Slider          sfxVolumeSlider;
    [SerializeField] TextMeshProUGUI offsetText;

    GameSettings settings;

    void Start()
    {
        settings = GameManager.Instance?.Settings ?? new GameSettings();
        Refresh();
    }

    void Refresh()
    {
        if (noteSpeedSlider     != null) noteSpeedSlider.value     = settings.noteSpeed;
        if (leftHandedToggle    != null) leftHandedToggle.isOn     = settings.leftHandedMode;
        if (masterVolumeSlider  != null) masterVolumeSlider.value  = settings.masterVolume;
        if (musicVolumeSlider   != null) musicVolumeSlider.value   = settings.musicVolume;
        if (sfxVolumeSlider     != null) sfxVolumeSlider.value     = settings.sfxVolume;
        UpdateOffsetText();
    }

    public void OnNoteSpeedChanged(float v)    { settings.noteSpeed     = v; }
    public void OnLeftHandedChanged(bool v)    { settings.leftHandedMode = v; }
    public void OnMasterVolumeChanged(float v) { settings.masterVolume  = v; }
    public void OnMusicVolumeChanged(float v)  { settings.musicVolume   = v; }
    public void OnSfxVolumeChanged(float v)    { settings.sfxVolume     = v; }

    public void IncreaseOffset() { settings.userOffset += 0.005f; UpdateOffsetText(); }
    public void DecreaseOffset() { settings.userOffset -= 0.005f; UpdateOffsetText(); }

    void UpdateOffsetText()
    {
        if (offsetText != null)
            offsetText.text = $"Offset: {settings.userOffset * 1000f:F0} ms";
    }

    public void Save()
    {
        GameManager.Instance?.SaveSettings();
    }

    public void Back() => GameManager.Instance?.GoToSongSelect();
}
