using UnityEngine;
using System.Collections.Generic;
[System.Serializable]

[CreateAssetMenu(fileName = "Create Market Item", menuName = "Market/Item")]
public class MarketItem :ScriptableObject
{
    public new string name;
    public string description;
    public int price;
    public Sprite icon;
    public bool isTool;
    public int requiredLevel;
    public GameObject itemObject;
    public Tool toolObject;
    public ItemType itemType;
    
    public MarketItemDTO ToDTO()
    {
        return new MarketItemDTO()
        {
            name = this.name,
            description = this.description,
            price = this.price,
            icon = this.icon,
            isTool=this.isTool,
            requiredLevel=this.requiredLevel,
            toolObject=this.toolObject,
            itemObject=this.itemObject
        };
    }
}
[System.Serializable]
public class MarketItemDTO
{
    public string name;
    public string description;
    public int price;
    public Sprite icon;
    public bool isTool;
    public int requiredLevel;
    public GameObject itemObject;
    public Tool toolObject;
}
