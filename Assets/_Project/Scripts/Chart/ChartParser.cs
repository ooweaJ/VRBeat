using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class ChartParser
{
    public static ChartData Parse(string json)
    {
        var chart = JsonUtility.FromJson<ChartData>(json);
        if (chart?.notes != null)
            Array.Sort(chart.notes, (a, b) => a.beat.CompareTo(b.beat));
        return chart;
    }

    public static IEnumerator LoadFromPath(string path, Action<ChartData> onLoaded)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string uri = path;
#else
        string uri = "file:///" + path.Replace("\\", "/");
#endif
        using var req = UnityWebRequest.Get(uri);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onLoaded?.Invoke(Parse(req.downloadHandler.text));
        else
            Debug.LogWarning($"[ChartParser] Load failed: {req.error} ({uri})");
    }

    public static ChartData LoadFromPathSync(string path)
    {
        if (!File.Exists(path)) return null;
        return Parse(File.ReadAllText(path));
    }
}
