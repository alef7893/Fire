using UnityEngine;

public class FireMovingSegment : MonoBehaviour, IFireWaterTarget
{
    [Header("Visual")]
    public Transform visualRoot;
    public Vector3 fireVisualScale = Vector3.one * 0.75f;
    public Vector3 localVisualOffset = Vector3.zero;

    [Header("Suppression")]
    public float waterToSuppress = 0.35f;
    public float suppressionDuration = 2.0f;
    public float waterDecayRate = 1.5f;

    [Header("Collider")]
    public SphereCollider interactionCollider;
    public float colliderRadius = 0.35f;

    [Header("Gizmo")]
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0.1f, 0.65f, 1.0f, 0.45f);

    private FireEdge owner;
    private int segmentIndex;
    private bool reverseDirection;
    private float accumulatedWater;
    private float suppressedUntilTime;

    public bool IsSuppressed => Time.time < suppressedUntilTime;

    private void Awake()
    {
        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<SphereCollider>();
        }

        if (interactionCollider != null)
        {
            interactionCollider.isTrigger = true;
            interactionCollider.radius = Mathf.Max(0.01f, colliderRadius);
        }

        ApplyVisualScale();
    }

    private void Update()
    {
        accumulatedWater = Mathf.MoveTowards(accumulatedWater, 0.0f, waterDecayRate * Time.deltaTime);
    }

    public void Configure(FireEdge edgeOwner, int index, bool reverse)
    {
        owner = edgeOwner;
        segmentIndex = index;
        reverseDirection = reverse;
    }

    public void SetSegmentPose(Vector3 start, Vector3 end, float progress)
    {
        transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(progress));
        Vector3 direction = end - start;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = localVisualOffset;
        }

        ApplyVisualScale();
    }

    public void ApplyWater(float amount)
    {
        if (!isActiveAndEnabled || IsSuppressed)
        {
            return;
        }

        accumulatedWater += Mathf.Max(0.0f, amount) * Time.deltaTime;
        if (accumulatedWater >= Mathf.Max(0.01f, waterToSuppress))
        {
            Suppress();
        }
    }

    public void Suppress()
    {
        accumulatedWater = 0.0f;
        suppressedUntilTime = Time.time + Mathf.Max(0.0f, suppressionDuration);
        owner?.SuppressSegment(segmentIndex, reverseDirection, suppressionDuration);
        gameObject.SetActive(false);
    }

    private void ApplyVisualScale()
    {
        Transform target = visualRoot != null ? visualRoot : transform;
        target.localScale = fireVisualScale;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, colliderRadius);
    }
}
