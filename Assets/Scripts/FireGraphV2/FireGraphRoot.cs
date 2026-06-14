using UnityEngine;

public class FireGraphRoot : MonoBehaviour
{
    public Transform nodesRoot;
    public Transform edgesRoot;

    [Header("Propagation")]
    public bool enablePropagation = true;
    public bool includeInactiveGraphObjects = false;
    [Min(0.0f)] public float globalHeatMultiplier = 1.0f;

    public FireNodeBase[] GetNodes(bool includeInactive)
    {
        Transform searchRoot = nodesRoot != null ? nodesRoot : transform;
        return searchRoot.GetComponentsInChildren<FireNodeBase>(includeInactive);
    }

    public FireEdge[] GetEdges(bool includeInactive)
    {
        Transform searchRoot = edgesRoot != null ? edgesRoot : transform;
        return searchRoot.GetComponentsInChildren<FireEdge>(includeInactive);
    }

    private void Update()
    {
        if (!enablePropagation)
        {
            return;
        }

        FireEdge[] edges = GetEdges(includeInactiveGraphObjects);
        foreach (FireEdge edge in edges)
        {
            if (edge != null)
            {
                edge.TickPropagation(Time.deltaTime, globalHeatMultiplier);
            }
        }
    }
}
