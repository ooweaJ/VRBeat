using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static event Action<float> OnProgress;

    public static void Load(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"[SceneLoader] Cannot load scene '{sceneName}'. Is it added to Build Settings?");
        }
    }

    public static IEnumerator LoadAsync(string sceneName, Action onComplete = null)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Cannot load async scene '{sceneName}'.");
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            OnProgress?.Invoke(op.progress / 0.9f);
            yield return null;
        }

        OnProgress?.Invoke(1f);
        op.allowSceneActivation = true;
        yield return new WaitUntil(() => op.isDone);
        onComplete?.Invoke();
    }
}
