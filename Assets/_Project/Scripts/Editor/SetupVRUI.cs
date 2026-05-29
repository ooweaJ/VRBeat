using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem.UI;

public static class SetupVRUI
{
    const string PrefabPath  = "Assets/_Project/Prefabs/XR Origin (XR Rig).prefab";

    static readonly string[] MenuScenes =
    {
        "Assets/_Project/Scenes/SongSelect.unity",
        "Assets/_Project/Scenes/Result.unity",
        "Assets/_Project/Scenes/Calibration.unity",
        "Assets/_Project/Scenes/Settings.unity",
        "Assets/_Project/Scenes/Tutorial.unity",
    };

    [MenuItem("VRBeat/Setup VR UI Interaction")]
    public static void Run()
    {
        SetupXROriginPrefab();
        foreach (var scene in MenuScenes)
            SetupMenuScene(scene);

        AssetDatabase.SaveAssets();
        Debug.Log("[VRBeat] VR UI setup complete.");
    }

    // ──────────────────────────────────────────────────
    // 1. XR Origin 프리팹 — Right Controller에 UIRayInteractor 추가
    // ───────────────��────────────────────────────��─────
    static void SetupXROriginPrefab()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[VRBeat] Prefab not found: {PrefabPath}");
            return;
        }

        try
        {
            var rightCtrl = FindDeepChild(prefabRoot.transform, "Right Controller");
            if (rightCtrl == null)
            {
                Debug.LogError("[VRBeat] 'Right Controller' not found in prefab.");
                return;
            }

            // 이미 존재하면 건너뜀
            if (FindDeepChild(rightCtrl, "UIRayInteractor") != null)
            {
                Debug.Log("[VRBeat] UIRayInteractor already exists.");
                return;
            }

            var rayObj = new GameObject("UIRayInteractor");
            rayObj.transform.SetParent(rightCtrl, false);

            // ── XRRayInteractor ──
            var ray = rayObj.AddComponent<XRRayInteractor>();
            ray.enableUIInteraction = true;

            // trigger 로 UI 클릭 — m_UIPressInput 로컬 액션 (InputSourceMode=1)
            var sRay = new SerializedObject(ray);
            SetLocalButtonAction(sRay.FindProperty("m_SelectInput"),  "Select",   "<XRController>{RightHand}/triggerPressed");
            SetLocalButtonAction(sRay.FindProperty("m_UIPressInput"), "UI Press", "<XRController>{RightHand}/triggerPressed");
            sRay.ApplyModifiedPropertiesWithoutUndo();

            // ── LineRenderer (레이 시각화) ──
            var lr = rayObj.AddComponent<LineRenderer>();
            lr.widthMultiplier    = 0.005f;
            lr.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows     = false;
            lr.useWorldSpace      = true;
            lr.material           = new Material(Shader.Find("Sprites/Default"));

            // ── XRInteractorLineVisual ──
            rayObj.AddComponent<XRInteractorLineVisual>();

            // ── ControllerInputActionManager 에 연결 ──
            var actionMgr = rightCtrl.GetComponent<ControllerInputActionManager>();
            if (actionMgr != null)
            {
                var sMgr = new SerializedObject(actionMgr);
                sMgr.FindProperty("m_RayInteractor").objectReferenceValue = ray;
                sMgr.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── XRInteractionGroup 첫 번째 빈 슬롯에 연결 ──
            var group = rightCtrl.GetComponent<XRInteractionGroup>();
            if (group != null)
            {
                var sGroup   = new SerializedObject(group);
                var members  = sGroup.FindProperty("m_StartingGroupMembers");
                bool assigned = false;
                for (int i = 0; i < members.arraySize; i++)
                {
                    var elem = members.GetArrayElementAtIndex(i);
                    if (elem.objectReferenceValue == null)
                    {
                        elem.objectReferenceValue = ray;
                        assigned = true;
                        break;
                    }
                }
                if (!assigned)
                {
                    members.arraySize++;
                    members.GetArrayElementAtIndex(members.arraySize - 1).objectReferenceValue = ray;
                }
                sGroup.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Debug.Log("[VRBeat] UIRayInteractor added to Right Controller.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    // ──────────────────────────────────────────────────
    // 2. 메뉴 씬 — EventSystem + Canvas 수정
    // ─────────────���──────────────────────────────��─────
    static void SetupMenuScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        bool dirty = false;

        foreach (var root in scene.GetRootGameObjects())
        {
            // EventSystem: InputSystemUIInputModule → XRUIInputModule
            var es = root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
            if (es != null)
            {
                var inputSysModule = es.GetComponent<InputSystemUIInputModule>();
                if (inputSysModule != null && inputSysModule.enabled)
                {
                    inputSysModule.enabled = false;
                    dirty = true;
                }

                if (es.GetComponent<XRUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<XRUIInputModule>();
                    dirty = true;
                    Debug.Log($"[VRBeat] XRUIInputModule added → {scenePath}");
                }
            }

            // Canvas: TrackedDeviceGraphicRaycaster (XRI) 추가
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                    dirty = true;
                    Debug.Log($"[VRBeat] TrackedDeviceGraphicRaycaster added to '{canvas.name}' → {scenePath}");
                }
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.CloseScene(scene, true);
    }

    // ──────────────────────────────────────────────────
    // 헬퍼
    // ──────��───────────────────────────────────────────

    // XRInputButtonReader 의 m_InputSourceMode=1 (local InputAction) + 단일 바인딩 설정
    static void SetLocalButtonAction(SerializedProperty reader, string name, string binding)
    {
        reader.FindPropertyRelative("m_InputSourceMode").intValue = 1;

        var performed = reader.FindPropertyRelative("m_InputActionPerformed");
        performed.FindPropertyRelative("m_Name").stringValue = name;
        performed.FindPropertyRelative("m_Type").intValue    = 1; // Button

        var id = performed.FindPropertyRelative("m_Id");
        if (string.IsNullOrEmpty(id.stringValue))
            id.stringValue = System.Guid.NewGuid().ToString();

        var bindings = performed.FindPropertyRelative("m_SingletonActionBindings");
        bindings.arraySize = 1;
        var b = bindings.GetArrayElementAtIndex(0);
        b.FindPropertyRelative("m_Name").stringValue   = "";
        b.FindPropertyRelative("m_Id").stringValue     = System.Guid.NewGuid().ToString();
        b.FindPropertyRelative("m_Path").stringValue   = binding;
        b.FindPropertyRelative("m_Action").stringValue = name;
        b.FindPropertyRelative("m_Flags").intValue     = 0;
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
