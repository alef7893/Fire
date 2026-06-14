using UnityEngine;

public class FireVfxScaleController : MonoBehaviour
{
    [Header("Scale")]
    public Vector3 minimumScale = Vector3.one * 0.2f;
    public Vector3 maximumScale = Vector3.one;
    public float resizeSpeed = 1.0f;

    [Header("Playback")]
    public bool playOnStart = true;
    public bool loop = true;
    public bool useUnscaledTime = false;

    private float progress;
    private int direction = 1;
    private bool isPlaying;

    private void Start()
    {
        transform.localScale = minimumScale;
        isPlaying = playOnStart;
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        progress += direction * Mathf.Max(0.0f, resizeSpeed) * deltaTime;
        progress = Mathf.Clamp01(progress);
        transform.localScale = Vector3.Lerp(minimumScale, maximumScale, progress);

        if (direction > 0 && progress >= 1.0f)
        {
            direction = -1;
        }
        else if (direction < 0 && progress <= 0.0f)
        {
            if (loop)
            {
                direction = 1;
            }
            else
            {
                isPlaying = false;
            }
        }
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void ResetScale()
    {
        progress = 0.0f;
        direction = 1;
        transform.localScale = minimumScale;
    }
}
