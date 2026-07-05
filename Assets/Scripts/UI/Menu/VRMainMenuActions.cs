using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class VRMainMenuActions : MonoBehaviour
{
    [FormerlySerializedAs("tutorialSceneName")]
    [SerializeField] private string missionZeroSceneName = "M00_Training";
    [SerializeField] private string missionOneSceneName = "M01_BasicSuppression";
    [FormerlySerializedAs("missionSceneName")]
    [SerializeField] private string missionTwoSceneName = "M02_ForestContainment";
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject missionSelectionPanel;
    [SerializeField] private Button[] mainPanelButtons;
    [SerializeField] private Button quitButton;
    [SerializeField] private float quitConfirmationSeconds = 3f;

    private float quitConfirmationDeadline = -1f;

    public void Configure(Button exitButton)
    {
        quitButton = exitButton;
    }

    public void ConfigurePanels(
        GameObject mainPanel,
        GameObject missionPanel,
        Button[] primaryButtons,
        Button exitButton)
    {
        mainButtonsPanel = mainPanel;
        missionSelectionPanel = missionPanel;
        mainPanelButtons = primaryButtons;
        quitButton = exitButton;
    }

    private void Awake()
    {
        CloseMissionPanel();
    }

    public void OpenMissionPanel()
    {
        SetMainPanelInteractable(false);

        if (missionSelectionPanel != null)
        {
            missionSelectionPanel.SetActive(true);
        }
    }

    public void CloseMissionPanel()
    {
        if (missionSelectionPanel != null)
        {
            missionSelectionPanel.SetActive(false);
        }

        SetMainPanelInteractable(true);
    }

    public void LoadTutorial()
    {
        LoadMissionZero();
    }

    public void LoadMissionZero()
    {
        SceneLoadUtility.LoadScene(missionZeroSceneName);
    }

    public void LoadMissionOne()
    {
        SceneLoadUtility.LoadScene(missionOneSceneName);
    }

    public void LoadMission()
    {
        LoadMissionTwo();
    }

    public void LoadMissionTwo()
    {
        SceneLoadUtility.LoadScene(missionTwoSceneName);
    }

    public void RequestQuit()
    {
        if (Time.unscaledTime > quitConfirmationDeadline)
        {
            quitConfirmationDeadline = Time.unscaledTime + quitConfirmationSeconds;
            SetQuitLabel("Confirmar salida");
            return;
        }

        Debug.Log("[VR Main Menu] Salida confirmada por el usuario.");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Update()
    {
        if (quitConfirmationDeadline < 0f || Time.unscaledTime <= quitConfirmationDeadline)
        {
            return;
        }

        quitConfirmationDeadline = -1f;
        SetQuitLabel("Salir del Juego");
    }

    private void SetQuitLabel(string label)
    {
        if (quitButton == null)
        {
            return;
        }

        Text labelText = quitButton.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    private void SetMainPanelInteractable(bool interactable)
    {
        if (mainPanelButtons == null || mainPanelButtons.Length == 0)
        {
            if (mainButtonsPanel == null)
            {
                return;
            }

            mainPanelButtons = mainButtonsPanel.GetComponentsInChildren<Button>(true);
        }

        foreach (Button button in mainPanelButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
