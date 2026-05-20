using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LongNoteBody : MonoBehaviour
{
    [SerializeField] LongNote longNote;

    [SerializeField] float sliceStep = 0.3f;

    Transform saberInside;
    float     totalLocalLength; // Initialize 후 body.localScale.z
    float     consumedLocalZ;   // 잘린 누적 길이 (부모 로컬 기준)

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (longNote == null) longNote = GetComponentInParent<LongNote>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<SaberController>(out var saber)) return;

        // 진입 시점에 현재 스케일 기준으로 초기화 (풀 재사용 대응)
        totalLocalLength = transform.localScale.z;
        consumedLocalZ   = 0f;
        saberInside      = saber.transform;

        longNote?.SetHeld(true);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent<SaberController>(out _)) return;
        longNote?.SetHeld(true);
        if (saberInside == null || totalLocalLength <= 0f) return;

        // 세이버 위치를 부모 로컬 공간으로 변환
        float saberParentZ = transform.parent != null
            ? transform.parent.InverseTransformPoint(saberInside.position).z
            : saberInside.position.z;

        // 소비 위치를 totalLocalLength 안으로 제한
        float targetZ = Mathf.Clamp(saberParentZ, 0f, totalLocalLength);

        while (targetZ - consumedLocalZ >= sliceStep)
        {
            float chunkStart = consumedLocalZ;
            consumedLocalZ  += sliceStep;

            SpawnChunk(chunkStart);
            TrimBodyFront(consumedLocalZ);
            PlayEffect();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<SaberController>(out _)) return;
        saberInside = null;
        longNote?.SetHeld(false);
    }

    // ── 핵심 로직 ──────────────────────────────────────────

    void SpawnChunk(float chunkStartLocalZ)
    {
        Transform p = transform.parent;

        // 조각 중심 = chunkStart + sliceStep/2 (부모 로컬 Z)
        float chunkCenterZ = chunkStartLocalZ + sliceStep * 0.5f;

        // 부모 로컬 → 월드
        Vector3 worldCenter = p != null
            ? p.TransformPoint(new Vector3(0f, 0f, chunkCenterZ))
            : new Vector3(0f, 0f, chunkCenterZ);

        // 조각 월드 크기 계산
        float worldXY = transform.lossyScale.x;
        float worldZ  = p != null
            ? Mathf.Abs(p.lossyScale.z) * sliceStep
            : sliceStep;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetPositionAndRotation(worldCenter, transform.rotation);
        go.transform.localScale = new Vector3(worldXY, worldXY, worldZ);

        // 충돌체 제거 (시각 전용)
        Destroy(go.GetComponent<Collider>());

        // 바디와 같은 머티리얼 사용
        if (GetComponent<MeshRenderer>() is MeshRenderer mr)
            go.GetComponent<MeshRenderer>().material = mr.sharedMaterial;

        // 물리 — 플레이어 방향 + 약간 랜덤
        var rb = go.AddComponent<Rigidbody>();
        rb.AddForce(Vector3.back * 3f + Random.insideUnitSphere * 1.2f,
                    ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);

        Destroy(go, 0.6f);
    }

    void TrimBodyFront(float newConsumedLocalZ)
    {
        float newLength = totalLocalLength - newConsumedLocalZ;
        if (newLength < 0.05f) { gameObject.SetActive(false); return; }

        // 뒷면(tail 쪽)은 고정, 앞면만 소비된 만큼 뒤로 이동
        float newCenterZ = newConsumedLocalZ + newLength * 0.5f;
        transform.localPosition = new Vector3(0f, 0f, newCenterZ);
        transform.localScale    = new Vector3(
            transform.localScale.x,
            transform.localScale.y,
            newLength);
    }

    void PlayEffect()
    {
        if (saberInside == null) return;
        string color = longNote?.Data?.color ?? "red";
        SliceEffect.Play(saberInside.position, Vector3.back, color);
    }
}
