using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class FireNodeBase : MonoBehaviour, IFireWaterTarget
{
    [Header("Node")]
    public FireNodeState state = FireNodeState.Off;
    public bool canReignite = false;

    [Header("Scene Label")]
    public bool showSceneLabel = true;
    public string sceneLabel;
    public Vector3 sceneLabelOffset = Vector3.up;
    public Color sceneLabelColor = Color.white;

    [Header("Fire Intensity")]
    [Range(0.0f, 1.0f)] public float fireIntensity = 0.0f;
    public float growthRate = 0.35f;
    public float coolingRate = 0.12f;
    public float extinguishRate = 0.85f;
    public float minimumVisibleIntensity = 0.12f;
    public float extinguishedIntensityThreshold = 0.03f;
    [Range(0.0f, 1.0f)] public float burningIntensityThreshold = 0.95f;
    [Range(0.0f, 0.95f)] public float heatResistanceWhileWet = 0.35f;
    public float heatInputDecayRate = 8.0f;
    public float waterInputDecayRate = 8.0f;

    [Header("Fire Interaction")]
    public bool canReceiveWater = true;

    [Header("Visual Effect")]
    public bool enableBurningEffect = true;
    public GameObject burningEffectPrefab;
    public Vector3 burningEffectLocalOffset = Vector3.zero;
    public Vector3 burningEffectMinimumScale = Vector3.one * 0.05f;
    public Vector3 burningEffectMaximumScale = Vector3.one;
    public float burningEffectDestroyDelay = 1.0f;

    private GameObject activeBurningEffect;
    protected float heatInput;
    protected float waterInput;
    protected bool hasShownVisibleFlame;

    public bool IsHeatSource => fireIntensity > minimumVisibleIntensity && !IsFinalState();
    public bool HasActiveFire => fireIntensity > extinguishedIntensityThreshold && !IsFinalState();
    public bool HasEverIgnited { get; private set; }
    protected bool ReceivesWater => canReceiveWater && enableBurningEffect && waterInput > 0.0f;
    protected bool ReceivesHeat => heatInput > 0.0f;

    public virtual bool CanPropagateHeat()
    {
        return IsHeatSource;
    }

    public virtual bool CanReceiveHeat()
    {
        return !IsFinalState() && (state != FireNodeState.Extinguished || canReignite);
    }

    public virtual void ApplyHeat(float amount)
    {
        if (!CanReceiveHeat())
        {
            return;
        }

        heatInput = Mathf.Max(heatInput, Mathf.Max(0.0f, amount));
    }

    public virtual void ApplyWater(float amount)
    {
        if (!canReceiveWater || !enableBurningEffect || IsFinalState() || fireIntensity <= 0.0f)
        {
            return;
        }

        waterInput = Mathf.Max(waterInput, Mathf.Max(0.0f, amount));
    }

    [ContextMenu("Debug Apply Heat")]
    private void DebugApplyHeat()
    {
        ApplyHeat(1.0f);
    }

    [ContextMenu("Debug Apply Water")]
    private void DebugApplyWater()
    {
        ApplyWater(1.0f);
    }

    protected virtual void OnValidate()
    {
        NormalizeThresholds();
    }

    protected virtual void Start()
    {
        NormalizeThresholds();
        InitializeNode();
        UpdateBurningEffect();
        UpdateBurningEffectScale();
    }

    protected virtual void Update()
    {
        float deltaTime = Time.deltaTime;
        UpdateFireState(deltaTime);
        UpdateBurningEffect();
        UpdateBurningEffectScale();
        DecayInputs(deltaTime);
    }

    protected virtual void InitializeNode()
    {
        fireIntensity = Mathf.Clamp01(fireIntensity);
        hasShownVisibleFlame = fireIntensity >= minimumVisibleIntensity;
        HasEverIgnited = hasShownVisibleFlame;
        state = hasShownVisibleFlame ? FireNodeState.Heating : FireNodeState.Off;
    }

    protected abstract void UpdateFireState(float deltaTime);

    protected void MarkAsIgnited()
    {
        hasShownVisibleFlame = true;
        HasEverIgnited = true;
    }

    protected virtual bool IsFinalState()
    {
        return false;
    }

    protected void UpdateHeatDependentFire(float deltaTime, bool extinguishedIsFinal)
    {
        if (state == FireNodeState.Extinguished && !canReignite)
        {
            fireIntensity = 0.0f;
            return;
        }

        if (ReceivesWater)
        {
            state = FireNodeState.Extinguishing;
            float wetExtinguishRate = extinguishRate * waterInput;
            if (ReceivesHeat)
            {
                wetExtinguishRate *= 1.0f - Mathf.Clamp01(heatResistanceWhileWet * heatInput);
            }

            fireIntensity -= wetExtinguishRate * deltaTime;
        }
        else if (ReceivesHeat)
        {
            state = fireIntensity >= burningIntensityThreshold ? FireNodeState.Burning : FireNodeState.Heating;
            fireIntensity += growthRate * heatInput * deltaTime;
        }
        else if (fireIntensity > extinguishedIntensityThreshold)
        {
            state = FireNodeState.Cooling;
            fireIntensity -= coolingRate * deltaTime;
        }
        else
        {
            state = hasShownVisibleFlame ? FireNodeState.Extinguished : FireNodeState.Off;
        }

        fireIntensity = Mathf.Clamp01(fireIntensity);

        if (fireIntensity >= minimumVisibleIntensity)
        {
            hasShownVisibleFlame = true;
            HasEverIgnited = true;
        }

        if (hasShownVisibleFlame && fireIntensity <= extinguishedIntensityThreshold)
        {
            if (extinguishedIsFinal)
            {
                ExtinguishPermanently();
            }
            else
            {
                ExtinguishForNow();
            }

            return;
        }

        if (!hasShownVisibleFlame && fireIntensity <= 0.0f)
        {
            fireIntensity = 0.0f;
            state = FireNodeState.Off;
        }
        else if (fireIntensity >= burningIntensityThreshold && state != FireNodeState.Extinguishing)
        {
            state = FireNodeState.Burning;
        }
    }

    protected virtual void ExtinguishPermanently()
    {
        fireIntensity = 0.0f;
        canReignite = false;
        state = FireNodeState.Extinguished;
        StopBurningEffect();
    }

    protected virtual void ExtinguishForNow()
    {
        fireIntensity = 0.0f;
        state = FireNodeState.Extinguished;
        hasShownVisibleFlame = false;
        StopBurningEffect();
    }

    protected void NormalizeThresholds()
    {
        extinguishedIntensityThreshold = Mathf.Clamp01(extinguishedIntensityThreshold);
        minimumVisibleIntensity = Mathf.Clamp01(Mathf.Max(minimumVisibleIntensity, extinguishedIntensityThreshold + 0.001f));
        burningIntensityThreshold = Mathf.Clamp01(Mathf.Max(burningIntensityThreshold, minimumVisibleIntensity));
    }

    private void UpdateBurningEffect()
    {
        bool shouldShowEffect = enableBurningEffect && burningEffectPrefab != null && fireIntensity > minimumVisibleIntensity;
        if (shouldShowEffect && activeBurningEffect == null)
        {
            activeBurningEffect = Instantiate(burningEffectPrefab, transform);
            activeBurningEffect.transform.localPosition = burningEffectLocalOffset;
            activeBurningEffect.transform.localRotation = Quaternion.identity;
        }
        else if (!shouldShowEffect && activeBurningEffect != null)
        {
            StopBurningEffect();
        }
    }

    private void UpdateBurningEffectScale()
    {
        if (activeBurningEffect == null)
        {
            return;
        }

        activeBurningEffect.transform.localScale = Vector3.Lerp(
            burningEffectMinimumScale,
            burningEffectMaximumScale,
            Mathf.Clamp01(fireIntensity));
    }

    protected void StopBurningEffect()
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

        Destroy(activeBurningEffect, Mathf.Max(0.0f, burningEffectDestroyDelay));
        activeBurningEffect = null;
    }

    private void DecayInputs(float deltaTime)
    {
        if (!canReceiveWater || !enableBurningEffect)
        {
            waterInput = 0.0f;
        }

        heatInput = Mathf.MoveTowards(heatInput, 0.0f, heatInputDecayRate * deltaTime);
        waterInput = Mathf.MoveTowards(waterInput, 0.0f, waterInputDecayRate * deltaTime);
    }

    protected virtual void OnDisable()
    {
        StopBurningEffect();
    }

    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!showSceneLabel)
        {
            return;
        }

        string label = string.IsNullOrWhiteSpace(sceneLabel) ? gameObject.name : sceneLabel;
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = sceneLabelColor }
        };

        Handles.Label(transform.position + sceneLabelOffset, label, labelStyle);
#endif
    }
}
