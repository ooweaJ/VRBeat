using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRRigSetup : MonoBehaviour
{
    [SerializeField] GameObject leftControllerPrefab;
    [SerializeField] GameObject rightControllerPrefab;

    void Awake()
    {
        ApplySettings();
    }

    void ApplySettings()
    {
        var settings = GameManager.Instance?.Settings;
        if (settings == null) return;

        // Mirror controller anchors if left-handed mode
        if (settings.leftHandedMode)
        {
            var scale = transform.localScale;
            scale.x = -1f;
            transform.localScale = scale;
        }
    }
}
