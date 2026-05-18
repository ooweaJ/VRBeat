using UnityEngine;

// Attach to the body child of a LongNote prefab (with a stretched BoxCollider trigger).
// Maintains the hold state while the saber stays inside.
[RequireComponent(typeof(Collider))]
public class LongNoteBody : MonoBehaviour
{
    [SerializeField] LongNote longNote;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (longNote == null) longNote = GetComponentInParent<LongNote>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<SaberController>(out _))
            longNote?.SetHeld(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<SaberController>(out _))
            longNote?.SetHeld(false);
    }
}
