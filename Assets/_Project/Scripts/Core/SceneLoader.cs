using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static event Action<float> OnProgress;

    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static IEnumerator LoadAsync(string sceneName, Action onComplete = null)
    {
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
