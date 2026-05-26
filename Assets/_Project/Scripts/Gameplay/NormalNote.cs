using UnityEngine;

public class NormalNote : NoteBase
{
    public const float MinSliceVelocity = 2.0f;
    public const float DirectionDotThreshold = 0.7f;

    [SerializeField] Renderer meshRenderer;
    [SerializeField] NoteSliceHandler sliceHandler;

    public override void Initialize(NoteData d, float speed, float hz, GameConfig cfg)
    {
        base.Initialize(d, speed, hz, cfg);
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();

        if (meshRenderer != null)
        {
            Material mat = null;
            string colorName = data.color.ToLower();
            
#if UNITY_EDITOR
            string assetPath = colorName == "red" ? "Assets/Material/matR.mat" : "Assets/Material/matB.mat";
            mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
#endif

            if (mat != null)
            {
                meshRenderer.material = mat;
            }
            else
            {
                // Fallback to simple colors if material not found
                meshRenderer.material.color = colorName == "red" ? Color.red : Color.blue;
            }
        }
    }

    public override void OnSliced(Vector3 sliceDir, float velocity, SaberColor saberColor)
    {
        if (WasHit) return;

        // 1. Check Color Match
        if (!ColorMatches(saberColor, data.color))
        {
            WasHit = true;   // 중복 호출 방지
            Debug.Log($"[Note] Wrong Color! Saber: {saberColor}, Note: {data.color}");
            ScoreManager.Instance?.RegisterWrongColor(this);
            gameObject.SetActive(false);
            return;
        }

        // 2. Check Velocity
        if (velocity < MinSliceVelocity) 
        {
            Debug.Log($"[Note] Too slow! Velocity: {velocity:F2}");
            return;
        }

        // 3. Check Direction
        bool correctDir = data.direction == "any" ||
                          Vector3.Dot(sliceDir.normalized, GetRequiredDirection()) > DirectionDotThreshold;

        // 4. Success!
        WasHit = true;
        ScoreManager.Instance?.RegisterHit(this, correctDir, velocity);

        Material mat = meshRenderer != null ? meshRenderer.sharedMaterial : null;
        if (sliceHandler == null) sliceHandler = GetComponent<NoteSliceHandler>();
        sliceHandler?.Slice(sliceDir, mat);

        SliceEffect.Play(transform.position, sliceDir, data.color);
        gameObject.SetActive(false);
    }

}
