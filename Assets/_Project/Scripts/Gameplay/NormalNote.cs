using UnityEngine;

public class NormalNote : NoteBase
{
    public const float MinSliceVelocity = 2.0f;
    public const float DirectionDotThreshold = 0.7f;

    [SerializeField] Renderer meshRenderer;
    [SerializeField] NoteSliceHandler sliceHandler;

    static Material s_MatRed;
    static Material s_MatBlue;

    public override void Initialize(NoteData d, float speed, float hz, GameConfig cfg)
    {
        base.Initialize(d, speed, hz, cfg);
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer == null) return;

        string colorName = data.color.ToLower();
        Material mat = GetNoteMaterial(colorName);

        if (mat != null)
            meshRenderer.material = mat;
        else
            meshRenderer.material.color = colorName == "red" ? Color.red : Color.blue;
    }

    static Material GetNoteMaterial(string colorName)
    {
        if (colorName == "red")
        {
            if (s_MatRed == null) s_MatRed = Resources.Load<Material>("Notes/Note_Red");
            return s_MatRed;
        }
        else
        {
            if (s_MatBlue == null) s_MatBlue = Resources.Load<Material>("Notes/Note_Blue");
            return s_MatBlue;
        }
    }

    public override bool OnSliced(Vector3 sliceDir, float velocity, SaberColor saberColor)
    {
        if (WasHit) return false;

        // 1. Check Color Match
        if (!ColorMatches(saberColor, data.color))
        {
            WasHit = true;
            Debug.Log($"[Note] Wrong Color! Saber: {saberColor}, Note: {data.color}");
            ScoreManager.Instance?.RegisterWrongColor(this);
            gameObject.SetActive(false);
            return false;
        }

        // 2. Check Velocity
        if (velocity < MinSliceVelocity)
        {
            Debug.Log($"[Note] Too slow! Velocity: {velocity:F2}");
            return false;
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
        return true;
    }
}
