using UnityEngine;

public abstract class NoteBase : MonoBehaviour
{
    protected NoteData data;
    protected float noteSpeed;
    protected float hitZ;
    protected GameConfig config;
    protected Vector3 fullScale;
    bool hasFullScale;

    public NoteData Data => data;
    public bool WasHit { get; protected set; }

    public virtual void Initialize(NoteData d, float speed, float hz, GameConfig cfg)
    {
        data      = d;
        noteSpeed = speed;
        hitZ      = hz;
        config    = cfg;
        WasHit    = false;
        if (!hasFullScale)
        {
            fullScale = transform.localScale;
            hasFullScale = true;
        }
        else
        {
            transform.localScale = fullScale;
        }

        ApplyDirectionRotation();
        UpdatePosition();
    }

    protected virtual void Update() => UpdatePosition();

    protected virtual void UpdatePosition()
    {
        if (Conductor.Instance == null) return;

        float beatsRemaining   = data.beat - Conductor.Instance.SongBeat;
        float secondsRemaining = beatsRemaining * Conductor.Instance.SecondsPerBeat;
        float syncStartDist    = config != null ? config.noteSyncStartDistance : 20f;
        float spawnDist        = config != null ? config.spawnDistance : 40f;
        float preSyncDuration  = config != null ? config.notePreSyncDuration : 0.35f;
        float syncLeadSeconds  = syncStartDist / noteSpeed;

        float zPos;
        float scaleMultiplier;

        if (preSyncDuration > 0f && secondsRemaining > syncLeadSeconds)
        {
            float spawnLeadSeconds = syncLeadSeconds + preSyncDuration;
            float t = Mathf.InverseLerp(spawnLeadSeconds, syncLeadSeconds, secondsRemaining);
            t = Mathf.SmoothStep(0f, 1f, t);

            zPos = hitZ + Mathf.Lerp(spawnDist, syncStartDist, t);

            float spawnScale = config != null ? config.noteSpawnScale : 0.25f;
            float syncScale = config != null ? config.noteSyncScale : 1f;
            scaleMultiplier = Mathf.Lerp(spawnScale, syncScale, t);
        }
        else
        {
            zPos = hitZ + secondsRemaining * noteSpeed;
            scaleMultiplier = config != null ? config.noteSyncScale : 1f;
        }

        Vector3 pos = LaneToWorldPos(data.lane, data.row);
        pos.z = zPos;
        transform.position = pos;
        transform.localScale = fullScale * scaleMultiplier;
    }

    public virtual bool ShouldDespawn(float despawnZ) => transform.position.z < despawnZ;

    void ApplyDirectionRotation() =>
        transform.localRotation = DirectionToRotation(data.direction);

    public abstract bool OnSliced(Vector3 sliceDirection, float velocity, SaberColor saberColor);

    protected Vector3 LaneToWorldPos(int lane, int row)
    {
        float laneW = config != null ? config.laneWidth : 0.6f;
        float rowH  = config != null ? config.rowHeight  : 0.6f;
        float baseH = config != null ? config.baseHeight : 0.8f;

        float x = (lane - 1.5f) * laneW;
        float y = baseH + row * rowH;
        return new Vector3(x, y, 0f);
    }

    protected static bool ColorMatches(SaberColor saberColor, string noteColor)
    {
        return saberColor == SaberColor.Red   && noteColor == "red"  ||
               saberColor == SaberColor.Blue  && noteColor == "blue";
    }

    static Quaternion DirectionToRotation(string dir)
    {
        return dir switch
        {
            "up"        => Quaternion.identity,
            "down"      => Quaternion.Euler(0, 0, 180),
            "left"      => Quaternion.Euler(0, 0, 90),
            "right"     => Quaternion.Euler(0, 0, -90),
            "upLeft"    => Quaternion.Euler(0, 0, 45),
            "upRight"   => Quaternion.Euler(0, 0, -45),
            "downLeft"  => Quaternion.Euler(0, 0, 135),
            "downRight" => Quaternion.Euler(0, 0, -135),
            _           => Quaternion.identity, // "any"
        };
    }

    protected Vector3 GetRequiredDirection()
    {
        return data.direction switch
        {
            "up"        => Vector3.up,
            "down"      => Vector3.down,
            "left"      => Vector3.left,
            "right"     => Vector3.right,
            "upLeft"    => (Vector3.up   + Vector3.left).normalized,
            "upRight"   => (Vector3.up   + Vector3.right).normalized,
            "downLeft"  => (Vector3.down + Vector3.left).normalized,
            "downRight" => (Vector3.down + Vector3.right).normalized,
            _           => Vector3.up, // "any"
        };
    }
}
