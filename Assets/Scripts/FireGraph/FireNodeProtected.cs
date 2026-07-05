using UnityEngine;

public class FireNodeProtected : FireNodeBase
{
    [Header("Protected Vegetation")]
    public bool enableProtectedVegetationAlerts = true;
    [Min(0.1f)] public float maximumBurnDuration = 20.0f;

    private float protectedBurnTimer;
    private bool hasSentIgnitionAlert;
    private bool hasSentLossWarning;

    public float RemainingSafeTime => Mathf.Max(0.0f, maximumBurnDuration - protectedBurnTimer);

    public override bool CanPropagateHeat()
    {
        return false;
    }

    protected override void InitializeNode()
    {
        canReignite = true;
        base.InitializeNode();
    }

    protected override void Update()
    {
        base.Update();
        UpdateProtectedRisk(Time.deltaTime);
    }

    protected override void UpdateFireState(float deltaTime)
    {
        if (state == FireNodeState.ProtectedLost)
        {
            return;
        }

        UpdateHeatDependentFire(deltaTime, false);
    }

    public override void ApplyHeat(float amount)
    {
        if (state == FireNodeState.ProtectedLost)
        {
            return;
        }

        base.ApplyHeat(amount);
    }

    public override void ApplyWater(float amount)
    {
        if (state == FireNodeState.ProtectedLost)
        {
            return;
        }

        base.ApplyWater(amount);
    }

    protected override bool IsFinalState()
    {
        return state == FireNodeState.ProtectedLost;
    }

    protected override void ExtinguishForNow()
    {
        base.ExtinguishForNow();
        ResetProtectedRisk();
    }

    private void UpdateProtectedRisk(float deltaTime)
    {
        if (state == FireNodeState.ProtectedLost)
        {
            return;
        }

        if (state == FireNodeState.Extinguished)
        {
            ResetProtectedRisk();
            return;
        }

        if (!hasSentIgnitionAlert)
        {
            if (fireIntensity <= minimumVisibleIntensity)
            {
                return;
            }

            hasSentIgnitionAlert = true;
            if (enableProtectedVegetationAlerts)
            {
                Debug.Log($"[ALERTA] Vegetacion protegida empezo a incendiarse: {gameObject.name}");
            }
        }

        protectedBurnTimer += deltaTime;
        if (!hasSentLossWarning && protectedBurnTimer >= maximumBurnDuration)
        {
            hasSentLossWarning = true;
            state = FireNodeState.ProtectedLost;
            if (enableProtectedVegetationAlerts)
            {
                Debug.Log($"[DERROTA] Vegetacion protegida excedio el tiempo permitido: {gameObject.name}");
            }
        }
    }

    private void ResetProtectedRisk()
    {
        protectedBurnTimer = 0.0f;
        hasSentIgnitionAlert = false;
        hasSentLossWarning = false;
    }
}
