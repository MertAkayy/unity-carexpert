using System.Collections.Generic;
using PlayerScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public class UiDeskManager : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;
    private readonly List<Button> _buttons = new List<Button>();
    [SerializeField]  private PlayerDataManager playerDataManager;

    public void Start()
    {
        UIManager.Instance.ShowCursor();
    }

    public void UpdateUI()
    {
        foreach (Button btn in _buttons)
        {
            Destroy(btn.gameObject);
        }
        _buttons.Clear();
        FillUiFromInventory();
    }
    private void ConfigureButton(Button button, int index)
    {
        button.onClick.AddListener(() =>
        {
            playerDataManager.SelectTool(playerDataManager.playerData.inventory[index]);
        });
    }
    private void FillUiFromInventory()
    {
        for (int i = 0; i < playerDataManager.playerData.inventory.Count; i++)
        {
            MarketItem item = playerDataManager.playerData.inventory[i];
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            Button button = buttonObj.GetComponent<Button>();

            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>(); 
            if (buttonText != null) buttonText.text = item.name;

            Image buttonImage = buttonObj.GetComponentsInChildren<Image>(true)
                .FirstOrDefault(img => img.gameObject != buttonObj);
            if (item.icon != null && buttonImage != null) buttonImage.sprite = item.icon;

            ConfigureButton(button, i); //closure problem
            _buttons.Add(button);
        }
    }
}
