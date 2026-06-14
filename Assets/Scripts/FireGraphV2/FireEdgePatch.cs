using UnityEngine;

public class FireEdgePatch : FireNodeBase
{
    [Header("Patch")]
    public bool destroyWhenExtinguished = true;
    public float destroyDelay = 0.2f;
    public float propagationIntensityThreshold = 0.45f;

    [Header("Patch Gizmo")]
    public bool showPatchGizmo = true;
    public Color patchGizmoColor = new Color(1.0f, 0.3f, 0.05f, 0.65f);
    public float patchGizmoRadius = 0.16f;

    protected override void InitializeNode()
    {
        canReignite = false;
        base.InitializeNode();
    }

    public override bool CanPropagateHeat()
    {
        return fireIntensity >= propagationIntensityThreshold && state != FireNodeState.Extinguished;
    }

    protected override void UpdateFireState(float deltaTime)
    {
        UpdateHeatDependentFire(deltaTime, true);
    }

    protected override bool IsFinalState()
    {
        return state == FireNodeState.Extinguished && !canReignite;
    }

    protected override void ExtinguishPermanently()
    {
        base.ExtinguishPermanently();

        if (destroyWhenExtinguished)
        {
            Destroy(gameObject, Mathf.Max(0.0f, destroyDelay));
        }
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!showPatchGizmo)
        {
            return;
        }

        Gizmos.color = patchGizmoColor;
        Gizmos.DrawSphere(transform.position, patchGizmoRadius);
    }
}
