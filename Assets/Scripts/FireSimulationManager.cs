using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulationManager : MonoBehaviour
{
    [Header("Graph")]
    public FireGraphRoot graphRoot;
    public bool treatConnectionsAsBidirectional = true;
    public bool includeInactiveNodes = true;

    [Header("Propagation")]
    public float spreadInterval = 1.0f;
    public float minimumEdgeDistance = 0.1f;
    public float propagationMultiplier = 5.0f;

    private readonly List<FireNode> allNodes = new List<FireNode>();
    private readonly List<FireNode> burningNodes = new List<FireNode>();
    private readonly Dictionary<FireNode, List<RuntimeFireEdge>> graph = new Dictionary<FireNode, List<RuntimeFireEdge>>();

    private class RuntimeFireEdge
    {
        public FireEdge edge;
        public FireNode target;
    }

    private void Start()
    {
        BuildGraphFromScene();
        StartCoroutine(SpreadLoop());
    }

    public void RebuildGraph()
    {
        BuildGraphFromScene();
    }

    public void RegisterBurningNode(FireNode node)
    {
        if (node == null || burningNodes.Contains(node))
        {
            return;
        }

        burningNodes.Add(node);
    }

    public void RemoveBurningNode(FireNode node)
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

        FireNode[] sceneNodes = graphRoot != null
            ? graphRoot.GetNodes(includeInactiveNodes)
            : FindObjectsOfType<FireNode>(includeInactiveNodes);

        foreach (FireNode node in sceneNodes)
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

    private void AddEdge(FireNode source, FireNode target, FireEdge edgeComponent)
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

        List<FireNode> snapshot = new List<FireNode>(burningNodes);
        foreach (FireNode current in snapshot)
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

    private void PropagateFrom(FireNode source, float deltaTime)
    {
        if (!graph.TryGetValue(source, out List<RuntimeFireEdge> edges))
        {
            return;
        }

        foreach (RuntimeFireEdge edge in edges)
        {
            FireNode target = edge.target;
            if (target == null || !target.CanIgnite())
            {
                continue;
            }

            edge.edge?.TryStartPropagation(source, this);
        }
    }

    private void DecayInactiveExposure(float deltaTime)
    {
        foreach (FireNode node in allNodes)
        {
            if (node != null && !node.IsBurning())
            {
                node.DecayExposure(deltaTime);
            }
        }
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
