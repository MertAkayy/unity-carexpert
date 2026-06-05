using UnityEngine;
using UnityEngine.UI;

public class LaptopHandler : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject mainFrame;
    [SerializeField] private GameObject computerFrame;

    [Header("Vehicle Information")]
    [SerializeField] private Button vehicleInformationButton;
    [SerializeField] private Button closeReportButton;
    [SerializeField] private AccidentReportScreen accidentReportScreen;

    [Header("Inspection Checklist")]
    [SerializeField] private Button reportButton;
    [SerializeField] private Button closeChecklistButton;
    [SerializeField] private InspectionChecklistUI checklistUI;

    private void Awake()
    {
        if (vehicleInformationButton != null)
            vehicleInformationButton.onClick.AddListener(OpenVehicleInformation);

        if (closeReportButton != null)
            closeReportButton.onClick.AddListener(BackToMain);

        if (reportButton != null)
            reportButton.onClick.AddListener(OpenChecklist);

        if (closeChecklistButton != null)
            closeChecklistButton.onClick.AddListener(BackToMain);
    }

    public void Interact()
    {
        UIManager.Instance.ShowCursor();
        mainFrame.SetActive(false);
        computerFrame.SetActive(true);

        // Always start on the main screen
        HideAllPanels();
    }

    private void OpenVehicleInformation()
    {
        computerFrame.SetActive(false);
        HideAllPanels();
        if (accidentReportScreen != null)
            accidentReportScreen.Show();
    }

    private void OpenChecklist()
    {
        computerFrame.SetActive(false);
        HideAllPanels();
        if (checklistUI != null)
            checklistUI.Show();
    }

    private void BackToMain()
    {
        HideAllPanels();
        computerFrame.SetActive(true);
    }

    private void HideAllPanels()
    {
        if (accidentReportScreen != null)
            accidentReportScreen.Hide();
        if (checklistUI != null)
            checklistUI.Hide();
    }
}
