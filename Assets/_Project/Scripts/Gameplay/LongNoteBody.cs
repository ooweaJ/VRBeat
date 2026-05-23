using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LongNoteBody : MonoBehaviour
{
    [SerializeField] LongNote longNote;

    [SerializeField] float sliceStep = 0.15f; // 작을수록 더 자주/빨리 잘림

    [Header("대각선 양옆 갈라짐 연출")]
    [Tooltip("수평면 기준 위로 들어올린 각도. 60이면 양쪽 조각이 60°로 벌어져 위로 튐")]
    [SerializeField] float spreadAngle   = 60f;
    [SerializeField] float spreadForce   = 3.0f; // 대각선 바깥으로 밀어내는 힘
    [SerializeField] float backForce     = 0.8f; // 플레이어 쪽(뒤)으로 미는 힘
    [SerializeField] float spinTorque    = 5.0f; // 굴러가는 회전량
    [SerializeField] float chunkLifetime = 0.8f;

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

            // 먼저 바디를 잘라낸 뒤, 방금 제거된 구간([chunkStart, consumedLocalZ])에서
            // 조각을 생성 → 남은 바디와 겹치지 않고 절단면에서 자연스럽게 떨어져 나감
            TrimBodyFront(consumedLocalZ);
            SpawnChunk(chunkStart);
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

        // 조각 중심 = 방금 제거된 구간의 중심 = 새 절단면 바로 앞 (부모 로컬 Z)
        float chunkCenterZ = chunkStartLocalZ + sliceStep * 0.5f;
        Vector3 worldCenter = p != null
            ? p.TransformPoint(new Vector3(0f, 0f, chunkCenterZ))
            : new Vector3(0f, 0f, chunkCenterZ);

        // 조각 월드 크기 (바디 두께 = lossyScale.x, 길이 = sliceStep)
        float worldXY = transform.lossyScale.x;
        float worldZ  = p != null
            ? Mathf.Abs(p.lossyScale.z) * sliceStep
            : sliceStep;

        // 좌/우 반쪽 두 조각으로 갈라 60° 대각선으로 튕겨냄
        SpawnHalf(worldCenter, worldXY, worldZ, -1f); // 왼쪽 반쪽
        SpawnHalf(worldCenter, worldXY, worldZ, +1f); // 오른쪽 반쪽
    }

    // side: -1 = 왼쪽, +1 = 오른쪽 (노트 로컬 X 기준)
    void SpawnHalf(Vector3 fullWorldCenter, float worldXY, float worldZ, float side)
    {
        float   halfWidth = worldXY * 0.5f;
        Vector3 right     = transform.right;
        Vector3 up        = transform.up;

        // 반쪽 중심을 자기 쪽으로 1/4 폭 이동 → 가운데가 갈라진 모양
        Vector3 halfCenter = fullWorldCenter + right * (side * worldXY * 0.25f);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetPositionAndRotation(halfCenter, transform.rotation);
        go.transform.localScale = new Vector3(halfWidth, worldXY, worldZ);

        Destroy(go.GetComponent<Collider>()); // 시각 전용

        if (GetComponent<MeshRenderer>() is MeshRenderer mr)
            go.GetComponent<MeshRenderer>().material = mr.sharedMaterial;

        // ── 60° 대각선 좌우 분리 ──
        // 수평(right) 성분 cos, 수직(up) 성분 sin → spreadAngle 만큼 위로 들린 대각선
        float   rad     = spreadAngle * Mathf.Deg2Rad;
        Vector3 outward = right * (side * Mathf.Cos(rad)) + up * Mathf.Sin(rad);

        var rb = go.AddComponent<Rigidbody>(); // 중력 ON → 자연스럽게 포물선 낙하
        Vector3 force = outward * (spreadForce * Random.Range(0.9f, 1.1f))
                      + Vector3.back * backForce;
        rb.AddForce(force, ForceMode.VelocityChange);

        // 바깥으로 굴러 넘어가는 회전 (좌/우 반대), 약간의 랜덤만 가미
        Vector3 spin = transform.forward * (-side * spinTorque)
                     + Random.insideUnitSphere * (spinTorque * 0.25f);
        rb.AddTorque(spin, ForceMode.VelocityChange);

        Destroy(go, chunkLifetime);
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
