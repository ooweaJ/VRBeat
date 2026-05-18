using UnityEngine;

public class NormalNote : NoteBase
{
    public const float MinSliceVelocity = 2.0f;
    public const float DirectionDotThreshold = 0.7f;

    public override void OnSliced(Vector3 sliceDir, float velocity, SaberColor saberColor)
    {
        if (WasHit) return;

        if (!ColorMatches(saberColor, data.color))
        {
            ScoreManager.Instance?.RegisterWrongColor(this);
            return;
        }

        if (velocity < MinSliceVelocity) return;

        bool correctDir = data.direction == "any" ||
                          Vector3.Dot(sliceDir.normalized, GetRequiredDirection()) > DirectionDotThreshold;

        WasHit = true;
        ScoreManager.Instance?.RegisterHit(this, correctDir, velocity);
        PlaySliceEffect(sliceDir);
        gameObject.SetActive(false);
    }

    void PlaySliceEffect(Vector3 dir)
    {
        SliceEffect.Play(transform.position, dir, data.color);
    }
}
