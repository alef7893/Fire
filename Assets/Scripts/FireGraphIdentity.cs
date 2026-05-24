using UnityEngine;

public class FireGraphIdentity : MonoBehaviour
{
    public string nodeId;

    public string GetId()
    {
        return string.IsNullOrWhiteSpace(nodeId) ? gameObject.name : nodeId;
    }
}
