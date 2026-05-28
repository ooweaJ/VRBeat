using UnityEngine;

// 단일 빔/렌더러를 EnvColorManager 색에 동기화 — 박자 펄스 + 슬라이스 색반응 자동 반영.
public class EnvSyncedBeam : MonoBehaviour
{
    static readonly int ColorId = Shader.PropertyToID("_Color");
    MaterialPropertyBlock mpb;
    Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb  = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (rend == null) return;
        // SliceLightShow가 이 렌더러를 제어 중이면 색을 안 건드림 (자동 펄스 차단)
        if (SliceLightShow.Instance != null && SliceLightShow.Instance.IsControlled(rend)) return;
        var ec = EnvColorManager.Instance;
        if (ec == null) return;
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(ColorId, ec.GetCurrentColor());
        rend.SetPropertyBlock(mpb);
    }
}
