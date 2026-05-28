using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SetupSliceLightShow
{
    static readonly string[] UpperGroupNames =
    {
        "DiagonalLasers_Right",
        "DiagonalLasers_Left",
        "CrossLasers",
        "ChevronLasers",
    };

    [MenuItem("VRBeat/Setup Slice Light Show")]
    public static void Setup()
    {
        GameObject lightShow = GameObject.Find("LightShow");
        if (lightShow == null)
        {
            Debug.LogError("[SetupSliceLightShow] LightShow GameObject not found.");
            return;
        }

        SliceLightShow sls = lightShow.GetComponent<SliceLightShow>();
        if (sls == null) sls = lightShow.AddComponent<SliceLightShow>();

        sls.upperGroups = CollectUpperGroups();
        sls.upperBeams = new Renderer[0];

        GameObject movingUp = GameObject.Find("MovingUpLasers");
        sls.rearBeams = CollectRenderers(movingUp);
        sls.rearLightUpMin = 2;
        sls.rearLightUpMax = 3;
        sls.flashDuration = 0.5f;
        sls.redColor = new Color(9.5f, 0.95f, 0.45f);
        sls.blueColor = new Color(0.12f, 3.20f, 16.0f);
        sls.rearRedColor = new Color(9.5f, 0.95f, 0.45f);
        sls.rearBlueColor = new Color(0.08f, 0.75f, 13.5f);
        sls.ringRedColor = new Color(16.0f, 0.55f, 2.25f);
        sls.ringBlueColor = new Color(0.12f, 3.20f, 16.0f);

        GameObject rings = GameObject.Find("Rings");
        (sls.redRings, sls.blueRings) = CollectRingsByColor(rings);

        EditorUtility.SetDirty(sls);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[SetupSliceLightShow] upperGroups={sls.upperGroups?.Length ?? 0}, " +
                  $"rear={sls.rearBeams?.Length ?? 0}, redRings={sls.redRings?.Length ?? 0}, " +
                  $"blueRings={sls.blueRings?.Length ?? 0}");
    }

    static SliceLightShow.RendererGroup[] CollectUpperGroups()
    {
        var groups = new List<SliceLightShow.RendererGroup>();
        for (int i = 0; i < UpperGroupNames.Length; i++)
        {
            GameObject root = GameObject.Find(UpperGroupNames[i]);
            Renderer[] renderers = CollectRenderers(root);
            if (renderers.Length == 0) continue;

            groups.Add(new SliceLightShow.RendererGroup
            {
                name = UpperGroupNames[i],
                renderers = renderers,
            });
        }

        return groups.ToArray();
    }

    static Renderer[] CollectRenderers(GameObject root)
    {
        if (root == null) return new Renderer[0];

        var list = new List<Renderer>();
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
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
            Transform pivot = ringsRoot.transform.GetChild(i);
            if (!pivot.name.StartsWith("Ring_")) continue;

            List<Renderer> target = (i & 1) == 0 ? red : blue;
            foreach (Renderer r in pivot.GetComponentsInChildren<Renderer>(true))
                if (r != null) target.Add(r);
        }

        return (red.ToArray(), blue.ToArray());
    }
}
