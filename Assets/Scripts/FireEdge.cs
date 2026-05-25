using UnityEngine;

public enum FireSurfaceType
{
    Ground,
    Wall,
    Object
}

public enum FireEdgeState
{
    Idle,
    Burning,
    Burned
}

public class FireEdge : MonoBehaviour
{
    public FireNode source;
    public FireNode target;
    public bool enabledForPropagation = true;
    public FireSurfaceType surfaceType = FireSurfaceType.Ground;
    public float spreadDelay = 0.0f;
    public float propagationCostMultiplier = 1.0f;

    [Header("Linear Propagation")]
    public FireEdgeState state = FireEdgeState.Idle;
    public float propagationSpeed = 1.0f;
    [Range(0.0f, 1.0f)] public float progress = 0.0f;

    [Header("Ground Fire Effect")]
    public GameObject groundFirePatchPrefab;
    public GameObject nodeArrivalEffectPrefab;
    public Vector3 fireEffectLocalOffset = new Vector3(0.0f, 0.25f, 0.0f);
    public Vector3 firePatchLocalScale = Vector3.one;
    public Vector3 nodeArrivalEffectLocalScale = Vector3.one;
    public bool alignEffectToEdge = true;
    public bool muteFirePatchAudio = true;
    public float firePatchSpacing = 1.0f;
    public float firePatchLifetime = 18.0f;
    public float nodeArrivalEffectLifetime = 2.0f;

    [Header("Dynamic Patch Scale")]
    public bool useDynamicPatchScale = false;
    [Range(0.0f, 1.0f)] public float firePatchEdgeScaleFactor = 0.65f;
    public float firePatchMinimumScale = 0.0f;
    public float firePatchMaximumScale = 1.5f;
    public float firePatchResizeSpeed = 1.0f;
    public bool scaleGrowthByPropagationCost = true;
    public float firePatchGrowDuration = 3.0f;
    public float firePatchFadeDuration = 3.0f;
    public AnimationCurve firePatchGrowthCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    public AnimationCurve firePatchFadeCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

    [Header("Ground Visual Debug")]
    public bool showGizmo = true;
    public Color edgeColor = new Color(1.0f, 0.55f, 0.05f, 0.9f);
    public float midpointSize = 0.12f;

    private FireNode activeSource;
    private FireNode activeTarget;
    private FireSimulationManager simulationManager;
    private readonly System.Collections.Generic.List<GameObject> activeFirePatches = new System.Collections.Generic.List<GameObject>();
    private float delayTimer;
    private float nextPatchDistance;

    public bool IsValid()
    {
        return enabledForPropagation && source != null && target != null && source != target;
    }

    public void AssignSimulationManager(FireSimulationManager manager)
    {
        simulationManager = manager;
    }

    public bool TryStartPropagation(FireNode startNode, FireSimulationManager manager)
    {
        if (!IsValid() || state != FireEdgeState.Idle || startNode == null)
        {
            return false;
        }

        if (startNode == source)
        {
            activeSource = source;
            activeTarget = target;
        }
        else if (startNode == target)
        {
            activeSource = target;
            activeTarget = source;
        }
        else
        {
            return false;
        }

        simulationManager = manager;
        state = FireEdgeState.Burning;
        progress = 0.0f;
        delayTimer = Mathf.Max(0.0f, spreadDelay);
        nextPatchDistance = 0.0f;
        UpdateFireVisuals();
        return true;
    }

    public float GetDistance()
    {
        if (source == null || target == null)
        {
            return 0.0f;
        }

        return Vector3.Distance(source.transform.position, target.transform.position);
    }

    public Vector3 GetMidpoint()
    {
        if (source == null || target == null)
        {
            return transform.position;
        }

        return Vector3.Lerp(source.transform.position, target.transform.position, 0.5f);
    }

