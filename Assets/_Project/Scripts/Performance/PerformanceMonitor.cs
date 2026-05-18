using TMPro;
using UnityEngine;

public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI fpsText;
    [SerializeField] bool showInRelease = false;

    float elapsed;
    int frameCount;
    float currentFps;

    const float UpdateInterval = 0.5f;
    const float TargetMs = 11.1f; // 90 fps

    void Start()
    {
#if !UNITY_EDITOR
        if (!showInRelease && fpsText != null)
            fpsText.gameObject.SetActive(false);
#endif
    }

    void Update()
    {
        frameCount++;
        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= UpdateInterval)
        {
            currentFps = frameCount / elapsed;
            elapsed    = 0;
            frameCount = 0;

            if (fpsText != null && fpsText.gameObject.activeSelf)
            {
                float ms = 1000f / currentFps;
                fpsText.text  = $"{currentFps:F0} fps  {ms:F1} ms";
                fpsText.color = ms <= TargetMs ? Color.green : Color.red;
            }

            if (currentFps < 72f)
                Debug.LogWarning($"[Perf] Low FPS: {currentFps:F1}");
        }
    }
}
