using UnityEngine;

public enum FireNodeType
{
    Structure,
    Vegetation,
    Spark
}

public enum FireNodeState
{
    Off,
    Burning,
    Destroyed
}

public class FireObject : MonoBehaviour
{
    public FireNodeType nodeType = FireNodeType.Structure;
    public FireNodeState state = FireNodeState.Off;

    public Material unlitMaterial;
    public Material litMaterial;
    public Material destroyedMaterial;

    public float ignitionResistance = 0.5f;
    public float firePower = 5.0f;
    public float exposureDecayRate = 0.1f;
    public float timeToDestroy = 20.0f;
    public bool canBeDestroyed = true;
    public bool isCritical = false;

    [Header("Legacy Node Visual Effects")]
    public GameObject burningEffectPrefab;
    public Vector3 burningEffectLocalOffset = Vector3.zero;
    public Vector3 burningEffectLocalScale = Vector3.one;
    public bool parentBurningEffectToNode = true;
    public float burningEffectDestroyDelay = 3.0f;

    [Header("Vegetation Warning")]
    public bool blinkVegetationWhenBurning = true;
    public float vegetationBlinkInterval = 0.25f;

    [Range(0.0f, 1.0f)] public float fireIntensity = 0.0f;
    [HideInInspector] public float accumulatedExposure = 0.0f;

    // Legacy fields kept so existing scene references do not break immediately.
    [HideInInspector] public float burn_time = 5.0f;
    [HideInInspector] public float combustibility = 0.5f;
    [HideInInspector] public float explosion_radius = 0.0f;
    [HideInInspector] public bool is_burning = false;
    [HideInInspector] public bool is_burnt = false;
    [HideInInspector] public float burn_timer = 0.0f;

    private Renderer objectRenderer;
    private Material material;
    private float burningTimer = 0.0f;
    private GameObject activeBurningEffect;
    private float vegetationBlinkTimer = 0.0f;
    private bool vegetationBlinkUsesLitMaterial = true;
    private static readonly int BurnProgressId = Shader.PropertyToID("_BurnProgress");

