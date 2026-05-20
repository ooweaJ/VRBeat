using UnityEngine;

public class Conductor : MonoBehaviour
{
    public static Conductor Instance { get; private set; }

    [SerializeField] AudioSource audioSource;
    public float bpm;
    public float songOffset;    // chart author offset
    public float userOffset;    // calibration offset

    double dspStartTime;
    double pauseDspTime;
    bool isPaused;

    public double SongTime =>
        AudioSettings.dspTime - dspStartTime - songOffset - userOffset;

    public float SongBeat => (float)(SongTime * bpm / 60.0);
    public float SecondsPerBeat => 60f / bpm;

    public bool IsPlaying => audioSource.isPlaying || (!isPaused && dspStartTime > 0);

    public bool IsSongFinished =>
        audioSource.clip != null && SongTime >= audioSource.clip.length;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void StartSong(AudioClip clip, float bpmValue, float offset = 0f)
    {
        bpm = bpmValue;
        songOffset = offset;
        userOffset = GameManager.Instance != null ? GameManager.Instance.Settings.userOffset : 0f;

        audioSource.clip = clip;
        dspStartTime = AudioSettings.dspTime + 0.5;
        audioSource.PlayScheduled(dspStartTime);
        isPaused = false;
    }

    public void Pause()
    {
        if (isPaused) return;
        pauseDspTime = AudioSettings.dspTime;
        audioSource.Pause();
        isPaused = true;
    }

    public void Resume()
    {
        if (!isPaused) return;
        double pausedDuration = AudioSettings.dspTime - pauseDspTime;
        dspStartTime += pausedDuration;
        audioSource.UnPause();
        isPaused = false;
    }

    public float BeatToSeconds(float beat) => beat * SecondsPerBeat;
}
