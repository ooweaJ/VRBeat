using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 슬라이스 라이트쇼 셋업 — ChevronLasers(위 4), MovingUpLasers(뒤 움직이는), Rings를 자동 와이어링.
// 등록된 라이트는 평소 OFF, 슬라이스 성공 시에만 잠깐 점등.
public static class SetupSliceLightShow
{
    [MenuItem("VRBeat/Setup Slice Light Show")]
    public static void Setup()
    {
        var lightShow = GameObject.Find("LightShow");
        if (lightShow == null)
        {
            Debug.LogError("[SetupSliceLightShow] 'LightShow' GameObject 없음. Light Show 먼저 만들기.");
            return;
        }

        var sls = lightShow.GetComponent<SliceLightShow>();
        if (sls == null) sls = lightShow.AddComponent<SliceLightShow>();

        // 위 4개 — ChevronLasers (4 빔 순차 점등)
        var chev = GameObject.Find("ChevronLasers");
        sls.upperBeams = CollectRenderers(chev);

        // 뒤 움직이는 — MovingUpLasers (랜덤 2개)
        var movingUp = GameObject.Find("MovingUpLasers");
        sls.rearBeams = CollectRenderers(movingUp);

        // 링 — 색별 그룹
        var rings = GameObject.Find("Rings");
        (sls.redRings, sls.blueRings) = CollectRingsByColor(rings);

        UnityEditor.EditorUtility.SetDirty(sls);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[SetupSliceLightShow] upper={sls.upperBeams?.Length ?? 0}, " +
                  $"rear={sls.rearBeams?.Length ?? 0}, redRings={sls.redRings?.Length ?? 0}, " +
                  $"blueRings={sls.blueRings?.Length ?? 0}");
    }

    static Renderer[] CollectRenderers(GameObject root)
    {
        if (root == null) return new Renderer[0];
        var list = new List<Renderer>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            if (r != null) list.Add(r);
        return list.ToArray();
    }

    static (Renderer[] red, Renderer[] blue) CollectRingsByColor(GameObject ringsRoot)
    {
        var red = new List<Renderer>();
        var blue = new List<Renderer>();
        if (ringsRoot == null) return (red.ToArray(), blue.ToArray());

        for (int i = 0; i < ringsRoot.transform.childCount; i++)
        {
            var pivot = ringsRoot.transform.GetChild(i);
            if (!pivot.name.StartsWith("Ring_")) continue;
            int idx = i;
            // BuildRings에서 (i & 1) == 0 → red, 그 외 blue 사용 중
            var target = (idx & 1) == 0 ? red : blue;
            foreach (var r in pivot.GetComponentsInChildren<Renderer>(true))
                if (r != null) target.Add(r);
        }
        return (red.ToArray(), blue.ToArray());
    }
}
