using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SongLibrary : MonoBehaviour
{
    public static SongLibrary Instance { get; private set; }

    public List<SongData> Songs { get; private set; } = new List<SongData>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<SongData> LoadAllSongs()
    {
        Songs.Clear();
        string songsPath = Path.Combine(Application.streamingAssetsPath, "Songs");
        Debug.Log($"[SongLibrary] Searching for songs at: {songsPath}");

#if UNITY_ANDROID && !UNITY_EDITOR
        // On Android, Directory.GetDirectories doesn't work on StreamingAssets.
        // Requires a manifest.json listing folder names — see SongLibraryAndroid for coroutine version.
        Debug.LogWarning("[SongLibrary] Synchronous scan not supported on Android. Use LoadAllSongsCoroutine.");
        return Songs;
#else
        if (!Directory.Exists(songsPath))
        {
            Debug.LogWarning($"[SongLibrary] Songs folder not found: {songsPath}");
            return Songs;
        }

        foreach (var dir in Directory.GetDirectories(songsPath))
        {
            string infoPath = Path.Combine(dir, "info.json");
            if (!File.Exists(infoPath)) continue;
            try
            {
                var info = JsonUtility.FromJson<SongInfo>(File.ReadAllText(infoPath));
                Songs.Add(new SongData { info = info, folderPath = dir });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SongLibrary] Failed to parse {infoPath}: {e.Message}");
            }
        }
        return Songs;
#endif
    }
}
