using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// 2단계 캘리브레이션
/// 1단계: 소리만 — 들리고 나서 눌러서 오프셋 측정 (예측 금지)
/// 2단계: 비주얼 트랙 무한 루프 — 맞으면 저장, 아니면 다시 측정
public class CalibrationController : MonoBehaviour
{
    [SerializeField] float               bpm           = 72f;
    [SerializeField] TextMeshProUGUI     instructionText;
    [SerializeField] TextMeshProUGUI     offsetText;
    [SerializeField] GameObject          confirmPanel;
    [SerializeField] TMPro.TMP_FontAsset koreanFont;

    const int MeasureCount = 5;

    // 트랙 (2단계에서만 표시)
    GameObject    trackGO;
    RectTransform trackRT;
    const float TrackWidth  = 660f;
    const float TrackHeight = 60f;
    const float NoteW       = 54f;
    const float NoteH       = 58f;
    const float HitLineX    = -TrackWidth * 0.5f + 44f;
    const float approachSec = 2.0f;
    float pixelsPerSec;

    double secPerBeat;
    double dspStart;
    int    nextBeatIdx;

    enum Phase { Idle, Measuring, Verifying, Done }
    Phase phase = Phase.Idle;

    readonly List<double>                             taps           = new();
    readonly List<double>                             scheduledClicks = new();
    readonly List<(RectTransform rt, double hitTime)> activeNotes    = new();

    AudioSource[] clickPool;
    int           poolIdx;
    AudioClip     clickClip;
    bool          prevBtn;
    float         measuredOffset;

