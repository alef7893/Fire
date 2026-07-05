using UnityEngine;
using UnityEngine.UI;

public class VRMissionResultUI : MonoBehaviour
{
    [Header("Placement")]
    [Min(0.5f)] public float distanceFromPlayer = 1.5f;
    public float verticalOffset = 0.0f;

    [Header("Navigation")]
    public string mainMenuSceneName = "MainMenu";

    private GameObject interactivePanel;
    private Text titleText;
    private Text messageText;
    private Button restartButton;
    private Button menuButton;
    private Button messageButton;
    private FireGraphOutcomeController displayedOutcome;

    private void Awake()
    {
        CachePanelElements();
        ConfigureButtons();
        interactivePanel.SetActive(false);
    }

    private void Update()
    {
        if (displayedOutcome != null)
        {
            return;
        }

        FireGraphOutcomeController[] outcomes = FindObjectsOfType<FireGraphOutcomeController>();
        foreach (FireGraphOutcomeController outcome in outcomes)
        {
            if (outcome.currentOutcome == FireGraphOutcome.Victory)
            {
                ShowVictory(outcome);
                return;
            }

            if (outcome.currentOutcome == FireGraphOutcome.Defeat)
            {
                ShowDefeat(outcome);
                return;
            }
        }
    }

    public void RestartMission()
    {
        SceneLoadUtility.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        VRSceneInputSanitizer.Sanitize();
        SceneLoadUtility.LoadScene(mainMenuSceneName);
    }

    private void ShowVictory(FireGraphOutcomeController outcome)
    {
        displayedOutcome = outcome;
        titleText.text = "Mision completada";
        messageText.text = "Todo el fuego fue extinguido.";
        ShowPanel();
    }

    private void ShowDefeat(FireGraphOutcomeController outcome)
    {
        displayedOutcome = outcome;
        titleText.text = "Mision fallida";
        messageText.text = GetDefeatMessage(outcome.currentDefeatReason);
        ShowPanel();
    }

    private void ShowPanel()
    {
        PositionInFrontOfPlayer();
        interactivePanel.SetActive(true);
    }

    private void PositionInFrontOfPlayer()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            return;
        }

        Vector3 horizontalForward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        if (horizontalForward.sqrMagnitude < 0.001f)
        {
            horizontalForward = playerCamera.transform.forward;
        }

        transform.position = playerCamera.transform.position +
                             horizontalForward * distanceFromPlayer +
                             Vector3.up * verticalOffset;
        transform.rotation =
            Quaternion.LookRotation(playerCamera.transform.position - transform.position, Vector3.up) *
            Quaternion.Euler(0.0f, 180.0f, 0.0f);
    }

    private string GetDefeatMessage(MissionDefeatReason reason)
    {
        switch (reason)
        {
            case MissionDefeatReason.ProtectedVegetationBurned:
                return "La vegetacion protegida permanecio incendiada durante mas de 20 segundos.";
            case MissionDefeatReason.PlayerFireExposureExceeded:
                return "Permaneciste demasiado cerca del fuego y sufriste una exposicion peligrosa.";
            case MissionDefeatReason.TimeExpired:
                return "Se agoto el tiempo disponible para completar la mision.";
            case MissionDefeatReason.ObjectiveFailed:
                return "No se pudo completar uno de los objetivos de la mision.";
            default:
                return "La mision termino por una condicion de derrota.";
        }
    }

    private void CachePanelElements()
    {
        Transform panelTransform = transform.Find("InteractivePanel");
        if (panelTransform == null)
        {
            throw new MissingReferenceException("VRMissionResultUI requires an InteractivePanel child.");
        }

        interactivePanel = panelTransform.gameObject;
        titleText = FindText("Title");
        restartButton = FindButton("OptionButton1");
        menuButton = FindButton("OptionButton2");
        messageButton = FindButton("OptionButton3");
        messageText = messageButton.GetComponentInChildren<Text>(true);
    }

    private void ConfigureButtons()
    {
        titleText.text = "Resultado de la mision";
        SetButtonLabel(restartButton, "Reiniciar mision");
        SetButtonLabel(menuButton, "Volver al menu");

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartMission);
        menuButton.onClick.RemoveAllListeners();
        menuButton.onClick.AddListener(ReturnToMainMenu);

        messageButton.interactable = false;
        messageText.resizeTextForBestFit = true;
        messageText.resizeTextMinSize = 10;
        messageText.resizeTextMaxSize = 18;
    }

    private Text FindText(string objectName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text.gameObject.name == objectName)
            {
                return text;
            }
        }

        throw new MissingReferenceException($"VRMissionResultUI could not find text {objectName}.");
    }

    private Button FindButton(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.gameObject.name == objectName)
            {
                return button;
            }
        }

        throw new MissingReferenceException($"VRMissionResultUI could not find button {objectName}.");
    }

    private void SetButtonLabel(Button button, string label)
    {
        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }
}
