using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSimulationManager : MonoBehaviour
{
    public string fireGraphFileName = "fire_graph";
    public string startingNodeId;

    [Header("Propagation")]
    public float spreadInterval = 1.0f;
    public float minimumEdgeDistance = 0.1f;
    public float propagationMultiplier = 5.0f;
    public bool igniteStartingNodeOnStart = true;

    private Dictionary<string, FireObject> fireObjects = new Dictionary<string, FireObject>();
    private Dictionary<string, FireGraphNode> fireGraph = new Dictionary<string, FireGraphNode>();
    private List<FireObject> allNodes = new List<FireObject>();
    private List<FireObject> burningNodes = new List<FireObject>();

    void Start()
    {
        LoadFireGraph();
        CacheFireObjects();

        if (igniteStartingNodeOnStart)
        {
            IgniteStartingNode();
        }

        StartCoroutine(SpreadLoop());
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

    private IEnumerator SpreadLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(spreadInterval);

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
        string sourceId = source.gameObject.name;
        if (!fireGraph.ContainsKey(sourceId))
        {
            return;
        }

        FireGraphNode graphNode = fireGraph[sourceId];
        foreach (FireGraphEdge edge in graphNode.edges)
        {
            FireObject target;
            if (!fireObjects.TryGetValue(edge.targetId, out target))
            {
                continue;
            }

            if (!target.CanIgnite())
            {
                continue;
            }

            float distance = Mathf.Max(edge.distance, minimumEdgeDistance);
            float exposure = source.firePower * source.fireIntensity * propagationMultiplier * deltaTime / distance;
            bool ignited = target.AddExposure(exposure);
            if (ignited)
            {
                RegisterBurningNode(target);
            }
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
        if (string.IsNullOrEmpty(startingNodeId))
        {
            Debug.LogWarning("No startingNodeId configured for the fire simulation.");
            return;
        }

        FireObject start;
        if (!fireObjects.TryGetValue(startingNodeId, out start))
        {
            Debug.LogWarning($"Starting node '{startingNodeId}' was not found in the scene.");
            return;
        }

        start.Ignite();
        RegisterBurningNode(start);
    }

    private void LoadFireGraph()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fireGraphFileName);
        if (jsonFile == null)
        {
            Debug.LogError($"Could not find {fireGraphFileName}.json in a Resources folder.");
            return;
        }

        FireGraph graph = JsonUtility.FromJson<FireGraph>(jsonFile.text);
        if (graph == null || graph.nodes == null)
        {
            Debug.LogError($"Could not parse fire graph from {fireGraphFileName}.json.");
            return;
        }

        fireGraph.Clear();
        foreach (FireGraphNode node in graph.nodes)
        {
            fireGraph[node.id] = node;
        }
    }

    private void CacheFireObjects()
    {
        fireObjects.Clear();
        allNodes.Clear();
        burningNodes.Clear();

        FireObject[] fireObjectsInScene = GameObject.FindObjectsOfType<FireObject>();
        foreach (FireObject fireObject in fireObjectsInScene)
        {
            fireObjects[fireObject.gameObject.name] = fireObject;
            allNodes.Add(fireObject);

            if (fireObject.IsBurning())
            {
                RegisterBurningNode(fireObject);
            }
        }
    }
}
