using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FireEdge : MonoBehaviour
{
    public FireNodeBase source;
    public FireNodeBase target;
    public bool enabledForPropagation = true;
    public bool bidirectional = true;

    [Header("Propagation")]
    [Min(0.0f)] public float heatPower = 1.0f;
    [Min(0.01f)] public float propagationSpeed = 0.5f;
    public bool scalePropagationByDistance = true;
    public bool resetProgressWhenHeatStops = true;
    [Range(0.0f, 1.0f)] public float sourceToTargetProgress;
    [Range(0.0f, 1.0f)] public float targetToSourceProgress;

    [Header("Static Patches")]
    public bool useStaticPatches = true;
    public FireEdgePatch patchPrefab;
    [Min(0.1f)] public float patchSpacing = 1.0f;
    public Vector3 patchWorldOffset = Vector3.zero;
    public Transform patchesRoot;

    [Header("Moving Fire Visual")]
    public bool useMovingFireVisual = true;
    public FireMovingSegment movingFirePrefab;
    public Vector3 movingFireScale = Vector3.one * 0.75f;
    public Vector3 movingFireWorldOffset = Vector3.zero;
    public Transform movingFireRoot;

    [Header("Debug")]
    public bool showGizmo = true;
    public Color edgeColor = new Color(1.0f, 0.45f, 0.05f, 0.9f);
    [Min(1.0f)] public float lineThickness = 4.0f;
    public bool showPatchSlotGizmos = true;
    public Color patchSlotColor = new Color(1.0f, 0.55f, 0.05f, 0.55f);
    public float patchSlotRadius = 0.14f;

    private readonly List<FireEdgePatch> staticPatches = new List<FireEdgePatch>();
    private FireMovingSegment forwardMovingFire;
    private FireMovingSegment backwardMovingFire;
    private int forwardActiveSegment;
    private int backwardActiveSegment;
    private float forwardActiveProgress;
    private float backwardActiveProgress;
    private float forwardSuppressedUntil;
    private float backwardSuppressedUntil;

    public bool HasActivePropagation =>
        (forwardMovingFire != null && forwardMovingFire.gameObject.activeInHierarchy) ||
        (backwardMovingFire != null && backwardMovingFire.gameObject.activeInHierarchy);

    public bool IsValid()
    {
        return enabledForPropagation && source != null && target != null && source != target;
    }

    public FireNodeBase GetOtherNode(FireNodeBase node)
    {
        if (node == source)
        {
            return target;
        }

        if (bidirectional && node == target)
        {
            return source;
        }

        return null;
    }

    public float GetDistance()
    {
        if (!IsValid())
        {
            return 0.0f;
        }

        return Vector3.Distance(source.transform.position, target.transform.position);
    }

    public Vector3 GetMidpoint()
    {
        if (!IsValid())
        {
            return transform.position;
        }

        return Vector3.Lerp(source.transform.position, target.transform.position, 0.5f);
    }

    public void TickPropagation(float deltaTime, float globalHeatMultiplier)
    {
        if (!IsValid())
        {
            sourceToTargetProgress = 0.0f;
            targetToSourceProgress = 0.0f;
            HideMovingFire(forwardMovingFire);
            HideMovingFire(backwardMovingFire);
            return;
        }

        EnsureStaticPatches();
        EnsureMovingFires();
        sourceToTargetProgress = TickContinuousChain(source, target, staticPatches, ref forwardActiveSegment, ref forwardActiveProgress, forwardSuppressedUntil, forwardMovingFire, deltaTime, globalHeatMultiplier, false);

        if (bidirectional)
        {
            targetToSourceProgress = TickContinuousChain(target, source, staticPatches, ref backwardActiveSegment, ref backwardActiveProgress, backwardSuppressedUntil, backwardMovingFire, deltaTime, globalHeatMultiplier, true);
        }
        else
        {
            backwardActiveSegment = 0;
            backwardActiveProgress = 0.0f;
            backwardSuppressedUntil = 0.0f;
            HideMovingFire(backwardMovingFire);
            targetToSourceProgress = 0.0f;
        }
    }

    private float TickContinuousChain(FireNodeBase start, FireNodeBase end, List<FireEdgePatch> patches, ref int activeSegment, ref float activeProgress, float suppressedUntil, FireMovingSegment movingFire, float deltaTime, float globalHeatMultiplier, bool reverse)
    {
        int segmentCount = patches.Count + 1;
        if (segmentCount <= 0)
        {
            HideMovingFire(movingFire);
            return 0.0f;
        }

        ApplyEstablishedHeat(start, end, patches, activeSegment, segmentCount, globalHeatMultiplier, reverse);

        if (activeSegment >= segmentCount)
        {
            HideMovingFire(movingFire);
            return 1.0f;
        }

        FireNodeBase heatSource = GetChainElement(start, end, patches, activeSegment, reverse);
        FireNodeBase heatTarget = GetChainElement(start, end, patches, activeSegment + 1, reverse);
        bool isSuppressed = Time.time < suppressedUntil;

        if (isSuppressed || heatSource == null || heatTarget == null || !heatSource.CanPropagateHeat() || !heatTarget.CanReceiveHeat())
        {
            if (resetProgressWhenHeatStops)
            {
                activeProgress = 0.0f;
            }

            HideMovingFire(movingFire);
            return CalculateChainProgress(activeSegment, activeProgress, segmentCount);
        }

        if (activeProgress < 1.0f)
        {
            float distance = GetSegmentDistance(heatSource, heatTarget);
            float distanceFactor = scalePropagationByDistance ? Mathf.Max(0.01f, distance) : 1.0f;
            activeProgress = Mathf.Clamp01(activeProgress + (propagationSpeed / distanceFactor) * deltaTime);
        }

        heatTarget.ApplyHeat(heatPower * globalHeatMultiplier);
        UpdateMovingFire(movingFire, heatSource, heatTarget, activeProgress, false);

        bool reachedCurrentTarget = activeProgress >= 1.0f;
        bool currentTargetIsFinal = activeSegment >= segmentCount - 1;
        if (reachedCurrentTarget && currentTargetIsFinal && heatTarget.IsHeatSource)
        {
            activeSegment = segmentCount;
            activeProgress = 1.0f;
            HideMovingFire(movingFire);
            return 1.0f;
        }

        if (reachedCurrentTarget && !currentTargetIsFinal && heatTarget.CanPropagateHeat())
        {
            activeSegment++;
            activeProgress = 0.0f;
        }

        return CalculateChainProgress(activeSegment, activeProgress, segmentCount);
    }

    private void ApplyEstablishedHeat(FireNodeBase start, FireNodeBase end, List<FireEdgePatch> patches, int activeSegment, int segmentCount, float globalHeatMultiplier, bool reverse)
    {
        int establishedSegments = Mathf.Clamp(activeSegment, 0, segmentCount);
        for (int i = 0; i < establishedSegments; i++)
        {
            FireNodeBase establishedSource = GetChainElement(start, end, patches, i, reverse);
            FireNodeBase establishedTarget = GetChainElement(start, end, patches, i + 1, reverse);
            if (establishedSource == null || establishedTarget == null || !establishedSource.CanPropagateHeat() || !establishedTarget.CanReceiveHeat())
            {
                continue;
            }

            establishedTarget.ApplyHeat(heatPower * globalHeatMultiplier);
        }
    }

    private float CalculateChainProgress(int activeSegment, float activeProgress, int segmentCount)
    {
        if (segmentCount <= 0)
        {
            return 0.0f;
        }

        return Mathf.Clamp01((activeSegment + activeProgress) / segmentCount);
    }

    public void SuppressSegment(int segmentIndex, bool reverse, float duration)
    {
        if (reverse)
        {
            backwardActiveSegment = Mathf.Max(0, segmentIndex);
            backwardActiveProgress = 0.0f;
            backwardSuppressedUntil = Time.time + Mathf.Max(0.0f, duration);
            HideMovingFire(backwardMovingFire);
        }
        else
        {
            forwardActiveSegment = Mathf.Max(0, segmentIndex);
            forwardActiveProgress = 0.0f;
            forwardSuppressedUntil = Time.time + Mathf.Max(0.0f, duration);
            HideMovingFire(forwardMovingFire);
        }
    }

    private FireNodeBase GetChainElement(FireNodeBase start, FireNodeBase end, List<FireEdgePatch> patches, int index, bool reverse)
    {
        if (index == 0)
        {
            return start;
        }

        if (index == patches.Count + 1)
        {
            return end;
        }

        int patchIndex = reverse ? patches.Count - index : index - 1;
        if (patchIndex < 0 || patchIndex >= patches.Count)
        {
            return null;
        }

        return patches[patchIndex];
    }

    private float GetSegmentDistance(FireNodeBase from, FireNodeBase to)
    {
        if (from == null || to == null)
        {
            return 1.0f;
        }

        return Vector3.Distance(from.transform.position, to.transform.position);
    }

    private void EnsureStaticPatches()
    {
        if (!useStaticPatches || patchPrefab == null || !IsValid())
        {
            return;
        }

        int requiredCount = CalculatePatchCount();
        while (staticPatches.Count < requiredCount)
        {
            int index = staticPatches.Count;
            FireEdgePatch patch = Instantiate(patchPrefab, GetPatchParent());
            patch.name = $"{name}_Patch_{index + 1:00}";
            staticPatches.Add(patch);
        }

        for (int i = 0; i < staticPatches.Count; i++)
        {
            if (staticPatches[i] == null)
            {
                continue;
            }

            bool shouldBeActive = i < requiredCount;
            staticPatches[i].gameObject.SetActive(shouldBeActive);
            if (shouldBeActive)
            {
                staticPatches[i].transform.position = GetPatchPosition(i, requiredCount);
            }
        }
    }

    private int CalculatePatchCount()
    {
        float distance = GetDistance();
        if (distance <= patchSpacing)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.FloorToInt(distance / Mathf.Max(0.1f, patchSpacing)));
    }

    private Transform GetPatchParent()
    {
        return patchesRoot != null ? patchesRoot : transform;
    }

    private Vector3 GetPatchPosition(int index, int patchCount)
    {
        float t = (index + 1.0f) / (patchCount + 1.0f);
        return Vector3.Lerp(source.transform.position, target.transform.position, t) + patchWorldOffset;
    }

    private void EnsureMovingFires()
    {
        if (!useMovingFireVisual || movingFirePrefab == null)
        {
            HideMovingFire(forwardMovingFire);
            HideMovingFire(backwardMovingFire);
            return;
        }

        if (forwardMovingFire == null)
        {
            forwardMovingFire = CreateMovingFire("Forward", false);
        }

        if (bidirectional && backwardMovingFire == null)
        {
            backwardMovingFire = CreateMovingFire("Reverse", true);
        }
    }

    private FireMovingSegment CreateMovingFire(string label, bool reverse)
    {
        FireMovingSegment movingFire = Instantiate(movingFirePrefab, GetMovingFireParent());
        movingFire.name = $"{name}_MovingFire_{label}";
        movingFire.fireVisualScale = movingFireScale;
        movingFire.Configure(this, 0, reverse);
        movingFire.gameObject.SetActive(false);
        return movingFire;
    }

    private Transform GetMovingFireParent()
    {
        if (movingFireRoot != null)
        {
            return movingFireRoot;
        }

        return patchesRoot != null ? patchesRoot : transform;
    }

    private void UpdateMovingFire(FireMovingSegment movingFire, FireNodeBase heatSource, FireNodeBase heatTarget, float progress, bool isSuppressed)
    {
        if (!useMovingFireVisual || movingFire == null || heatSource == null || heatTarget == null || isSuppressed || !heatSource.CanPropagateHeat() || !heatTarget.CanReceiveHeat())
        {
            HideMovingFire(movingFire);
            return;
        }

        if (!movingFire.gameObject.activeSelf)
        {
            movingFire.gameObject.SetActive(true);
        }

        movingFire.fireVisualScale = movingFireScale;
        movingFire.Configure(this, GetCurrentMovingFireSegment(movingFire), IsReverseMovingFire(movingFire));
        movingFire.SetSegmentPose(heatSource.transform.position + movingFireWorldOffset, heatTarget.transform.position + movingFireWorldOffset, progress);
    }

    private int GetCurrentMovingFireSegment(FireMovingSegment movingFire)
    {
        return movingFire == backwardMovingFire ? backwardActiveSegment : forwardActiveSegment;
    }

    private bool IsReverseMovingFire(FireMovingSegment movingFire)
    {
        return movingFire == backwardMovingFire;
    }

    private void HideMovingFire(FireMovingSegment movingFire)
    {
        if (movingFire != null && movingFire.gameObject.activeSelf)
        {
            movingFire.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo || source == null || target == null)
        {
            return;
        }

#if UNITY_EDITOR
        Handles.color = edgeColor;
        Handles.DrawAAPolyLine(lineThickness, source.transform.position, target.transform.position);
#else
        Gizmos.color = edgeColor;
        Gizmos.DrawLine(source.transform.position, target.transform.position);
#endif

        if (showPatchSlotGizmos)
        {
            Gizmos.color = patchSlotColor;
            int patchCount = CalculatePatchCount();
            for (int i = 0; i < patchCount; i++)
            {
                Gizmos.DrawSphere(GetPatchPosition(i, patchCount), patchSlotRadius);
            }
        }
    }
}
