using UnityEngine;

// Optional additional per-note hit detection component placed on note colliders.
// Works alongside SaberController's trigger/raycast system.
public class SaberHitDetector : MonoBehaviour
{
    NoteBase note;

    void Awake() => note = GetComponent<NoteBase>() ?? GetComponentInParent<NoteBase>();

    void OnTriggerEnter(Collider other)
    {
        if (note == null) return;
        if (other.TryGetComponent(out SaberController saber))
        {
            bool sliced = note.OnSliced(saber.Velocity.normalized, saber.Velocity.magnitude, saber.color);
            if (sliced)
            {
                HapticFeedback.Pulse(saber.handedness, 0.5f, 0.1f);
                EnvColorManager.Instance?.TriggerSlice(saber.color);
            }
        }
    }
}