    private void Update()
    {
        if (state != FireEdgeState.Burning)
        {
            return;
        }

        if (activeSource == null || activeTarget == null)
        {
            CompletePropagation();
            return;
        }

        if (delayTimer > 0.0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        float distance = Mathf.Max(Vector3.Distance(activeSource.transform.position, activeTarget.transform.position), 0.01f);
        float speed = Mathf.Max(0.01f, propagationSpeed);
        float costMultiplier = propagationCostMultiplier > 0.0f ? propagationCostMultiplier : 1.0f;
        progress = Mathf.Clamp01(progress + (speed * Time.deltaTime) / (distance * costMultiplier));
        UpdateFireVisuals();

        if (progress >= 1.0f)
        {
            CompletePropagation();
        }
    }

    private void UpdateFireVisuals()
    {
        if (activeSource == null || activeTarget == null)
        {
            return;
        }

        Vector3 start = activeSource.transform.position;
        Vector3 end = activeTarget.transform.position;
        SpawnFirePatches(start, end);
    }

    private void SpawnFirePatches(Vector3 start, Vector3 end)
    {
        if (groundFirePatchPrefab == null)
        {
            return;
        }

        float totalDistance = Vector3.Distance(start, end);
        if (totalDistance <= 0.01f)
        {
            return;
        }

        float spacing = Mathf.Max(0.1f, firePatchSpacing);
        float burnedDistance = totalDistance * progress;
        while (nextPatchDistance <= burnedDistance)
        {
            float normalizedDistance = Mathf.Clamp01(nextPatchDistance / totalDistance);
            CreateFirePatch(start, end, normalizedDistance);
            nextPatchDistance += spacing;
        }
    }

    private void CreateFirePatch(Vector3 start, Vector3 end, float normalizedDistance)
    {
        Vector3 position = Vector3.Lerp(start, end, normalizedDistance) + fireEffectLocalOffset;
        GameObject patchRoot = new GameObject($"FirePatch_{activeFirePatches.Count:00}");
        patchRoot.transform.SetParent(transform);
        patchRoot.transform.position = position;

        GameObject patch = Instantiate(groundFirePatchPrefab, patchRoot.transform);
        patch.transform.localPosition = Vector3.zero;
        patch.transform.localRotation = Quaternion.identity;
        patch.transform.localScale = firePatchLocalScale;

        float positionScaleFactor = GetPatchPositionScaleFactor(normalizedDistance);
        float minimumScale = useDynamicPatchScale ? Mathf.Max(0.0f, firePatchMinimumScale) : positionScaleFactor;
        float maximumScale = useDynamicPatchScale
            ? Mathf.Max(minimumScale, firePatchMaximumScale * positionScaleFactor)
            : positionScaleFactor;

        patchRoot.transform.localScale = Vector3.one * minimumScale;
        if (muteFirePatchAudio)
        {
            MuteAudioSources(patch);
        }

        if (alignEffectToEdge)
        {
            Vector3 direction = end - start;
            direction.y = 0.0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                patchRoot.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        activeFirePatches.Add(patchRoot);
        if (useDynamicPatchScale)
        {
            StartCoroutine(AnimateFirePatchScale(patchRoot, minimumScale, maximumScale));
        }
        else if (firePatchLifetime > 0.0f)
        {
            Destroy(patchRoot, firePatchLifetime);
        }
    }

    private float GetPatchPositionScaleFactor(float normalizedDistance)
    {
        float edgeScaleFactor = Mathf.Clamp01(firePatchEdgeScaleFactor);
        float centerFactor = 1.0f - Mathf.Abs(Mathf.Clamp01(normalizedDistance) - 0.5f) * 2.0f;
        return Mathf.Lerp(edgeScaleFactor, 1.0f, centerFactor);
    }

    private System.Collections.IEnumerator AnimateFirePatchScale(GameObject patchRoot, float minimumScale, float maximumScale)
    {
        if (patchRoot == null)
        {
            yield break;
        }

        float propagationCost = scaleGrowthByPropagationCost ? Mathf.Max(0.01f, propagationCostMultiplier) : 1.0f;
        float resizeSpeed = Mathf.Max(0.01f, firePatchResizeSpeed);
        float growDuration = Mathf.Max(0.0f, firePatchGrowDuration * propagationCost / resizeSpeed);
        if (growDuration > 0.0f)
        {
            float elapsed = 0.0f;
            while (elapsed < growDuration && patchRoot != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / growDuration);
                float curvedT = firePatchGrowthCurve != null ? firePatchGrowthCurve.Evaluate(t) : t;
                float scale = Mathf.LerpUnclamped(minimumScale, maximumScale, curvedT);
                patchRoot.transform.localScale = Vector3.one * Mathf.Max(0.0f, scale);
                yield return null;
            }
        }

        if (patchRoot == null)
        {
            yield break;
        }

        patchRoot.transform.localScale = Vector3.one * maximumScale;

        float fadeDuration = Mathf.Max(0.0f, firePatchFadeDuration / resizeSpeed);
        float stableDuration = Mathf.Max(0.0f, firePatchLifetime - growDuration - fadeDuration);
        if (stableDuration > 0.0f)
        {
            yield return new WaitForSeconds(stableDuration);
        }

        if (patchRoot == null)
        {
            yield break;
        }

        if (fadeDuration > 0.0f)
        {
            float elapsed = 0.0f;
            float fadeStartScale = patchRoot.transform.localScale.x;
            while (elapsed < fadeDuration && patchRoot != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float scaleFactor = firePatchFadeCurve != null ? firePatchFadeCurve.Evaluate(t) : 1.0f - t;
                float scale = Mathf.LerpUnclamped(minimumScale, fadeStartScale, Mathf.Max(0.0f, scaleFactor));
                patchRoot.transform.localScale = Vector3.one * Mathf.Max(0.0f, scale);
                yield return null;
            }
        }

        if (patchRoot != null)
        {
            Destroy(patchRoot);
        }
    }

    private void MuteAudioSources(GameObject instance)
    {
        AudioSource[] audioSources = instance.GetComponentsInChildren<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.mute = true;
            audioSource.Stop();
        }
    }

