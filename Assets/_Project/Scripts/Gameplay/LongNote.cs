using UnityEngine;

public class LongNote : NoteBase
{
    [SerializeField] Transform head;
    [SerializeField] Transform tail;
    [SerializeField] Transform body;
    [SerializeField] NoteSliceHandler sliceHandler;

    [Tooltip("바디 두께 배율 (1=원본, 작을수록 얇음)")]
    [SerializeField] float bodyThickness = 0.5f;

    bool isHeld;
    float holdTimer;
    const float HoldScoreInterval = 0.1f;

    // 풀 재사용 시 두께 배율이 누적되지 않도록 원본 x/y 캐시
    float baseBodyX;
    float baseBodyY;

    void Awake()
    {
        // 루트에 큐브 메시가 있으면 Head 구체를 가리므로 숨김
        var rootRenderer = GetComponent<MeshRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

        if (body != null)
        {
            baseBodyX = body.localScale.x;
            baseBodyY = body.localScale.y;
        }
    }

    public override void Initialize(NoteData d, float speed, float hz, GameConfig cfg)
    {
        base.Initialize(d, speed, hz, cfg);
        isHeld    = false;
        holdTimer = 0f;

        float lengthInSeconds = d.duration * Conductor.Instance.SecondsPerBeat;
        float lengthInMeters  = lengthInSeconds * speed;

        // 프리롤 스케일(0.1→0.4)이 걸려도 원본 루트 스케일 기준으로 로컬 길이 환산
        float rootScaleZ  = Mathf.Abs(fullScale.z);
        float localLength = rootScaleZ > 0f ? lengthInMeters / rootScaleZ : lengthInMeters;

        if (body != null)
        {
            // body 앞면이 Head(z=0)와 정렬되도록 center를 localLength/2 로 이동
            // x/y 는 원본 기준 bodyThickness 배율로 얇게 (재사용 시 누적 방지)
            body.localScale    = new Vector3(
                baseBodyX * bodyThickness,
                baseBodyY * bodyThickness,
                localLength);
            body.localPosition = new Vector3(0f, 0f, localLength / 2f);
        }
        if (tail != null)
            tail.localPosition = new Vector3(0f, 0f, localLength);

        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        Material mat = LoadMaterial(data.color.ToLower());
        if (mat == null) return;

        ApplyMat(head, mat);
        ApplyMat(body, mat);
        ApplyMat(tail, mat);
    }

    static Material LoadMaterial(string colorName)
    {
#if UNITY_EDITOR
        string path = colorName == "red" ? "Assets/Material/matR.mat" : "Assets/Material/matB.mat";
        var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
#endif
        // 런타임 폴백
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = colorName == "red" ? Color.red : Color.blue;
        return m;
    }

    static void ApplyMat(Transform t, Material mat)
    {
        if (t == null) return;
        var r = t.GetComponent<Renderer>() ?? t.GetComponentInChildren<Renderer>();
        if (r != null) r.material = mat;
    }

    protected override void Update()
    {
        base.Update();

        if (isHeld)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= HoldScoreInterval)
            {
                holdTimer -= HoldScoreInterval;
                ScoreManager.Instance?.AddHoldScore(10);
            }
        }
    }

    public override bool ShouldDespawn(float despawnZ)
    {
        // Duration이 끝나기 전에는 절대 despawn 안 함
        if (Conductor.Instance != null &&
            Conductor.Instance.SongBeat < data.beat + data.duration)
            return false;

        if (tail == null) return base.ShouldDespawn(despawnZ);
        return tail.position.z < despawnZ;
    }

    public override bool OnSliced(Vector3 sliceDir, float velocity, SaberColor saberColor)
    {
        if (WasHit || isHeld) return false;
        if (!ColorMatches(saberColor, data.color)) return false;

        WasHit = true;
        isHeld = true;
        ScoreManager.Instance?.RegisterHit(this, true, velocity);

        // 헤드 슬라이싱 — sliceHandler.sliceTarget 을 head 오브젝트로 지정할 것
        if (sliceHandler == null) sliceHandler = GetComponent<NoteSliceHandler>();
        Material headMat = head != null
            ? (head.GetComponent<Renderer>() ?? head.GetComponentInChildren<Renderer>())?.sharedMaterial
            : null;
        sliceHandler?.Slice(sliceDir, headMat);

        SliceEffect.Play(transform.position, sliceDir, data.color);

        // 헤드만 숨기고 바디/꼬리는 홀드 중 유지
        if (head != null) head.gameObject.SetActive(false);
        return true;
    }

    public void SetHeld(bool held) => isHeld = held;
}
