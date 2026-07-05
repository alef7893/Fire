using UnityEngine;

public sealed class XRTrackedAreaBoundary : MonoBehaviour
{
    [SerializeField] private Transform trackedHead;
    [SerializeField] private Vector3 worldCenter;
    [SerializeField, Min(0.1f)] private float maxRadius = 2.7f;

    public void Configure(Transform head, Vector3 center, float radius)
    {
        trackedHead = head;
        worldCenter = center;
        maxRadius = Mathf.Max(0.1f, radius);
    }

    private void LateUpdate()
    {
        if (trackedHead == null)
        {
            return;
        }

        Vector3 offset = trackedHead.position - worldCenter;
        offset.y = 0f;

        float radiusSquared = maxRadius * maxRadius;
        if (offset.sqrMagnitude <= radiusSquared)
        {
            return;
        }

        Vector3 allowedPosition = worldCenter + offset.normalized * maxRadius;
        Vector3 correction = allowedPosition - trackedHead.position;
        correction.y = 0f;
        transform.position += correction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        const int segmentCount = 48;
        Vector3 previous = worldCenter + Vector3.forward * maxRadius;

        for (int index = 1; index <= segmentCount; index++)
        {
            float angle = index * Mathf.PI * 2f / segmentCount;
            Vector3 current = worldCenter
                + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * maxRadius;
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }
}
