using UnityEngine;

public class SliceEffect : MonoBehaviour
{
    static SliceEffect instance;

    [SerializeField] ParticleSystem redParticles;
    [SerializeField] ParticleSystem blueParticles;

    void Awake() => instance = this;

    public static void Play(Vector3 position, Vector3 direction, string noteColor)
    {
        if (instance == null) return;

        var ps = noteColor == "blue" ? instance.blueParticles : instance.redParticles;
        if (ps == null) return;

        ps.transform.position = position;
        ps.transform.forward  = direction;
        ps.Play();
    }
}
