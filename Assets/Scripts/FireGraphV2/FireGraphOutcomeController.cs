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

    [Header("Events")]
    public UnityEvent onVictory;
    public UnityEvent onDefeat;

    private FireGraphRoot graphRoot;
    private bool simulationStarted;
    private float victoryConfirmationTimer;

    private void Awake()
    {
        graphRoot = GetComponent<FireGraphRoot>();
        if (graphRoot == null)
        {
            graphRoot = GetComponentInParent<FireGraphRoot>();
        }

        Debug.Log($"[FIRE GRAPH] OutcomeController Awake. graphRoot={(graphRoot != null ? "found" : "null")} ({gameObject.name})");
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

    private void Finish(FireGraphOutcome outcome)
    {
        currentOutcome = outcome;

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
