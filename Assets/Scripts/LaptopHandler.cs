using UnityEngine;
using UnityEngine.UI;

public class LaptopHandler : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject mainFrame;
    [SerializeField] private GameObject computerFrame;

    [Header("Computer UI")]
    [SerializeField] private Button vehicleInformationButton;
    [SerializeField] private Button closeReportButton;
    [SerializeField] private AccidentReportScreen accidentReportScreen;

    private void Awake()
    {
        if (vehicleInformationButton != null)
            vehicleInformationButton.onClick.AddListener(OpenVehicleInformation);

        if (closeReportButton != null)
            closeReportButton.onClick.AddListener(CloseVehicleInformation);
    }

    public void Interact()
    {
        UIManager.Instance.ShowCursor();
        mainFrame.SetActive(false);
        computerFrame.SetActive(true);

        if (accidentReportScreen != null)
            accidentReportScreen.Hide();
    }

    private void OpenVehicleInformation()
    {
        computerFrame.SetActive(false);
        if (accidentReportScreen != null)
            accidentReportScreen.Show();
    }

    private void CloseVehicleInformation()
    {
        if (accidentReportScreen != null)
            accidentReportScreen.Hide();
        computerFrame.SetActive(true);
    }
}
