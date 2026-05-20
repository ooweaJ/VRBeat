using UnityEngine;

// Apply performance settings on startup.
public class OVRSettings : MonoBehaviour
{
    [SerializeField] int cpuLevel = 2;
    [SerializeField] int gpuLevel = 3;

    void Awake()
    {
        // OVRManager requires Meta XR SDK. Uncomment if package is installed.
/*
#if UNITY_ANDROID && !UNITY_EDITOR
        OVRManager.fixedFoveatedRenderingLevel  = OVRManager.FixedFoveatedRenderingLevel.High;
        OVRManager.useDynamicFixedFoveatedRendering = true;
        OVRManager.cpuLevel = cpuLevel;
        OVRManager.gpuLevel = gpuLevel;
#endif
*/
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount  = 0;
    }
}
