using UnityEngine;
using UnityEngine.Events;

public enum FireGraphOutcome
{
    InProgress,
    Victory,
    Defeat
}

public enum MissionDefeatReason
{
    None,
    ProtectedVegetationBurned,
    PlayerFireExposureExceeded,
    TimeExpired,
    ObjectiveFailed
}

[RequireComponent(typeof(FireGraphRoot))]
public class FireGraphOutcomeController : MonoBehaviour
{
    [Header("Outcome")]
    public FireGraphOutcome currentOutcome = FireGraphOutcome.InProgress;
    public MissionDefeatReason currentDefeatReason = MissionDefeatReason.None;
    [Min(0.0f)] public float victoryConfirmationDuration = 2.0f;
    public bool stopPropagationOnFinish = true;
    public bool logResultToConsole = true;

    [Header("Optional Defeat Conditions")]
    public bool enablePlayerExposureDefeat = true;
    public bool enableTimeLimitDefeat;
    public bool startMonitoringOnEnable;

    [Header("Player Fire Exposure")]
    [Tooltip("Player position used for distance checks. Camera.main is used when empty.")]
    public Transform playerTransform;
    [Min(0.1f)] public float fireDangerRadius = 2.5f;
    [Min(0.1f)] public float maximumExposure = 10.0f;
    [Min(0.0f)] public float exposureRate = 1.0f;
    [Min(0.0f)] public float exposureRecoveryRate = 0.5f;
    public bool useHorizontalDistance = true;

    [Header("Mission Time Limit")]
    [Range(0, 59)] public int missionTimeLimitMinutes = 3;
    [Range(0, 59)] public int missionTimeLimitSeconds;

    [Header("Events")]
    public UnityEvent onVictory;
    public UnityEvent onDefeat;

    private FireGraphRoot graphRoot;
    private bool simulationStarted;
    private float victoryConfirmationTimer;
    private bool monitoringOptionalDefeatConditions;
    private bool timeLimitEvaluated;
    private float currentExposure;
    private float remainingTime;

    public float CurrentExposure => currentExposure;
    public float NormalizedExposure => maximumExposure > 0.0f
        ? Mathf.Clamp01(currentExposure / maximumExposure)
        : 0.0f;
    public float RemainingTime => remainingTime;
    public int MissionTimeLimitSeconds =>
        missionTimeLimitMinutes * 60 + missionTimeLimitSeconds;
    public bool IsMonitoringOptionalDefeatConditions => monitoringOptionalDefeatConditions;
    public bool IsPlayerExposureEnabled => enablePlayerExposureDefeat;
    public bool IsTimeLimitEnabled => enableTimeLimitDefeat;

    private void Awake()
    {
        graphRoot = GetComponent<FireGraphRoot>();
        if (graphRoot == null)
        {
            graphRoot = GetComponentInParent<FireGraphRoot>();
        }

        Debug.Log($"[FIRE GRAPH] OutcomeController Awake. graphRoot={(graphRoot != null ? "found" : "null")} ({gameObject.name})");
        ResetOptionalDefeatConditionState();
    }

    private void Start()
    {
        if (startMonitoringOnEnable)
        {
            BeginMissionMonitoring();
        }
    }

    private void Update()
    {
        if (currentOutcome != FireGraphOutcome.InProgress || graphRoot == null)
        {
            return;
        }

        FireNodeBase[] nodes = GetComponentsInChildren<FireNodeBase>(false);
        FireEdge[] edges = graphRoot.GetEdges(false);

        foreach (FireNodeBase node in nodes)
        {
            if (node is FireNodeProtected && node.state == FireNodeState.ProtectedLost)
            {
                ReportDefeat(MissionDefeatReason.ProtectedVegetationBurned);
                return;
            }

            simulationStarted |= node.HasEverIgnited;
        }

        if (monitoringOptionalDefeatConditions)
        {
            TickOptionalDefeatConditions(nodes, Time.deltaTime);
            if (currentOutcome != FireGraphOutcome.InProgress)
            {
                return;
            }
        }

        if (!simulationStarted)
        {
            victoryConfirmationTimer = 0.0f;
            return;
        }

        bool hasActiveFire = false;
        foreach (FireNodeBase node in nodes)
        {
            if (node.HasActiveFire)
            {
                hasActiveFire = true;
                break;
            }
        }

        bool hasActivePropagation = false;
        foreach (FireEdge edge in edges)
        {
            if (edge != null && edge.HasActivePropagation)
            {
                hasActivePropagation = true;
                break;
            }
        }

        if (hasActiveFire || hasActivePropagation)
        {
            victoryConfirmationTimer = 0.0f;
            return;
        }

        victoryConfirmationTimer += Time.deltaTime;
        if (victoryConfirmationTimer >= victoryConfirmationDuration)
        {
            Finish(FireGraphOutcome.Victory);
        }
    }

