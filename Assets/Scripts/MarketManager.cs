using System;
using UnityEngine;
using System.Collections.Generic;
using PlayerScripts;
using TMPro;
using UnityEngine.UI;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;
    public MarketItemDatabase itemDatabase;

    private List<MarketItem> AllItems => itemDatabase.items;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform itemParent;
    public int totalCost = 0;
    public TextMeshProUGUI costText;
    public List<MarketItem> takenItems=new List<MarketItem>();
    [SerializeField] private Button purchaseButton;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Start()
    {
        GenerateMarket();
        purchaseButton.onClick.AddListener(PurchaseItems);
    }

    private void GenerateMarket()
    {
        foreach (var item in AllItems)
        {
            GameObject currentItem = Instantiate(itemPrefab,itemParent);
            ItemUI currentItemUI = currentItem.GetComponent<ItemUI>();
            currentItemUI.Setup(item);
        }
    }

    private void PurchaseItems()
    {
        if (totalCost <= PlayerDataManager.Instance.playerData.money)
        {
            PlayerDataManager.Instance.playerData.money -= totalCost;
            foreach (MarketItem takenItem in takenItems)
                PlayerDataManager.Instance.playerData.inventory.Add(takenItem);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }
    public void AddItemtoPacket()
    {
        
    }

    public void UpdateCostText()
    {
        costText.text = this.totalCost.ToString() + " $";
    }
    public void ResetMarket()
    {
        totalCost = 0;
        UpdateCostText();
        takenItems.Clear();
    }
}
