using System.Collections.Generic;
using UnityEngine;

public class SliceLightShow : MonoBehaviour
{
    public static SliceLightShow Instance { get; private set; }

    [System.Serializable]
    public class RendererGroup
    {
        public string name;
        public Renderer[] renderers;
    }

    [Header("Upper Lasers (sequential groups)")]
    public RendererGroup[] upperGroups;

    [Header("Legacy Upper Beams")]
    public Renderer[] upperBeams;

    [Header("Rear Lasers (random 2-3)")]
    public Renderer[] rearBeams;
    [Range(1, 10)] public int rearLightUpMin = 2;
    [Range(1, 10)] public int rearLightUpMax = 3;

    [Header("Rings (matching color group)")]
    public Renderer[] redRings;
    public Renderer[] blueRings;

    [Header("Timing")]
    [Range(0.1f, 2f)] public float flashDuration = 0.5f;

    [Header("Colors (HDR)")]
    [ColorUsage(true, true)] public Color redColor = new Color(9.5f, 0.95f, 0.45f);
    [ColorUsage(true, true)] public Color blueColor = new Color(0.12f, 3.20f, 16.0f);

    [Header("Rear Laser Colors (sharp)")]
    [ColorUsage(true, true)] public Color rearRedColor = new Color(9.5f, 0.95f, 0.45f);
    [ColorUsage(true, true)] public Color rearBlueColor = new Color(0.45f, 0.95f, 9.5f);

    [Header("Ring Colors (wide glow)")]
    [ColorUsage(true, true)] public Color ringRedColor = new Color(16.0f, 0.12f, 3.20f);
    [ColorUsage(true, true)] public Color ringBlueColor = new Color(0.12f, 3.20f, 16.0f);

    static readonly string[] UpperGroupNames =
    {
        "DiagonalLasers_Right",
        "DiagonalLasers_Left",
        "CrossLasers",
        "ChevronLasers",
    };

    static readonly int ColorProp = Shader.PropertyToID("_Color");
    static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorProp = Shader.PropertyToID("_EmissionColor");

    readonly HashSet<Renderer> controlled = new HashSet<Renderer>();
    readonly Dictionary<Renderer, FlashState> active = new Dictionary<Renderer, FlashState>();
    readonly List<Renderer> doneScratch = new List<Renderer>();
    readonly List<Renderer> keyScratch = new List<Renderer>();
    readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    MaterialPropertyBlock mpb;
    Material lightShowMaterial;
    int upperIdx;
    float cycleRemaining;

    struct FlashState
    {
        public Color color;
        public float remaining;
    }

    void Awake()
    {
        Instance = this;
        mpb = new MaterialPropertyBlock();
        AutoWireMissingReferences();
        RegisterAll();
        AssignLightShowMaterials();
    }

    void OnDestroy()
    {
        RestoreOriginalMaterials();
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        foreach (var r in controlled) SetRendererColor(r, Color.black);
    }

    void OnEnable()
    {
        EnvColorManager.OnSliceEvent += HandleSlice;
    }

    void OnDisable()
    {
        EnvColorManager.OnSliceEvent -= HandleSlice;
    }

    public bool IsControlled(Renderer r) => r != null && controlled.Contains(r);

    void HandleSlice(SaberColor saberColor)
    {
        if (cycleRemaining > 0f) return;

        Color upperColor = saberColor == SaberColor.Red ? redColor : blueColor;
        Color rearColor = saberColor == SaberColor.Red ? rearRedColor : rearBlueColor;
        Color ringColor = saberColor == SaberColor.Red ? ringRedColor : ringBlueColor;

        TriggerNextUpperGroup(upperColor);
        TriggerRearRandom(rearColor);
        TriggerRingGroup(saberColor, ringColor);
        cycleRemaining = flashDuration;
    }

    void TriggerNextUpperGroup(Color flashColor)
    {
        if (upperGroups != null && upperGroups.Length > 0)
        {
            RendererGroup group = upperGroups[upperIdx % upperGroups.Length];
            upperIdx = (upperIdx + 1) % upperGroups.Length;
            TriggerGroupFlash(group);
            return;
        }

        if (upperBeams == null || upperBeams.Length == 0) return;

        Renderer beam = upperBeams[upperIdx % upperBeams.Length];
        upperIdx = (upperIdx + 1) % upperBeams.Length;
        TriggerFlash(beam, flashColor);

        void TriggerGroupFlash(RendererGroup group)
        {
            if (group?.renderers == null) return;
            for (int i = 0; i < group.renderers.Length; i++)
                TriggerFlash(group.renderers[i], flashColor);
        }
    }

    void TriggerRearRandom(Color flashColor)
    {
        if (rearBeams == null || rearBeams.Length == 0) return;

        int min = Mathf.Clamp(Mathf.Min(rearLightUpMin, rearLightUpMax), 1, rearBeams.Length);
        int max = Mathf.Clamp(Mathf.Max(rearLightUpMin, rearLightUpMax), min, rearBeams.Length);
        int pickCount = Random.Range(min, max + 1);

        int[] pool = new int[rearBeams.Length];
        for (int i = 0; i < pool.Length; i++) pool[i] = i;

        for (int n = 0; n < pickCount; n++)
        {
            int swapIdx = Random.Range(n, pool.Length);
            (pool[n], pool[swapIdx]) = (pool[swapIdx], pool[n]);
            TriggerFlash(rearBeams[pool[n]], flashColor);
        }
    }

