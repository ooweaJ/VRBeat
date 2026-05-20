using UnityEngine;

public class LongNote : NoteBase
{
    [SerializeField] Transform head;
    [SerializeField] Transform tail;
    [SerializeField] Transform body;

    bool isHeld;
    float holdTimer;
    const float HoldScoreInterval = 0.1f; // score tick every 100ms

    public override void Initialize(NoteData d, float speed, float hz, GameConfig cfg)
    {
        base.Initialize(d, speed, hz, cfg);
        isHeld    = false;
        holdTimer = 0f;

        float lengthInSeconds = d.duration * Conductor.Instance.SecondsPerBeat;
        float lengthInMeters  = lengthInSeconds * speed;

        if (body != null)
            body.localScale = new Vector3(body.localScale.x, body.localScale.y, lengthInMeters);
        if (tail != null)
            tail.localPosition = new Vector3(0, 0, lengthInMeters);
    }

    protected override void Update()
    {
        base.Update(); // moves head position

        if (isHeld)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= HoldScoreInterval)
            {
                holdTimer -= HoldScoreInterval;
                ScoreManager.Instance?.AddHoldScore(10);
            }
        }
    }

    public override bool ShouldDespawn(float despawnZ)
    {
        // Keep alive until the tail passes
        if (tail == null) return base.ShouldDespawn(despawnZ);
        return tail.position.z < despawnZ;
    }

    public override void OnSliced(Vector3 sliceDir, float velocity, SaberColor saberColor)
    {
        if (WasHit || isHeld) return;
        if (!ColorMatches(saberColor, data.color)) return;

        WasHit = true;
        isHeld = true;
        ScoreManager.Instance?.RegisterHit(this, true, velocity);
    }

    // Called by LongNoteBody trigger
    public void SetHeld(bool held) => isHeld = held;
}
