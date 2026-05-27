using UnityEngine;
using UnityEngine.Rendering;

// 미러 플로어용 실시간 Reflection Probe.
// 매 프레임 갱신은 Quest에서 비싸므로 N프레임마다 한 번만 RenderProbe() 호출(스로틀).
// 바닥 머티리얼 Smoothness 가 높으면 이 프로브의 큐브맵이 반사로 비친다.
[RequireComponent(typeof(ReflectionProbe))]
public class MirrorFloorProbe : MonoBehaviour
{
    [Tooltip("몇 프레임마다 반사를 다시 렌더링할지 (클수록 가벼움)")]
    [Range(1, 10)] public int refreshEvery = 3;

    ReflectionProbe probe;
    int frame;

    void Awake()
    {
        probe = GetComponent<ReflectionProbe>();
        probe.mode        = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.RenderProbe();
    }

    void Update()
    {
        if (++frame % refreshEvery == 0)
            probe.RenderProbe();
    }
}
