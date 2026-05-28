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
    public Transform Tip    => tip;
    public Transform Root   => root;

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
            float vel = Velocity.magnitude;
            Debug.Log($"[Saber] Hit detected with {other.name}. Velocity: {vel:F2}");

            bool sliced = note.OnSliced(Velocity.normalized, vel, color);
            if (sliced)
            {
                HapticFeedback.Pulse(handedness, 0.5f, 0.1f);
                EnvColorManager.Instance?.TriggerSlice(color);
            }
        }
    }
}
