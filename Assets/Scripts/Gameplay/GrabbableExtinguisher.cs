using UnityEngine;

public class GrabbableExtinguisher : MonoBehaviour
{
    [SerializeField] private Rigidbody targetRigidbody;

    private Transform originalParent;
    private Vector3 originalLocalScale;

    public bool IsHeld { get; private set; }

    private void Awake()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }
    }

    public void Grab(Transform holdPoint, Vector3 localPosition, Vector3 localEulerAngles)
    {
        if (holdPoint == null || IsHeld)
        {
            return;
        }

        originalParent = transform.parent;
        originalLocalScale = transform.localScale;

        if (targetRigidbody != null)
        {
            targetRigidbody.velocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
            targetRigidbody.isKinematic = true;
            targetRigidbody.useGravity = false;
        }

        transform.SetParent(holdPoint, false);
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.Euler(localEulerAngles);
        transform.localScale = originalLocalScale;
        IsHeld = true;
    }

    public void Drop()
    {
        if (!IsHeld)
        {
            return;
        }

        transform.SetParent(originalParent, true);

        if (targetRigidbody != null)
        {
            targetRigidbody.isKinematic = false;
            targetRigidbody.useGravity = true;
            targetRigidbody.velocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
        }

        IsHeld = false;
    }
}
