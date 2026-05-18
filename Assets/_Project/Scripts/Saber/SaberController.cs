using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SaberController : MonoBehaviour
{
    public SaberColor color;
    public Handedness handedness;

    [SerializeField] Transform tip;
    [SerializeField] Transform root;

    Vector3 lastTipPos;

    public Vector3 Velocity { get; private set; }
    public float Speed      => Velocity.magnitude;

    void Start()
    {
        lastTipPos = tip != null ? tip.position : transform.position;
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        Vector3 tipPos = tip != null ? tip.position : transform.position;
        Velocity = (tipPos - lastTipPos) / Time.deltaTime;

        // Tunneling prevention: raycast from last frame tip to current tip
        Vector3 dir  = tipPos - lastTipPos;
        float   dist = dir.magnitude;

        if (dist > 0.005f)
        {
            if (Physics.Raycast(lastTipPos, dir.normalized, out RaycastHit hit, dist))
            {
                TrySlice(hit.collider);
            }
        }

        lastTipPos = tipPos;
    }

    void OnTriggerEnter(Collider other) => TrySlice(other);

    void TrySlice(Collider other)
    {
        if (other.TryGetComponent(out NoteBase note))
        {
            note.OnSliced(Velocity.normalized, Velocity.magnitude, color);
            HapticFeedback.Pulse(handedness, 0.5f, 0.1f);
        }
    }
}
