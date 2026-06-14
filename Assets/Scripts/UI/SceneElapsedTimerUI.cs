using UnityEngine;
using UnityEngine.UI;

public class SceneElapsedTimerUI : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private string label = "Tiempo";

    private float elapsedTime;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<Text>();
        }
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        UpdateText();
    }

    private void UpdateText()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{label}: {minutes:00}:{seconds:00}";
    }
}
