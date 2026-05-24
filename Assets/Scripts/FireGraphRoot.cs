using UnityEngine;

public class FireGraphRoot : MonoBehaviour
{
    public Transform nodesRoot;
    public Transform edgesRoot;

    public FireObject[] GetNodes(bool includeInactive)
    {
        Transform searchRoot = nodesRoot != null ? nodesRoot : transform;
        return searchRoot.GetComponentsInChildren<FireObject>(includeInactive);
    }

    public FireEdge[] GetEdges(bool includeInactive)
    {
        Transform searchRoot = edgesRoot != null ? edgesRoot : transform;
        return searchRoot.GetComponentsInChildren<FireEdge>(includeInactive);
    }
}
