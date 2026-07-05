using System.Collections.Generic;
using UnityEngine;

public class VRExtinguisherWaterSprayer : MonoBehaviour
{
    [SerializeField] private LeverWithLocalRotation activationLever;
    [SerializeField] private Transform detectionOrigin;

    [Header("Fire Suppression")]
    [SerializeField] private float waterRange = 6.0f;
    [SerializeField] private float waterRadius = 0.35f;
    [SerializeField] private float waterPower = 0.15f;
    [SerializeField] private LayerMask fireTargetLayers = ~0;

    [Header("Debug")]
    [SerializeField] private bool showDetectionGizmo = true;
    [SerializeField] private Color detectionGizmoColor = new Color(0.1f, 0.65f, 1.0f, 0.35f);

    private readonly List<Component> wateredTargets = new List<Component>();

    public bool IsSpraying => activationLever != null && activationLever.IsActivated;

    private void Update()
    {
        if (IsSpraying && detectionOrigin != null)
        {
            ApplyWaterToTargets();
        }
    }

    public void Configure(LeverWithLocalRotation lever, Transform origin)
    {
        activationLever = lever;
        detectionOrigin = origin;
    }

    private void ApplyWaterToTargets()
    {
        wateredTargets.Clear();

        RaycastHit[] hits = Physics.SphereCastAll(
            detectionOrigin.position,
            Mathf.Max(0.01f, waterRadius),
            detectionOrigin.forward,
            Mathf.Max(0.01f, waterRange),
            fireTargetLayers,
            QueryTriggerInteraction.Collide);

        foreach (RaycastHit hit in hits)
        {
            IFireWaterTarget waterTarget = hit.collider.GetComponentInParent<IFireWaterTarget>();
            Component targetComponent = waterTarget as Component;
            if (waterTarget == null || targetComponent == null || wateredTargets.Contains(targetComponent))
            {
                continue;
            }

            waterTarget.ApplyWater(waterPower);
            wateredTargets.Add(targetComponent);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDetectionGizmo || detectionOrigin == null)
        {
            return;
        }

        float range = Mathf.Max(0.01f, waterRange);
        float radius = Mathf.Max(0.01f, waterRadius);
        Vector3 start = detectionOrigin.position;
        Vector3 end = start + detectionOrigin.forward * range;

        Gizmos.color = detectionGizmoColor;
        Gizmos.DrawWireSphere(start, radius);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, radius);
    }
}
