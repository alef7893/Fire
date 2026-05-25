using UnityEngine;

public class FireGraphRoot : MonoBehaviour
{
    public Transform nodesRoot;
    public Transform edgesRoot;

    public FireNode[] GetNodes(bool includeInactive)
    {
        Transform searchRoot = nodesRoot != null ? nodesRoot : transform;
        return searchRoot.GetComponentsInChildren<FireNode>(includeInactive);
    }

    public FireEdge[] GetEdges(bool includeInactive)
    {
        Transform searchRoot = edgesRoot != null ? edgesRoot : transform;
        return searchRoot.GetComponentsInChildren<FireEdge>(includeInactive);
    }
}
