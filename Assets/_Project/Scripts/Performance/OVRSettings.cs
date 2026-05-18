using UnityEngine;

// Apply Quest-specific performance settings on startup.
public class OVRSettings : MonoBehaviour
{
    [SerializeField] int cpuLevel = 2;
    [SerializeField] int gpuLevel = 3;

    void Awake()
    {
#if UNITY_ANDROID
        OVRManager.fixedFoveatedRenderingLevel  = OVRManager.FixedFoveatedRenderingLevel.High;
        OVRManager.useDynamicFixedFoveatedRendering = true;
        OVRManager.cpuLevel = cpuLevel;
        OVRManager.gpuLevel = gpuLevel;
#endif
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount  = 0;
    }
}
