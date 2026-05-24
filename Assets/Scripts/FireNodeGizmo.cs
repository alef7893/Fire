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

        FireObject fireObject = GetComponent<FireObject>();
        Color stateColor = GetStateColor(fireObject);
        float safeRadius = Mathf.Max(0.01f, radius);
        float safeCenterSize = Mathf.Max(0.01f, centerSize);

        Gizmos.color = stateColor;
        Gizmos.DrawWireSphere(transform.position, safeRadius);
        Gizmos.DrawSphere(transform.position, safeCenterSize);

#if UNITY_EDITOR
        Handles.color = stateColor;
        Handles.DrawWireDisc(transform.position, Vector3.up, safeRadius);
        Handles.DrawWireDisc(transform.position, Vector3.right, safeRadius);
        Handles.DrawWireDisc(transform.position, Vector3.forward, safeRadius);

        if (showLabel)
        {
            Handles.Label(transform.position + Vector3.up * (safeRadius + 0.15f), gameObject.name);
        }
#endif
    }

    private Color GetStateColor(FireObject fireObject)
    {
        if (fireObject == null)
        {
            return offColor;
        }

        if (fireObject.IsBurning())
        {
            return burningColor;
        }

        if (fireObject.IsDestroyed())
        {
            return destroyedColor;
        }

        return offColor;
    }
}