    void TriggerRingGroup(SaberColor saberColor, Color flashColor)
    {
        Renderer[] rings = saberColor == SaberColor.Red ? redRings : blueRings;
        if (rings == null) return;

        for (int i = 0; i < rings.Length; i++)
            TriggerFlash(rings[i], flashColor);
    }

    void TriggerFlash(Renderer r, Color c)
    {
        if (r == null) return;

        SetRendererColor(r, c);
        active[r] = new FlashState { color = c, remaining = flashDuration };
    }

    void Update()
    {
        if (cycleRemaining > 0f)
            cycleRemaining -= Time.deltaTime;

        if (active.Count == 0) return;

        doneScratch.Clear();
        keyScratch.Clear();
        keyScratch.AddRange(active.Keys);

        for (int i = 0; i < keyScratch.Count; i++)
        {
            Renderer r = keyScratch[i];
            FlashState st = active[r];
            st.remaining -= Time.deltaTime;

            if (st.remaining <= 0f)
            {
                SetRendererColor(r, Color.black);
                doneScratch.Add(r);
            }
            else
            {
                active[r] = st;
            }
        }

        for (int i = 0; i < doneScratch.Count; i++)
            active.Remove(doneScratch[i]);
    }

    void RegisterAll()
    {
        controlled.Clear();
        AddGroups(controlled, upperGroups);
        AddRange(controlled, upperBeams);
        AddRange(controlled, rearBeams);
        AddRange(controlled, redRings);
        AddRange(controlled, blueRings);
    }

    void AssignLightShowMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                        Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Unlit/Color") ??
                        Shader.Find("Standard");
        if (shader == null) return;

        lightShowMaterial = new Material(shader)
        {
            name = "Runtime_SliceLightShow_Neon",
            color = Color.black,
        };
        lightShowMaterial.EnableKeyword("_EMISSION");
        if (lightShowMaterial.HasProperty(ColorProp))
            lightShowMaterial.SetColor(ColorProp, Color.black);
        if (lightShowMaterial.HasProperty(BaseColorProp))
            lightShowMaterial.SetColor(BaseColorProp, Color.black);
        if (lightShowMaterial.HasProperty(EmissionColorProp))
            lightShowMaterial.SetColor(EmissionColorProp, Color.black);

        foreach (Renderer r in controlled)
        {
            if (r == null || originalMaterials.ContainsKey(r)) continue;

            originalMaterials[r] = r.sharedMaterials;
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = lightShowMaterial;
            r.sharedMaterials = mats;
        }
    }

    void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
        {
            if (entry.Key != null)
                entry.Key.sharedMaterials = entry.Value;
        }

        originalMaterials.Clear();

        if (lightShowMaterial != null)
        {
            Destroy(lightShowMaterial);
            lightShowMaterial = null;
        }
    }

    void AutoWireMissingReferences()
    {
        if (upperGroups == null || upperGroups.Length == 0)
            upperGroups = CollectUpperGroups();

        if (rearBeams == null || rearBeams.Length == 0)
            rearBeams = CollectRenderers("MovingUpLasers");

        if ((redRings == null || redRings.Length == 0) &&
            (blueRings == null || blueRings.Length == 0))
        {
            (redRings, blueRings) = CollectRingsByColor("Rings");
        }
    }

    RendererGroup[] CollectUpperGroups()
    {
        var groups = new List<RendererGroup>();
        for (int i = 0; i < UpperGroupNames.Length; i++)
        {
            Renderer[] renderers = CollectRenderers(UpperGroupNames[i]);
            if (renderers.Length == 0) continue;

            groups.Add(new RendererGroup
            {
                name = UpperGroupNames[i],
                renderers = renderers,
            });
        }

        return groups.ToArray();
    }

    static Renderer[] CollectRenderers(string rootName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) return new Renderer[0];

        var list = new List<Renderer>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            if (r != null) list.Add(r);
        return list.ToArray();
    }

    static (Renderer[] red, Renderer[] blue) CollectRingsByColor(string rootName)
    {
        var red = new List<Renderer>();
        var blue = new List<Renderer>();
        GameObject ringsRoot = GameObject.Find(rootName);
        if (ringsRoot == null) return (red.ToArray(), blue.ToArray());

        for (int i = 0; i < ringsRoot.transform.childCount; i++)
        {
            Transform pivot = ringsRoot.transform.GetChild(i);
            if (!pivot.name.StartsWith("Ring_")) continue;

            List<Renderer> target = (i & 1) == 0 ? red : blue;
            foreach (var r in pivot.GetComponentsInChildren<Renderer>(true))
                if (r != null) target.Add(r);
        }

        return (red.ToArray(), blue.ToArray());
    }

    void SetRendererColor(Renderer r, Color c)
    {
        if (r == null) return;

        r.GetPropertyBlock(mpb);
        mpb.SetColor(ColorProp, c);
        mpb.SetColor(BaseColorProp, c);
        mpb.SetColor(EmissionColorProp, c);
        r.SetPropertyBlock(mpb);
    }

    static void AddRange(HashSet<Renderer> set, Renderer[] arr)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null) set.Add(arr[i]);
    }

    static void AddGroups(HashSet<Renderer> set, RendererGroup[] groups)
    {
        if (groups == null) return;

        for (int i = 0; i < groups.Length; i++)
            if (groups[i] != null) AddRange(set, groups[i].renderers);
    }
}
