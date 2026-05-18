using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SongLoader : MonoBehaviour
{
    public IEnumerator LoadAudio(string path, Action<AudioClip> onLoaded)
    {
        string uri = PathToUri(path);
        using var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS);
        ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onLoaded?.Invoke(DownloadHandlerAudioClip.GetContent(req));
        else
            Debug.LogWarning($"[SongLoader] Audio load failed: {req.error} ({uri})");
    }

    public IEnumerator LoadCover(string path, Action<Sprite> onLoaded)
    {
        string uri = PathToUri(path);
        using var req = UnityWebRequestTexture.GetTexture(uri);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var tex = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height),
                                       new UnityEngine.Vector2(0.5f, 0.5f));
            onLoaded?.Invoke(sprite);
        }
        else
        {
            Debug.LogWarning($"[SongLoader] Cover load failed: {req.error}");
        }
    }

    static string PathToUri(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return path; // already jar:// on Android StreamingAssets
#else
        return "file:///" + path.Replace("\\", "/");
#endif
    }
}
