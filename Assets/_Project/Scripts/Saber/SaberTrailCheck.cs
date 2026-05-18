using UnityEngine;

// Enables/disables the saber trail renderer based on swing speed.
[RequireComponent(typeof(TrailRenderer))]
public class SaberTrailCheck : MonoBehaviour
{
    [SerializeField] SaberController saber;
    [SerializeField] float trailThreshold = 1.5f;

    TrailRenderer trail;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        if (saber == null) saber = GetComponentInParent<SaberController>();
    }

    void Update()
    {
        trail.emitting = saber != null && saber.Speed >= trailThreshold;
    }
}
