using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PacketItemUI : MonoBehaviour
{
    [SerializeField] private Image packetIcon;
    [SerializeField] private TextMeshProUGUI packetName;
    [SerializeField] private TextMeshProUGUI packetNumber;
    [SerializeField] private TextMeshProUGUI packetItemCost;

    public void Setup(MarketItem item)
    {
     packetIcon.sprite = item.icon;
     packetName.text = item.name;
     packetItemCost.text = item.price.ToString();
    }
}
