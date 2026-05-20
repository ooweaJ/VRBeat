using UnityEngine;
using UnityEditor;

public static class SaberTrailMaterialCreator
{
    [MenuItem("VRBeat/Create Saber Trail Materials")]
    static void CreateAll()
    {
        CreateTrailMat("matTrailRed",  new Color(1f, 0.15f, 0.15f, 1f));
        CreateTrailMat("matTrailBlue", new Color(0.15f, 0.5f, 1f,  1f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VRBeat] Saber trail materials created in Assets/Material/");
    }

    static void CreateTrailMat(string matName, Color color)
    {
        string savePath = $"Assets/Material/{matName}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(savePath) != null)
        {
            Debug.Log($"[VRBeat] {matName} already exists, skipping.");
            return;
        }

        // URP → Legacy 순으로 셰이더 탐색
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Particles/Additive")                        ??
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("[VRBeat] Trail shader not found. Install URP or check package.");
            return;
        }

        var mat = new Material(shader) { name = matName };

        // URP Particles/Unlit 프로퍼티
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_BlendMode", 3f);      // Additive
        mat.SetFloat("_SrcBlend",  1f);      // One
        mat.SetFloat("_DstBlend",  1f);      // One
        mat.SetFloat("_ZWrite",    0f);
        mat.SetFloat("_Surface",   1f);      // Transparent
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_BLENDMODE_ADD");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, savePath);
    }
}
