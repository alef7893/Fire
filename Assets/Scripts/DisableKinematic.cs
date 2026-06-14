using UnityEngine;
using Oculus.Interaction;

public class DisableKinematic : MonoBehaviour
{
    private Rigidbody rb; // Reference to the object's Rigidbody
    private bool isGrabbed = false; // Tracks the grab state

    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the object.");
        }

        // Ensure Rigidbody starts as kinematic
        rb.isKinematic = true;

        // Register grab events
        var grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
        {
            Debug.LogError("Grabbable component not found on the object.");
            return;
        }

        grabbable.WhenPointerEventRaised += HandleGrabEvent;
    }

    private void HandleGrabEvent(PointerEvent evt)
    {
        // Toggle grab state based on the event type
        if (evt.Type == PointerEventType.Select)
        {
            OnGrab();
        }
    }

    private void OnGrab()
    {
        isGrabbed = true;
        rb.isKinematic = false; // Disable isKinematic when grabbed
    }
}