    void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            material = objectRenderer.material;
        }
    }

    void Start()
    {
        SyncLegacyFlags();
        SetVisualState();
    }

    void Update()
    {
        UpdateVegetationBlink(Time.deltaTime);
    }

    public bool CanIgnite()
    {
        return state == FireNodeState.Off;
    }

    public bool IsBurning()
    {
        return state == FireNodeState.Burning;
    }

    public bool IsDestroyed()
    {
        return state == FireNodeState.Destroyed;
    }

    public bool AddExposure(float amount)
    {
        if (!CanIgnite() || amount <= 0.0f)
        {
            return false;
        }

        accumulatedExposure += amount;
        if (accumulatedExposure >= ignitionResistance)
        {
            Ignite();
            return true;
        }

        return false;
    }

    public void DecayExposure(float deltaTime)
    {
        if (state != FireNodeState.Off || accumulatedExposure <= 0.0f)
        {
            return;
        }

        accumulatedExposure -= exposureDecayRate * deltaTime;
        accumulatedExposure = Mathf.Max(0.0f, accumulatedExposure);
    }

    public void Ignite()
    {
        if (!CanIgnite())
        {
            return;
        }

        state = FireNodeState.Burning;
        fireIntensity = 1.0f;
        accumulatedExposure = 0.0f;
        burningTimer = 0.0f;
        burn_timer = 0.0f;
        vegetationBlinkTimer = 0.0f;
        vegetationBlinkUsesLitMaterial = true;
        SyncLegacyFlags();
        SetVisualState();
        Debug.Log($"{gameObject.name} has ignited!");

        if (nodeType == FireNodeType.Vegetation || isCritical)
        {
            Debug.LogWarning($"{gameObject.name} is a critical fire node.");
        }
    }

    public void BurnUpdate(float deltaTime)
    {
        if (state != FireNodeState.Burning)
        {
            return;
        }

        burningTimer += deltaTime;
        burn_timer = burningTimer;

        float progress = timeToDestroy > 0.0f ? Mathf.Clamp01(burningTimer / timeToDestroy) : 1.0f;
        SetBurnProgress(progress);

        if (ShouldDestroy(progress))
        {
            DestroyNode();
        }
    }

    public void BurnUpdate()
    {
        BurnUpdate(Time.deltaTime);
    }

    public float GetDestroyProgress()
    {
        if (timeToDestroy <= 0.0f)
        {
            return state == FireNodeState.Destroyed ? 1.0f : 0.0f;
        }

        return Mathf.Clamp01(burningTimer / timeToDestroy);
    }

    public float GetPercentCombusted()
    {
        return GetDestroyProgress();
    }

    public void DestroyNode()
    {
        if (state == FireNodeState.Destroyed)
        {
            return;
        }

        state = FireNodeState.Destroyed;
        fireIntensity = 0.0f;
        accumulatedExposure = 0.0f;
        vegetationBlinkTimer = 0.0f;
        SetBurnProgress(1.0f);
        SyncLegacyFlags();
        SetVisualState();
        StopBurningEffect();
        Debug.Log($"{gameObject.name} has been destroyed by fire.");
    }

    private void OnDisable()
    {
        StopBurningEffect();
    }

    private bool ShouldDestroy(float progress)
    {
        if (!canBeDestroyed)
        {
            return false;
        }

        return progress >= 1.0f;
    }

    private void SyncLegacyFlags()
    {
        is_burning = state == FireNodeState.Burning;
        is_burnt = state == FireNodeState.Destroyed;
    }

    private void SetVisualState()
    {
        if (objectRenderer == null)
        {
            return;
        }

        Material stateMaterial = null;
        if (state == FireNodeState.Burning)
        {
            stateMaterial = litMaterial;
        }
        else if (state == FireNodeState.Destroyed)
        {
            stateMaterial = destroyedMaterial;
        }
        else
        {
            stateMaterial = unlitMaterial;
        }

        if (stateMaterial == null)
        {
            return;
        }

        objectRenderer.material = stateMaterial;
        material = objectRenderer.material;
    }

    private void SetBurnProgress(float value)
    {
        if (material != null && material.HasProperty(BurnProgressId))
        {
            material.SetFloat(BurnProgressId, value);
        }
    }

    private void UpdateVegetationBlink(float deltaTime)
    {
        if (!blinkVegetationWhenBurning || nodeType != FireNodeType.Vegetation || state != FireNodeState.Burning)
        {
            return;
        }

        if (objectRenderer == null || litMaterial == null || unlitMaterial == null)
        {
            return;
        }

        vegetationBlinkTimer += deltaTime;
        if (vegetationBlinkTimer < vegetationBlinkInterval)
        {
            return;
        }

        vegetationBlinkTimer = 0.0f;
        vegetationBlinkUsesLitMaterial = !vegetationBlinkUsesLitMaterial;
        objectRenderer.material = vegetationBlinkUsesLitMaterial ? litMaterial : unlitMaterial;
        material = objectRenderer.material;
    }

    private void StartBurningEffect()
    {
        if (burningEffectPrefab == null || activeBurningEffect != null)
        {
            return;
        }

        if (parentBurningEffectToNode)
        {
            activeBurningEffect = Instantiate(burningEffectPrefab, transform);
            activeBurningEffect.transform.localPosition = burningEffectLocalOffset;
            activeBurningEffect.transform.localRotation = Quaternion.identity;
        }
        else
        {
            activeBurningEffect = Instantiate(
                burningEffectPrefab,
                transform.TransformPoint(burningEffectLocalOffset),
                transform.rotation);
        }

        activeBurningEffect.transform.localScale = burningEffectLocalScale;
    }

    private void StopBurningEffect()
    {
        if (activeBurningEffect == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = activeBurningEffect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(activeBurningEffect, burningEffectDestroyDelay);
        activeBurningEffect = null;
    }
}
