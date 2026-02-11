using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item Database", menuName = "Market/Item Database")]
public class MarketItemDatabase : ScriptableObject
{
    public List<MarketItem> items;
    
}