using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class ItemUI : MonoBehaviour
{
    [SerializeField]
    private Image itemIcon;
    [SerializeField]
    private TextMeshProUGUI itemName;
    [SerializeField]
    private TextMeshProUGUI itemPrice;
    [FormerlySerializedAs("button")] [SerializeField]
    private Button addToChartButton;
    private MarketItem _thisItem;
    private void Start()
    {
        addToChartButton.onClick.AddListener(AddToChart);
    }
    public void Setup(MarketItem item)
    {
        _thisItem=item;
        itemIcon.sprite = item.icon;
        itemName.text = item.name;
        itemPrice.text = item.price.ToString()+"  $";
    }
    private void AddToChart()
    {
        MarketManager.Instance.totalCost += _thisItem.price;
        MarketManager.Instance.takenItems.Add(_thisItem);
        MarketManager.Instance.UpdateCostText();
        PacketManager.Instance.AddItemtoPacket(_thisItem);
    }
    public int GetPrice()
    {
        return _thisItem.price;
    }
}
