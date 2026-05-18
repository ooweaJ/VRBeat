using UnityEngine;

public abstract class NoteBase : MonoBehaviour
{
    protected NoteData data;
    protected float noteSpeed;
    protected float hitZ;

    public NoteData Data => data;
    public bool WasHit { get; protected set; }

    // Lane/row layout constants
    const float LaneWidth  = 0.6f;
    const float RowHeight  = 0.6f;
    const float BaseHeight = 0.8f;

    public virtual void Initialize(NoteData d, float speed, float hz)
    {
        data      = d;
        noteSpeed = speed;
        hitZ      = hz;
        WasHit    = false;
        ApplyDirectionRotation();
        UpdatePosition();
    }

    protected virtual void Update() => UpdatePosition();

    protected virtual void UpdatePosition()
    {
        float beatsRemaining   = data.beat - Conductor.Instance.SongBeat;
        float secondsRemaining = beatsRemaining * Conductor.Instance.SecondsPerBeat;
        float zPos             = hitZ + secondsRemaining * noteSpeed;

        Vector3 pos = LaneToWorldPos(data.lane, data.row);
        pos.z = zPos;
        transform.position = pos;
    }

    public virtual bool ShouldDespawn(float despawnZ) => transform.position.z < despawnZ;

    void ApplyDirectionRotation() =>
        transform.localRotation = DirectionToRotation(data.direction);

    public abstract void OnSliced(Vector3 sliceDirection, float velocity, SaberColor saberColor);

    protected static Vector3 LaneToWorldPos(int lane, int row)
    {
        float x = (lane - 1.5f) * LaneWidth;
        float y = BaseHeight + row * RowHeight;
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