    void Awake()
    {
        secPerBeat   = 60.0 / bpm;
        pixelsPerSec = TrackWidth / approachSec;
        clickClip    = MakeClick();
        clickPool    = new AudioSource[8];
        for (int i = 0; i < clickPool.Length; i++)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false; a.spatialBlend = 0f; a.clip = clickClip;
            clickPool[i] = a;
        }
    }

    void Start()
    {
        BuildTrackUI();
        float cur = GameManager.Instance != null ? GameManager.Instance.Settings.userOffset : 0f;
        UpdateOffsetText(cur);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        SetInstruction(
            "[ 캘리브레이션 ]\n\n" +
            "<b>1단계</b> — 딸깍 소리가 들리면\n" +
            "예측하지 말고 <b>들린 후에</b> 버튼을 누르세요.\n\n" +
            "<b>2단계</b> — 노트가 선에 닿는 타이밍과\n" +
            "소리가 맞는지 눈으로 확인합니다.\n\n" +
            "<color=#FFDD44>START</color>를 눌러 시작하세요.");
    }

    void BuildTrackUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var canvasRT = canvas.GetComponent<RectTransform>();

        trackGO = new GameObject("NoteTrack", typeof(RectTransform), typeof(Image));
        trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.SetParent(canvasRT, false);
        trackRT.anchoredPosition = new Vector2(0f, 200f);
        trackRT.sizeDelta        = new Vector2(TrackWidth, TrackHeight);
        trackGO.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.12f, 0.95f);
        trackGO.SetActive(false); // 1단계에선 숨김

        var hlGO = new GameObject("HitLine", typeof(RectTransform), typeof(Image));
        var hlRT = hlGO.GetComponent<RectTransform>();
        hlRT.SetParent(trackRT, false);
        hlRT.anchoredPosition = new Vector2(HitLineX, 0f);
        hlRT.sizeDelta        = new Vector2(5f, TrackHeight + 30f);
        hlGO.GetComponent<Image>().color = new Color(1f, 0.9f, 0.15f, 1f);

        var lblGO = new GameObject("HitLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.SetParent(trackRT, false);
        lblRT.anchoredPosition = new Vector2(HitLineX, TrackHeight * 0.5f + 26f);
        lblRT.sizeDelta        = new Vector2(54f, 36f);
        var lbl = lblGO.GetComponent<TextMeshProUGUI>();
        lbl.text = "▶"; lbl.fontSize = 24f;
        lbl.alignment = TMPro.TextAlignmentOptions.Center;
        lbl.color = new Color(1f, 0.9f, 0.15f, 1f);
        if (koreanFont != null) lbl.font = koreanFont;
    }

    // ── 공개 메서드 ──────────────────────────────────────────────────────
    public void StartCalibration()
    {
        taps.Clear();
        scheduledClicks.Clear();
        ClearNotes();
        if (trackGO != null) trackGO.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        dspStart    = AudioSettings.dspTime + 1.0;
        nextBeatIdx = 0;
        phase       = Phase.Measuring;
        SetInstruction(
            "<b>[ 1단계: 측정 ]</b>\n\n" +
            "딸깍 소리가 들리면\n" +
            "<b>들린 후에</b> 버튼을 누르세요!\n\n" +
            "<size=19><color=#AAAAAA>예측해서 미리 누르지 마세요</color></size>\n\n" +
            $"( 0 / {MeasureCount} )");
    }

    public void Tap()
    {
        if (phase != Phase.Measuring) return;
        double now = AudioSettings.dspTime;

        int    bestIdx   = -1;
        double bestDelta = double.MaxValue;
        for (int i = 0; i < scheduledClicks.Count; i++)
        {
            double d = System.Math.Abs(now - scheduledClicks[i]);
            if (d < bestDelta) { bestDelta = d; bestIdx = i; }
        }
        if (bestIdx < 0 || bestDelta > 1.5) return;

        taps.Add(now - scheduledClicks[bestIdx]);
        int cnt = taps.Count;
        SetInstruction(
            "<b>[ 1단계: 측정 ]</b>\n\n" +
            "딸깍 소리가 들리면\n" +
            "<b>들린 후에</b> 버튼을 누르세요!\n\n" +
            "<size=19><color=#AAAAAA>예측해서 미리 누르지 마세요</color></size>\n\n" +
            $"( {cnt} / {MeasureCount} )");

        if (cnt >= MeasureCount) StartVerifying();
    }

    public void ConfirmCalibration()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Settings.userOffset = measuredOffset;
            GameManager.Instance.SaveSettings();
        }
        phase = Phase.Done;
        ClearNotes();
        if (trackGO != null) trackGO.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        UpdateOffsetText(measuredOffset);
        SetInstruction(
            "<b>저장 완료!</b>\n\n" +
            $"보정값: <color=#00EE66><b>{measuredOffset * 1000f:+0;-0;0} ms</b></color>\n\n" +
            "START를 눌러 다시 측정하거나\n뒤로가기로 나갈 수 있습니다.");
    }

    public void RetryCalibration() => StartCalibration();

    public void Back() => GameManager.Instance?.GoToSongSelect();

    // ── Update ──────────────────────────────────────────────────────────
    void Update()
    {
        if (phase == Phase.Measuring) ScheduleClicks();
        if (phase == Phase.Verifying) { ScheduleNotes(); MoveNotes(); }

        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) Tap();
        bool btn = AnyControllerButtonPressed();
        if (btn && !prevBtn) Tap();
        prevBtn = btn;
    }

    // ── Phase 1: 소리만 ──────────────────────────────────────────────────
    void ScheduleClicks()
    {
        while (dspStart + nextBeatIdx * secPerBeat < AudioSettings.dspTime + 2.0)
        {
            double t = dspStart + nextBeatIdx * secPerBeat;
            var src = clickPool[poolIdx];
            poolIdx = (poolIdx + 1) % clickPool.Length;
            src.PlayScheduled(t);
            scheduledClicks.Add(t);
            nextBeatIdx++;
        }
        scheduledClicks.RemoveAll(t => t < AudioSettings.dspTime - 2.0);
    }

    // ── Phase 2: 비주얼 노트 무한 루프 ──────────────────────────────────
    void StartVerifying()
    {
        var sorted = new List<double>(taps);
        sorted.Sort();
        measuredOffset = (float)sorted[sorted.Count / 2];

        if (GameManager.Instance != null)
            GameManager.Instance.Settings.userOffset = measuredOffset;
        UpdateOffsetText(measuredOffset);

        scheduledClicks.Clear();
        ClearNotes();
        if (trackGO != null) trackGO.SetActive(true);
        if (confirmPanel != null) confirmPanel.SetActive(true);

        dspStart    = AudioSettings.dspTime + 1.5;
        nextBeatIdx = 0;
        phase       = Phase.Verifying;
        SetInstruction(
            "<b>[ 2단계: 확인 ]</b>\n\n" +
            $"측정값: <b>{measuredOffset * 1000f:+0;-0;0} ms</b>\n\n" +
            "노트가 <color=#FFDD44>▶</color>에 닿을 때\n" +
            "소리가 딱 맞으면 <b>저장</b>을 누르세요.");
    }

    void ScheduleNotes()
    {
        // 무한 루프 — confirmPanel 눌릴 때까지 계속
        while (dspStart + nextBeatIdx * secPerBeat - approachSec < AudioSettings.dspTime + 0.1)
        {
            double hitTime = dspStart + nextBeatIdx * secPerBeat;
            CreateNote(hitTime);
            nextBeatIdx++;
        }
    }

    void CreateNote(double hitTime)
    {
        var go = new GameObject("CalibNote", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(trackRT, false);
        rt.sizeDelta = new Vector2(NoteW, NoteH);
        go.GetComponent<Image>().color = new Color(0.15f, 0.85f, 0.25f, 1f);
        activeNotes.Add((rt, hitTime));

        var src = clickPool[poolIdx];
        poolIdx = (poolIdx + 1) % clickPool.Length;
        src.PlayScheduled(hitTime);
    }

    void MoveNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var (rt, hitTime) = activeNotes[i];
            if (!rt || !rt.gameObject) { activeNotes.RemoveAt(i); continue; }

            double timeToHit = hitTime - AudioSettings.dspTime;
            float  x         = HitLineX + (float)(timeToHit * pixelsPerSec);
            rt.anchoredPosition = new Vector2(x, 0f);

            if (x < HitLineX - 360f)
            {
                Destroy(rt.gameObject);
                activeNotes.RemoveAt(i);
            }
        }
    }

    void ClearNotes()
    {
        foreach (var (rt, _) in activeNotes)
            if (rt && rt.gameObject) Destroy(rt.gameObject);
        activeNotes.Clear();
    }

    void SetInstruction(string msg) { if (instructionText != null) instructionText.text = msg; }
    void UpdateOffsetText(float s)  { if (offsetText != null) offsetText.text = $"현재 보정값: {s * 1000f:+0;-0;0} ms"; }

    static bool AnyControllerButtonPressed()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,   out bool a) && a) return true;
            if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool b) && b) return true;
            if (d.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,      out bool g) && g) return true;
        }
        return false;
    }

    static AudioClip MakeClick()
    {
        const int   sr  = 44100;
        const float dur = 0.06f;
        const float frq = 880f;
        int   n   = (int)(sr * dur);
        var   d   = new float[n];
        float dec = 1f / (dur * 0.25f);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            d[i] = Mathf.Sin(2f * Mathf.PI * frq * t) * Mathf.Exp(-t * dec) * 0.7f;
        }
        var clip = AudioClip.Create("calib_click", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }
}
