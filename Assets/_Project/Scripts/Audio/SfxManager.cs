using UnityEngine;

/// <summary>
/// 게임 효과음(히트/미스) 재생기. 효과음 에셋이 지정되지 않으면 코드로 짧은
/// 신스 톤을 생성해 사용한다(외부 에셋 의존 없음). 나중에 Inspector에서
/// hitClip/missClip에 실제 파일을 넣으면 그걸 우선 사용한다.
///
/// 씬에 미리 배치돼 있지 않아도 <see cref="Get"/>가 필요 시 자동 생성한다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("선택: 비워두면 코드로 생성")]
    [SerializeField] AudioClip hitClip;
    [SerializeField] AudioClip wrongColorClip;
    [SerializeField] AudioClip missClip;

    AudioSource src;

    /// <summary>인스턴스를 반환하고, 없으면 즉석에서 생성한다.</summary>
    public static SfxManager Get()
    {
        if (Instance == null)
        {
            var go = new GameObject("SfxManager");
            Instance = go.AddComponent<SfxManager>();
        }
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D

        if (hitClip        == null) hitClip        = GenerateBlip("sfx_hit",   880f, 0.07f, 0.5f);
        if (wrongColorClip == null) wrongColorClip = GenerateBlip("sfx_wrong", 220f, 0.12f, 0.45f);
        if (missClip       == null) missClip       = GenerateBlip("sfx_miss",  150f, 0.18f, 0.45f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayHit()        => Play(hitClip);
    public void PlayWrongColor() => Play(wrongColorClip);
    public void PlayMiss()       => Play(missClip);

    void Play(AudioClip clip)
    {
        if (clip == null || src == null) return;
        src.PlayOneShot(clip, Volume());
    }

    static float Volume()
    {
        var s = GameManager.Instance?.Settings;
        return s != null ? s.sfxVolume * s.masterVolume : 1f;
    }

    /// <summary>지수 감쇠 엔벨로프를 가진 짧은 사인 톤 클립을 생성한다.</summary>
    static AudioClip GenerateBlip(string name, float freq, float duration, float gain)
    {
        const int sampleRate = 44100;
        int samples = Mathf.Max(1, (int)(sampleRate * duration));
        var data = new float[samples];

        float decay = 1f / (duration * 0.4f); // 앞쪽에서 빠르게 감쇠
        for (int i = 0; i < samples; i++)
        {
            float t   = (float)i / sampleRate;
            float env = Mathf.Exp(-t * decay);
            data[i]   = Mathf.Sin(2f * Mathf.PI * freq * t) * env * gain;
        }

        var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
