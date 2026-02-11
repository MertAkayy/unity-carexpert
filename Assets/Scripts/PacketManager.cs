using System.Collections.Generic;
using TMPro;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

public class PacketManager : MonoBehaviour
{
   public static PacketManager Instance;
   [SerializeField] private GameObject _packetItemPrefab;
   [SerializeField] private Transform _packetItemParent;
   void Awake()
   {
       if (Instance == null)
           Instance = this;
       else
           Destroy(gameObject);
   }

   public void AddItemtoPacket(MarketItem marketItem)
   {
       GameObject item = Instantiate(_packetItemPrefab, _packetItemParent);
       PacketItemUI currentItemUI = item.GetComponent<PacketItemUI>();
       currentItemUI.Setup(marketItem);
   }
}