    [ContextMenu("Reset Outcome")]
    public void ResetOutcome()
    {
        currentOutcome = FireGraphOutcome.InProgress;
        currentDefeatReason = MissionDefeatReason.None;
        simulationStarted = false;
        victoryConfirmationTimer = 0.0f;
        monitoringOptionalDefeatConditions = false;
        ResetOptionalDefeatConditionState();

        if (graphRoot == null)
        {
            graphRoot = GetComponent<FireGraphRoot>();
        }

        if (graphRoot != null)
        {
            graphRoot.enablePropagation = true;
        }
    }

    public void ReportDefeat(MissionDefeatReason reason)
    {
        if (currentOutcome != FireGraphOutcome.InProgress)
        {
            return;
        }

        currentDefeatReason = reason;
        Finish(FireGraphOutcome.Defeat);
    }

    public void BeginMissionMonitoring()
    {
        ResetOptionalDefeatConditionState();
        monitoringOptionalDefeatConditions =
            enablePlayerExposureDefeat || enableTimeLimitDefeat;
    }

    public void StopMissionMonitoring()
    {
        monitoringOptionalDefeatConditions = false;
    }

    public void ConfigureOptionalDefeatConditions(
        bool playerExposureEnabled,
        bool timeLimitEnabled)
    {
        enablePlayerExposureDefeat = playerExposureEnabled;
        enableTimeLimitDefeat = timeLimitEnabled;
    }

    private void TickOptionalDefeatConditions(FireNodeBase[] nodes, float deltaTime)
    {
        if (enablePlayerExposureDefeat)
        {
            TickPlayerExposure(nodes, deltaTime);
            if (currentExposure >= maximumExposure)
            {
                ReportDefeat(MissionDefeatReason.PlayerFireExposureExceeded);
                return;
            }
        }

        if (!enableTimeLimitDefeat || timeLimitEvaluated)
        {
            return;
        }

        remainingTime = Mathf.Max(0.0f, remainingTime - deltaTime);
        if (remainingTime > 0.0f)
        {
            return;
        }

        timeLimitEvaluated = true;
        foreach (FireNodeBase node in nodes)
        {
            if (node != null && node.HasActiveFire)
            {
                ReportDefeat(MissionDefeatReason.TimeExpired);
                return;
            }
        }
    }

    private void TickPlayerExposure(FireNodeBase[] nodes, float deltaTime)
    {
        Transform trackedPlayer = ResolvePlayerTransform();
        if (trackedPlayer == null)
        {
            currentExposure = Mathf.MoveTowards(
                currentExposure,
                0.0f,
                exposureRecoveryRate * deltaTime);
            return;
        }

        float strongestExposure = 0.0f;
        foreach (FireNodeBase node in nodes)
        {
            if (node == null || !node.HasActiveFire)
            {
                continue;
            }

            Vector3 offset = trackedPlayer.position - node.transform.position;
            if (useHorizontalDistance)
            {
                offset.y = 0.0f;
            }

            float proximity = 1.0f - Mathf.Clamp01(offset.magnitude / fireDangerRadius);
            float fireExposure = proximity * Mathf.Clamp01(node.fireIntensity);
            strongestExposure = Mathf.Max(strongestExposure, fireExposure);
        }

        if (strongestExposure > 0.0f)
        {
            currentExposure = Mathf.Min(
                maximumExposure,
                currentExposure + strongestExposure * exposureRate * deltaTime);
        }
        else
        {
            currentExposure = Mathf.MoveTowards(
                currentExposure,
                0.0f,
                exposureRecoveryRate * deltaTime);
        }
    }

    private Transform ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return playerTransform;
        }

        Camera playerCamera = Camera.main;
        return playerCamera != null ? playerCamera.transform : null;
    }

    private void ResetOptionalDefeatConditionState()
    {
        currentExposure = 0.0f;
        remainingTime = Mathf.Max(0.0f, MissionTimeLimitSeconds);
        timeLimitEvaluated = false;
    }

    private void OnValidate()
    {
        fireDangerRadius = Mathf.Max(0.1f, fireDangerRadius);
        maximumExposure = Mathf.Max(0.1f, maximumExposure);
        exposureRate = Mathf.Max(0.0f, exposureRate);
        exposureRecoveryRate = Mathf.Max(0.0f, exposureRecoveryRate);
        missionTimeLimitMinutes = Mathf.Clamp(missionTimeLimitMinutes, 0, 59);
        missionTimeLimitSeconds = Mathf.Clamp(missionTimeLimitSeconds, 0, 59);
    }

    private void Finish(FireGraphOutcome outcome)
    {
        currentOutcome = outcome;
        monitoringOptionalDefeatConditions = false;

        if (stopPropagationOnFinish)
        {
            if (graphRoot != null)
            {
                graphRoot.enablePropagation = false;
            }
            else
            {
                Debug.LogWarning($"[FIRE GRAPH] Cannot stop propagation: graphRoot is null ({gameObject.name})");
            }
        }

        if (logResultToConsole)
        {
            Debug.Log($"[FIRE GRAPH] Result: {outcome} ({gameObject.name})");
        }

        if (outcome == FireGraphOutcome.Victory)
        {
            onVictory?.Invoke();
        }
        else
        {
            onDefeat?.Invoke();
        }
    }
}
