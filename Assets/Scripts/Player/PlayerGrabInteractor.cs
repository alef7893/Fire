using UnityEngine;

public class PlayerGrabInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float grabRange = 5.5f;
    [SerializeField] private float grabRadius = 0.45f;
    [SerializeField] private bool allowNearbyFallback = true;
    [SerializeField] private float nearbyGrabRadius = 2.25f;
    [SerializeField] private float nearbyMaxAngle = 75f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.35f, -0.35f, 0.75f);
    [SerializeField] private Vector3 heldLocalEulerAngles = new Vector3(8f, 180f, 0f);

    private GrabbableExtinguisher heldObject;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (holdPoint == null && playerCamera != null)
        {
            holdPoint = playerCamera.transform;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        if (heldObject != null)
        {
            heldObject.Drop();
            heldObject = null;
            return;
        }

        TryGrabObject();
    }

    private void TryGrabObject()
    {
        if (playerCamera == null || holdPoint == null)
        {
            return;
        }

        GrabbableExtinguisher grabbable = FindGrabbableTarget();
        if (grabbable == null || grabbable.IsHeld)
        {
            return;
        }

        grabbable.Grab(holdPoint, heldLocalPosition, heldLocalEulerAngles);
        heldObject = grabbable;
    }

    private GrabbableExtinguisher FindGrabbableTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray, grabRadius, grabRange, interactableLayers, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            GrabbableExtinguisher grabbable = hit.collider.GetComponentInParent<GrabbableExtinguisher>();
            if (grabbable != null && !grabbable.IsHeld)
            {
                return grabbable;
            }
        }

        return allowNearbyFallback ? FindNearbyGrabbable() : null;
    }

    private GrabbableExtinguisher FindNearbyGrabbable()
    {
        Collider[] colliders = Physics.OverlapSphere(playerCamera.transform.position, nearbyGrabRadius, interactableLayers, QueryTriggerInteraction.Collide);
        GrabbableExtinguisher bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            GrabbableExtinguisher grabbable = collider.GetComponentInParent<GrabbableExtinguisher>();
            if (grabbable == null || grabbable.IsHeld)
            {
                continue;
            }

            Vector3 toTarget = collider.bounds.center - playerCamera.transform.position;
            float angle = Vector3.Angle(playerCamera.transform.forward, toTarget);
            if (angle > nearbyMaxAngle)
            {
                continue;
            }

            float score = toTarget.sqrMagnitude + angle * 0.02f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = grabbable;
            }
        }

        return bestTarget;
    }
}
