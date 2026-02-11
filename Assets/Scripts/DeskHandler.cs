using UnityEngine;

public class DeskHandler : MonoBehaviour,IInteractable
{
    [SerializeField] private Canvas deskCanvas;
    [SerializeField] private GameObject uiManager;
    public void Interact()
    {
        UIManager.Instance.ShowCursor();
        deskCanvas.gameObject.SetActive(true);
        uiManager.GetComponentInChildren<UiDeskManager>().enabled = true;
        uiManager.GetComponentInChildren<UiDeskManager>().UpdateUI();
    }
}
