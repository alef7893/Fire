using UnityEngine;

public class FireNodeSpark : FireNodeBase
{
    [Header("Spark")]
    public float sparkHoldDuration = 6.0f;
    public float sparkDecayRate = 0.2f;

    private float sparkHoldTimer;

    protected override void InitializeNode()
    {
        canReignite = false;
        fireIntensity = 1.0f;
        state = FireNodeState.Burning;
        sparkHoldTimer = sparkHoldDuration;
        hasShownVisibleFlame = true;
        MarkAsIgnited();
    }

    protected override void UpdateFireState(float deltaTime)
    {
        if (state == FireNodeState.Extinguished)
        {
            fireIntensity = 0.0f;
            return;
        }

        if (ReceivesWater)
        {
            sparkHoldTimer = 0.0f;
            state = FireNodeState.Extinguishing;
            fireIntensity -= extinguishRate * waterInput * deltaTime;
        }
        else if (state == FireNodeState.Burning && sparkHoldTimer > 0.0f)
        {
            sparkHoldTimer -= deltaTime;
            state = FireNodeState.Burning;
            fireIntensity = 1.0f;
        }
        else
        {
            sparkHoldTimer = 0.0f;
            state = FireNodeState.Cooling;
            fireIntensity -= sparkDecayRate * deltaTime;
        }

        fireIntensity = Mathf.Clamp01(fireIntensity);

        if (fireIntensity <= extinguishedIntensityThreshold)
        {
            ExtinguishPermanently();
        }
    }

    protected override bool IsFinalState()
    {
        return state == FireNodeState.Extinguished;
    }
}
