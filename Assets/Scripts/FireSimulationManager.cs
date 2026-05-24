using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulationManager : MonoBehaviour
{
    [Header("Start")]
    public FireObject startingNode;
    public bool igniteStartingNodeOnStart = true;

    [Header("Graph")]
    public FireGraphRoot graphRoot;
    public bool treatConnectionsAsBidirectional = true;
    public bool includeInactiveNodes = true;

    [Header("Propagation")]
    public float spreadInterval = 1.0f;
    public float minimumEdgeDistance = 0.1f;
    public float propagationMultiplier = 5.0f;

    private readonly List<FireObject> allNodes = new List<FireObject>();
    private readonly List<FireObject> burningNodes = new List<FireObject>();
    private readonly Dictionary<FireObject, List<RuntimeFireEdge>> graph = new Dictionary<FireObject, List<RuntimeFireEdge>>();

    private class RuntimeFireEdge
    {
        public FireEdge edge;
        public FireObject target;
    }

    private void Start()
    {
        BuildGraphFromScene();

        if (igniteStartingNodeOnStart)
        {
            IgniteStartingNode();
        }

        StartCoroutine(SpreadLoop());
    }

    public void RebuildGraph()
    {
        BuildGraphFromScene();
    }

    public void RegisterBurningNode(FireObject node)
    {
        if (node == null || burningNodes.Contains(node))
        {
            return;
        }

        burningNodes.Add(node);
    }

    public void RemoveBurningNode(FireObject node)
    {
        if (node == null)
        {
            return;
        }

        burningNodes.Remove(node);
    }

    private void BuildGraphFromScene()
    {
        graph.Clear();
        allNodes.Clear();
        burningNodes.Clear();

        if (graphRoot == null)
        {
            graphRoot = GetComponent<FireGraphRoot>();
        }

        FireObject[] sceneNodes = graphRoot != null
            ? graphRoot.GetNodes(includeInactiveNodes)
            : FindObjectsOfType<FireObject>(includeInactiveNodes);

        foreach (FireObject node in sceneNodes)
        {
            if (node == null)
            {
                continue;
            }

            allNodes.Add(node);
            graph[node] = new List<RuntimeFireEdge>();

            if (node.IsBurning())
            {
                RegisterBurningNode(node);
            }
        }

        FireEdge[] explicitEdges = graphRoot != null
            ? graphRoot.GetEdges(includeInactiveNodes)
            : FindObjectsOfType<FireEdge>(includeInactiveNodes);

        foreach (FireEdge edge in explicitEdges)
        {
            if (edge == null || !edge.IsValid())
            {
                continue;
            }

            edge.AssignSimulationManager(this);
            AddEdge(edge.source, edge.target, edge);

            if (treatConnectionsAsBidirectional)
            {
                AddEdge(edge.target, edge.source, edge);
            }
        }

        Debug.Log($"Runtime fire graph built with {allNodes.Count} nodes and {CountEdges()} directed edges.");
    }

    private void AddEdge(FireObject source, FireObject target, FireEdge edgeComponent)
    {
        if (source == null || target == null || source == target)
        {
            return;
        }

        if (!graph.ContainsKey(source))
        {
            graph[source] = new List<RuntimeFireEdge>();
        }

        foreach (RuntimeFireEdge existingEdge in graph[source])
        {
            if (existingEdge.target == target)
            {
                return;
            }
        }

        graph[source].Add(new RuntimeFireEdge
        {
            edge = edgeComponent,
            target = target
        });
    }

    private IEnumerator SpreadLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.01f, spreadInterval));

        while (true)
        {
            ProcessBurningNodes(spreadInterval);
            DecayInactiveExposure(spreadInterval);
            yield return wait;
        }
    }

    private void ProcessBurningNodes(float deltaTime)
    {
        if (burningNodes.Count == 0)
        {
            return;
        }

        List<FireObject> snapshot = new List<FireObject>(burningNodes);
        foreach (FireObject current in snapshot)
        {
            if (current == null)
            {
                burningNodes.Remove(current);
                continue;
            }

            if (!current.IsBurning())
            {
                RemoveBurningNode(current);
                continue;
            }

            current.BurnUpdate(deltaTime);
            if (current.IsDestroyed())
            {
                RemoveBurningNode(current);
                continue;
            }

            PropagateFrom(current, deltaTime);
        }
    }

    private void PropagateFrom(FireObject source, float deltaTime)
    {
        if (!graph.TryGetValue(source, out List<RuntimeFireEdge> edges))
        {
            return;
        }

        foreach (RuntimeFireEdge edge in edges)
        {
            FireObject target = edge.target;
            if (target == null || !target.CanIgnite())
            {
                continue;
            }

            edge.edge?.TryStartPropagation(source, this);
        }
    }

    private void DecayInactiveExposure(float deltaTime)
    {
        foreach (FireObject node in allNodes)
        {
            if (node != null && !node.IsBurning())
            {
                node.DecayExposure(deltaTime);
            }
        }
    }

    private void IgniteStartingNode()
    {
        if (startingNode == null)
        {
            Debug.LogWarning("No starting fire node has been assigned.");
            return;
        }

        startingNode.Ignite();
        RegisterBurningNode(startingNode);
    }

    private int CountEdges()
    {
        int count = 0;
        foreach (List<RuntimeFireEdge> edges in graph.Values)
        {
            count += edges.Count;
        }

        return count;
    }
}
