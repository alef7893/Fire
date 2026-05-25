using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class FireNodeGizmo : MonoBehaviour
{
    public bool showGizmo = true;
    public bool showLabel = true;
    public float radius = 0.8f;
    public float centerSize = 0.08f;
    public Color offColor = new Color(1.0f, 0.85f, 0.15f, 0.85f);
    public Color burningColor = new Color(1.0f, 0.1f, 0.0f, 0.95f);
    public Color destroyedColor = new Color(0.35f, 0.35f, 0.35f, 0.8f);

    private void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }

        FireNode fireNode = GetComponent<FireNode>();
        Color stateColor = GetStateColor(fireNode);
        float safeRadius = Mathf.Max(0.01f, radius);
        float safeCenterSize = Mathf.Max(0.01f, centerSize);

        Gizmos.color = stateColor;
        Gizmos.DrawSphere(transform.position, safeRadius);
        Gizmos.DrawSphere(transform.position, safeCenterSize);

#if UNITY_EDITOR
        if (showLabel)
        {
            Handles.Label(transform.position + Vector3.up * (safeRadius + 0.15f), gameObject.name);
        }
#endif
    }

    private Color GetStateColor(FireNode fireNode)
    {
        if (fireNode == null)
        {
            return offColor;
        }

        if (fireNode.IsBurning())
        {
            return burningColor;
        }

        if (fireNode.IsDestroyed())
        {
            return destroyedColor;
        }

        return offColor;
    }
}
