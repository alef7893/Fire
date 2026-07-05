using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuMissionResultController : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject[] mainMenuPanels;
    [SerializeField] private Text titleText;
    [SerializeField] private Text messageText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        ConfigureButtons();
        Refresh();
    }

    public void RetryMission()
    {
        string retrySceneName = MissionResultState.RetrySceneName;
        MissionResultState.Clear();

        if (string.IsNullOrWhiteSpace(retrySceneName))
        {
            ShowMainMenu();
            return;
        }

        SceneLoadUtility.LoadScene(retrySceneName);
    }

    public void ReturnToMainMenu()
    {
        MissionResultState.Clear();
        ShowMainMenu();
    }

    public void ContinueMission()
    {
        string continueSceneName = MissionResultState.ContinueSceneName;
        MissionResultState.Clear();

        if (string.IsNullOrWhiteSpace(continueSceneName))
        {
            ShowMainMenu();
            return;
        }

        SceneLoadUtility.LoadScene(continueSceneName);
    }

    public void Configure(
        GameObject panel,
        GameObject[] normalPanels,
        Text title,
        Text message,
        Button retry,
        Button menu,
        Button continueActionButton)
    {
        resultPanel = panel;
        mainMenuPanels = normalPanels;
        titleText = title;
        messageText = message;
        retryButton = retry;
        mainMenuButton = menu;
        continueButton = continueActionButton;
    }

    private void ConfigureButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryMission);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            SetButtonLabel(mainMenuButton, "Volver al Menu Principal");
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueMission);
            SetButtonLabel(continueButton, "Continuar a Mision 1");
        }

        if (messageText != null)
        {
            messageText.resizeTextForBestFit = true;
            messageText.resizeTextMinSize = 10;
            messageText.resizeTextMaxSize = 18;
        }
    }

    private void Refresh()
    {
        if (!MissionResultState.HasPendingResult)
        {
            ShowMainMenu();
            return;
        }

        ShowResult();
    }

    private void ShowResult()
    {
        SetMainMenuPanelsActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        bool victory = MissionResultState.Outcome == FireGraphOutcome.Victory;
        SetButtonLabel(retryButton, MissionResultState.RetryButtonLabel);

        if (continueButton != null)
        {
            bool canContinue = victory &&
                               !string.IsNullOrWhiteSpace(MissionResultState.ContinueSceneName);
            continueButton.gameObject.SetActive(canContinue);
        }

        if (titleText != null)
        {
            titleText.text = victory ? "Mision completada" : "Mision fallida";
        }

        if (messageText != null)
        {
            messageText.text = victory
                ? $"{MissionResultState.MissionDisplayName} finalizada correctamente."
                : GetDefeatMessage(MissionResultState.DefeatReason);
        }
    }

    private void ShowMainMenu()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SetMainMenuPanelsActive(true);
    }

    private void SetMainMenuPanelsActive(bool active)
    {
        if (mainMenuPanels == null)
        {
            return;
        }

        foreach (GameObject panel in mainMenuPanels)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }

    private string GetDefeatMessage(MissionDefeatReason reason)
    {
        switch (reason)
        {
            case MissionDefeatReason.ProtectedVegetationBurned:
                return "La vegetacion protegida permanecio incendiada demasiado tiempo.";
            case MissionDefeatReason.PlayerFireExposureExceeded:
                return "El jugador permanecio demasiado cerca del fuego.";
            case MissionDefeatReason.TimeExpired:
                return "Se agoto el tiempo disponible para completar la mision.";
            case MissionDefeatReason.ObjectiveFailed:
                return "No se pudo completar uno de los objetivos de la mision.";
            default:
                return $"{MissionResultState.MissionDisplayName} termino en derrota.";
        }
    }

    private void SetButtonLabel(Button button, string label)
    {
        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }
    }
}
