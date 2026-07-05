using UnityEngine;

using UnityEngine.Serialization;

public class LeverWithLocalRotation : MonoBehaviour
{
    public Transform lever; // The lever's transform
    [FormerlySerializedAs("particleSystem")]
    public ParticleSystem sprayParticleSystem; // The particle system to activate
    public AudioSource audioSource; // The audio source to play
    public float activationThreshold = 10f; // Degrees of rotation to activate particles (e.g., ±10 degrees from the initial position)

    private float initialLocalRotationX; // The initial local rotation of the lever in X

    public bool IsActivated { get; private set; }

    void Start()
    {
        if (lever == null)
        {
            enabled = false;
            return;
        }

        // Save the initial local rotation of the lever
        initialLocalRotationX = lever.localEulerAngles.x;

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    void Update()
    {
        // Get the current local rotation of the lever in X
        float currentLocalRotationX = lever.localEulerAngles.x;

        // Handle the 360-degree wrap-around issue
        if (currentLocalRotationX > 180)
            currentLocalRotationX -= 360;

        // Calculate the rotation difference
        float rotationDifference = Mathf.Abs(currentLocalRotationX - initialLocalRotationX);
        IsActivated = rotationDifference >= activationThreshold;

        // Check if the rotation exceeds the threshold
        if (IsActivated)
        {
            if (sprayParticleSystem != null && !sprayParticleSystem.isPlaying)
            {
                sprayParticleSystem.Play(); // Activate particles
                Debug.Log("Particles activated!");

                if (audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.Play();
                    Debug.Log("Audio started!");
                }
            }

        }
        else
        {
            if (sprayParticleSystem != null && sprayParticleSystem.isPlaying)
            {
                sprayParticleSystem.Stop(); // Deactivate particles
                Debug.Log("Particles deactivated!");

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                    Debug.Log("Audio stopped!");
                }
            }
        }
    }

    private void OnDisable()
    {
        IsActivated = false;
    }
}

