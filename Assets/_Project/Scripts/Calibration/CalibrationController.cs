using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 일정한 BPM 메트로놈을 들려주고, 사용자가 박자에 맞춰 누른 타이밍과
/// 수학적으로 기대되는 박자 시각의 차이를 측정해 userOffset(입력 지연 보정값)을
/// 계산·저장한다. 클릭은 AudioSource.PlayScheduled로 샘플 단위 정확하게 예약한다.
/// </summary>
public class CalibrationController : MonoBehaviour
{
    [SerializeField] float bpm          = 120f;
    [SerializeField] int   requiredTaps = 8;
    [SerializeField] TextMeshProUGUI instructionText;
    [SerializeField] TextMeshProUGUI offsetText;

    AudioSource[] clickPool;
    int           poolIdx;
    AudioClip     clickClip;

    double secPerBeat;
    double dspStart;
    int    nextScheduleBeat;
    bool   running;
    readonly List<double> taps = new List<double>();

    void Awake()
    {
        secPerBeat = 60.0 / bpm;
        clickClip  = MakeClick();

        // 클릭이 겹치지 않도록 소스 풀(라운드로빈) 사용 → 정확한 예약 재생
        clickPool = new AudioSource[4];
        for (int i = 0; i < clickPool.Length; i++)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake  = false;
            a.spatialBlend = 0f;
            a.clip         = clickClip;
            clickPool[i]   = a;
        }
    }

    void Start()
    {
        float current = GameManager.Instance != null ? GameManager.Instance.Settings.userOffset : 0f;
        UpdateOffsetText(current);
        SetInstruction("START를 누른 뒤,\n들리는 클릭 박자에 맞춰\nTAP 버튼(또는 Space)을 누르세요.");
    }

    void Update()
    {
        if (running) ScheduleClicks();

        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) Tap();
    }

    // ── UI 버튼에서 호출 ─────────────────────────────────────────────
    public void StartCalibration()
    {
        taps.Clear();
        dspStart         = AudioSettings.dspTime + 1.0; // 1초 리드인
        nextScheduleBeat = 0;
        running          = true;
        SetInstruction("박자에 맞춰 누르세요...  (0/" + requiredTaps + ")");
    }

    public void Tap()
    {
        if (!running) return;

        double now = AudioSettings.dspTime;
        if (now < dspStart - secPerBeat * 0.5) return; // 첫 박자 이전 무시

        double beats   = (now - dspStart) / secPerBeat;
        long   nearest = (long)System.Math.Round(beats);
        if (nearest < 0) return;

        double expected = dspStart + nearest * secPerBeat;
        taps.Add(now - expected); // + : 박자보다 늦게 누름

        SetInstruction($"박자에 맞춰 누르세요...  ({taps.Count}/{requiredTaps})");
        if (taps.Count >= requiredTaps) Finish();
    }

    public void Back() => GameManager.Instance?.GoToSongSelect();

    // ── 측정 종료 ───────────────────────────────────────────────────
    void Finish()
    {
        running = false;

        // 평균보다 이상치에 강한 중앙값 사용
        var sorted = new List<double>(taps);
        sorted.Sort();
        float offset = (float)sorted[sorted.Count / 2];

        if (GameManager.Instance != null)
        {
            GameManager.Instance.Settings.userOffset = offset;
            GameManager.Instance.SaveSettings();
        }

        UpdateOffsetText(offset);
        SetInstruction("완료! 보정값을 저장했습니다.\n다시 측정하려면 START.");
    }

    void ScheduleClicks()
    {
        // 약 1초 앞까지 미리 예약
        while (dspStart + nextScheduleBeat * secPerBeat < AudioSettings.dspTime + 1.0)
        {
            double when = dspStart + nextScheduleBeat * secPerBeat;
            var a = clickPool[poolIdx];
            poolIdx = (poolIdx + 1) % clickPool.Length;
            a.PlayScheduled(when);
            nextScheduleBeat++;
        }
    }

    void SetInstruction(string msg)
    {
        if (instructionText != null) instructionText.text = msg;
    }

    void UpdateOffsetText(float offsetSeconds)
    {
        if (offsetText != null)
            offsetText.text = $"현재 보정값: {offsetSeconds * 1000f:+0;-0;0} ms";
    }

    /// <summary>짧은 고음 클릭 톤(메트로놈)을 생성.</summary>
    static AudioClip MakeClick()
    {
        const int sampleRate = 44100;
        const float duration = 0.04f;
        const float freq     = 1000f;
        int samples = (int)(sampleRate * duration);
        var data = new float[samples];

        float decay = 1f / (duration * 0.3f);
        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / sampleRate;
            float env = Mathf.Exp(-t * decay);
            data[i]   = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
        }

        var clip = AudioClip.Create("metronome_click", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