    private void CompletePropagation()
    {
        state = FireEdgeState.Burned;
        progress = 1.0f;
        UpdateFireVisuals();

        if (activeTarget != null && activeTarget.CanIgnite())
        {
            CreateNodeArrivalEffect(activeTarget.transform.position);
            activeTarget.Ignite();
            simulationManager?.RegisterBurningNode(activeTarget);
        }
    }

    private void CreateNodeArrivalEffect(Vector3 nodePosition)
    {
        if (nodeArrivalEffectPrefab == null)
        {
            return;
        }

        GameObject arrivalEffect = Instantiate(nodeArrivalEffectPrefab, nodePosition + fireEffectLocalOffset, Quaternion.identity);
        arrivalEffect.transform.localScale = nodeArrivalEffectLocalScale;

        if (nodeArrivalEffectLifetime > 0.0f)
        {
            Destroy(arrivalEffect, nodeArrivalEffectLifetime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo || source == null || target == null)
        {
            return;
        }

        Gizmos.color = edgeColor;
        Gizmos.DrawLine(source.transform.position, target.transform.position);
        DrawPatchGizmos();
    }

    private void DrawPatchGizmos()
    {
        Vector3 start = source.transform.position;
        Vector3 end = target.transform.position;
        float totalDistance = Vector3.Distance(start, end);
        if (totalDistance <= 0.01f)
        {
            return;
        }

        float spacing = Mathf.Max(0.1f, firePatchSpacing);
        float sphereRadius = Mathf.Max(0.01f, midpointSize);

        for (float distance = 0.0f; distance <= totalDistance; distance += spacing)
        {
            float normalizedDistance = Mathf.Clamp01(distance / totalDistance);
            Gizmos.DrawSphere(Vector3.Lerp(start, end, normalizedDistance), sphereRadius);
        }

        Gizmos.DrawSphere(end, sphereRadius);
    }
}
