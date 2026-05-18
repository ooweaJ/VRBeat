using UnityEngine;
using UnityEngine.XR;

public static class HapticFeedback
{
    public static void Pulse(Handedness hand, float amplitude, float duration)
    {
        var node = hand == Handedness.Left ? XRNode.LeftHand : XRNode.RightHand;
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), duration);
    }
}
