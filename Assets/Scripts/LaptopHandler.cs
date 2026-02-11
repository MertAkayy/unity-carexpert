using UnityEngine;

public class LaptopHandler : MonoBehaviour ,IInteractable
{
    [SerializeField] private GameObject mainFrame;
    [SerializeField] private GameObject computerFrame;
    public void Interact()
    {
        UIManager.Instance.ShowCursor();
        mainFrame.SetActive(false);
        computerFrame.SetActive(true);
    }
}
