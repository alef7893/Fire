using System.Collections.Generic;
using UnityEngine;

//Structure for holding connections and their precomputed distances.
[System.Serializable]
public class FireGraphEdge
{
    public string targetId;
    public float distance;

    public FireGraphEdge(string targetId, float distance)
    {
        this.targetId = targetId;
        this.distance = distance;
    }
}

//Structure for nodes, their positions, and connectivity.
[System.Serializable]
public class FireGraphNode
{
    public string id;
    public Vector3 position;
    public List<FireGraphEdge> edges;

    public FireGraphNode(string id, Vector3 position)
    {
        this.id = id;
        this.position = position;
        this.edges = new List<FireGraphEdge>();
    }
}

//Structure for the entire graph of nodes and connectivity.
[System.Serializable]
public class FireGraph
{
    public List<FireGraphNode> nodes;

    public FireGraph()
    {
        this.nodes = new List<FireGraphNode>();
    }
}
