using UnityEngine;
using UnityEngine.UI;

public sealed class VRMissionProgressDisplay : MonoBehaviour
{
    [SerializeField] private Text progressText;
    [SerializeField] private bool useCompactText = true;
    [SerializeField] private string timerPrefix = "Tiempo restante";

    public void Configure(Text text, bool compactText)
    {
        progressText = text;
        useCompactText = compactText;
    }

    public void SetProgress(int current, int total)
    {
        if (progressText == null)
        {
            return;
        }

        progressText.text = useCompactText
            ? $"{current}/{total}"
            : $"Fuegos apagados: {current}/{total}";
    }

    public void SetTimeRemaining(float seconds)
    {
        if (progressText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0.0f, seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        string timeText = $"{minutes:00}:{remainingSeconds:00}";

        progressText.text = useCompactText
            ? timeText
            : $"{timerPrefix}: {timeText}";
    }
}
