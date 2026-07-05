using UnityEngine;

[DefaultExecutionOrder(-9000)]
public sealed class StartAreaPanelFollower : MonoBehaviour
{
    [SerializeField] private Transform trackedHead;
    [SerializeField] private Transform panelRoot;
    [SerializeField] private Vector3 areaCenter;
    [SerializeField, Min(0.1f)] private float areaRadius = 2.0f;
    [SerializeField, Range(0.1f, 1.0f)] private float distanceMultiplier = 0.9f;
    [SerializeField] private float fixedY = 1.0f;
    [SerializeField] private float rotationOffsetY;
    [SerializeField] private float focusSideOffset;
    [SerializeField, Min(0.0f)] private float smoothSpeed = 12.0f;

    public void Configure(
        Transform head,
        Transform root,
        Vector3 center,
        float radius,
        float yPosition,
        float distanceScale)
    {
        trackedHead = head;
        panelRoot = root;
        areaCenter = center;
        areaRadius = Mathf.Max(0.1f, radius);
        fixedY = yPosition;
        distanceMultiplier = Mathf.Clamp01(distanceScale);
    }

    public void SetFocusLocalOffsetX(float offset)
    {
        focusSideOffset = offset;
    }

    public void SetRotationOffsetY(float offset)
    {
        rotationOffsetY = offset;
    }

    public void ClearFocusOffset()
    {
        focusSideOffset = 0.0f;
    }

    public void SnapToCurrentView()
    {
        if (trackedHead == null || panelRoot == null)
        {
            return;
        }

        ApplyPanelPose(1.0f);
    }

    private void LateUpdate()
    {
        if (trackedHead == null || panelRoot == null)
        {
            return;
        }

        ApplyPanelPose(1.0f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
    }

    private void ApplyPanelPose(float interpolation)
    {
        Vector3 direction = trackedHead.forward;
        direction.y = 0.0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = trackedHead.position - areaCenter;
            direction.y = 0.0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();
        Vector3 sideDirection = trackedHead.right;
        sideDirection.y = 0.0f;
        if (sideDirection.sqrMagnitude < 0.0001f)
        {
            sideDirection = Vector3.Cross(Vector3.up, direction);
        }

        sideDirection.Normalize();

        Vector3 targetPosition = areaCenter
            + direction * (areaRadius * distanceMultiplier)
            + sideDirection * focusSideOffset;
        targetPosition.y = fixedY;

        Vector3 lookDirection = targetPosition - trackedHead.position;
        lookDirection.y = 0.0f;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = direction;
        }

        lookDirection.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up)
            * Quaternion.Euler(0.0f, rotationOffsetY, 0.0f);

        if (smoothSpeed <= 0.0f)
        {
            panelRoot.position = targetPosition;
            panelRoot.rotation = targetRotation;
            return;
        }

        panelRoot.position = Vector3.Lerp(panelRoot.position, targetPosition, interpolation);
        panelRoot.rotation = Quaternion.Slerp(panelRoot.rotation, targetRotation, interpolation);
    }
}
