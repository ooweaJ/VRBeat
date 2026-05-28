using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// VRBeat 비-Gameplay 씬에 MenuStyleApplier 컴포넌트를 부착하고 즉시 적용.
// 각 씬을 열어서 'Apply Beat Saber Style (Current Scene)' 실행하거나
// 'Apply Beat Saber Style (All Menu Scenes)'로 5씬 일괄 처리.
public static class ApplyMenuStyle
{
    static readonly string[] MenuScenes =
    {
        "Assets/_Project/Scenes/SongSelect.unity",
        "Assets/_Project/Scenes/Settings.unity",
        "Assets/_Project/Scenes/Result.unity",
        "Assets/_Project/Scenes/Tutorial.unity",
        "Assets/_Project/Scenes/Calibration.unity",
    };

    [MenuItem("VRBeat/Apply Beat Saber Style (Current Scene)")]
    public static void ApplyCurrent()
    {
        var applier = AttachApplier();
        if (applier == null) return;
        applier.Apply();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[ApplyMenuStyle] Applied to {EditorSceneManager.GetActiveScene().name}.");
    }

    [MenuItem("VRBeat/Apply Beat Saber Style (All Menu Scenes)")]
    public static void ApplyAll()
    {
        var originalScene = EditorSceneManager.GetActiveScene().path;
        int applied = 0;

        foreach (var path in MenuScenes)
        {
            if (!System.IO.File.Exists(path)) { Debug.LogWarning($"[ApplyMenuStyle] {path} 없음, 건너뜀."); continue; }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var applier = AttachApplier();
            if (applier != null)
            {
                applier.Apply();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                applied++;
            }
        }

        if (!string.IsNullOrEmpty(originalScene) && System.IO.File.Exists(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        Debug.Log($"[ApplyMenuStyle] {applied}개 씬에 적용 완료.");
    }

    static MenuStyleApplier AttachApplier()
    {
        // 씬 루트의 어느 GO에 붙일지 찾아본다: 첫 번째 Canvas의 부모 또는 별도 GO 생성
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ApplyMenuStyle] 씬에 Canvas가 없음.");
            return null;
        }

        // 씬 최상위 GO에 MenuStyleApplier 단일 인스턴스
        var existing = Object.FindFirstObjectByType<MenuStyleApplier>();
        if (existing != null) return existing;

        var hostName = "_MenuStyle";
        var host = GameObject.Find(hostName);
        if (host == null) host = new GameObject(hostName);
        return host.AddComponent<MenuStyleApplier>();
    }
}
