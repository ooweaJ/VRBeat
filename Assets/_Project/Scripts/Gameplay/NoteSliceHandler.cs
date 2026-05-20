using EzySlice;
using UnityEngine;

public class NoteSliceHandler : MonoBehaviour
{
    [SerializeField] Material crossSectionMaterial;
    [SerializeField] GameObject sliceTarget; // 미지정 시 this.gameObject 사용 (LongNote 헤드 등에 활용)

    const float SpreadSpeed  = 3f;
    const float SideSpread   = 1.5f;
    const float ForwardSpeed = 0.5f;
    const float Lifetime     = 0.6f;

    public void Slice(Vector3 worldSliceDir, Material noteMat)
    {
        GameObject target = sliceTarget != null ? sliceTarget : gameObject;

        SlicedHull hull = target.Slice(target.transform.position, worldSliceDir, crossSectionMaterial);
        if (hull == null) return;

        GameObject upper = hull.CreateUpperHull(target, noteMat);
        GameObject lower = hull.CreateLowerHull(target, noteMat);

        LaunchHalf(upper, +1f, worldSliceDir.normalized);
        LaunchHalf(lower, -1f, worldSliceDir.normalized);
    }

    void LaunchHalf(GameObject half, float side, Vector3 sliceDir)
    {
        if (half == null) return;

        // sliceDir에 수직인 방향(XY 평면 기준)으로 두 조각이 벌어짐
        Vector3 perp = Vector3.Cross(sliceDir, Vector3.back).normalized;
        if (perp.sqrMagnitude < 0.01f)
            perp = Vector3.Cross(sliceDir, Vector3.up).normalized;

        var rb = half.AddComponent<Rigidbody>();
        rb.AddForce(
            sliceDir  * SpreadSpeed          // 스윙 방향으로 날아감
            + perp    * side * SideSpread    // 수직으로 벌어짐
            + Vector3.back * ForwardSpeed,   // 약간 앞으로
            ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);

        Destroy(half, Lifetime);
    }
}
