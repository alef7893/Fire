using UnityEngine;

public enum FireNodeType
{
    Spark,
    Sensitive,
    ProtectedVegetation
}

public enum FireNodeState
{
    Off,
    Heating,
    Burning,
    Cooling,
    Extinguishing,
    Extinguished,
    ProtectedLost
}

// Legacy compatibility component. New prefabs should use FireNodeSpark,
// FireNodeSensitive, or FireNodeProtected instead.
public class FireNode : FireNodeBase
{
    [Header("Legacy Node Type")]
    public FireNodeType nodeType = FireNodeType.Sensitive;

    [Header("Legacy Spark")]
    public float sparkHoldDuration = 6.0f;
    public float sparkDecayRate = 0.2f;

    [Header("Legacy Protected Vegetation")]
    public bool enableProtectedVegetationAlerts = true;
    public float maximumBurnDuration = 20.0f;

    protected override void UpdateFireState(float deltaTime)
    {
        UpdateHeatDependentFire(deltaTime, true);
    }
}
