using UnityEngine;

// Apply performance settings on startup.
// 씬에 컴포넌트를 배치하지 않아도 RuntimeInitializeOnLoadMethod로 항상 적용된다.
public class OVRSettings : MonoBehaviour
{
    [SerializeField] int cpuLevel = 2;
    [SerializeField] int gpuLevel = 3;

    /// <summary>
    /// 모든 씬 로드 후 자동 실행. 90fps 목표 + vSync off. (씬 배치 불필요)
    /// XR에서는 실제 프레임 페이싱을 컴포지터가 제어하지만, 비-XR 경로/에디터에서도
    /// 일관되게 90을 목표로 두기 위해 설정한다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyFrameRate()
    {
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount  = 0;
    }

    void Awake()
    {
        // OVRManager requires Meta XR SDK. 이 프로젝트는 OpenXR(XRI) 기반이라
        // 고정 포비티드 렌더링/CPU·GPU 레벨은 OpenXR Meta Quest feature가 담당한다.
/*
#if UNITY_ANDROID && !UNITY_EDITOR
        OVRManager.fixedFoveatedRenderingLevel  = OVRManager.FixedFoveatedRenderingLevel.High;
        OVRManager.useDynamicFixedFoveatedRendering = true;
        OVRManager.cpuLevel = cpuLevel;
        OVRManager.gpuLevel = gpuLevel;
#endif
*/
        ApplyFrameRate();
    }
}
