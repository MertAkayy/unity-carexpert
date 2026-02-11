using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerData
{
    public string name;
    public int level;
    public int point;
    public int experience;
    public int reliability;
    
    public Vector3 position;

    public List<MarketItem> inventory;
    public float money;
    public MarketItem selectedItem ;
    
    // Serialize et
    //string json = JsonUtility.ToJson(playerData);

    // Deserialize et
    //PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);
    
    //kaydetmek için kullanılabilir.
    public PlayerData(string name)
    {
        this.name = name;
        level = 25;
        experience = 0;
        reliability = 0;
        position = Vector3.zero;
        inventory = new List<MarketItem>();
        money = 500;
        //gözden geçir
    }

    public bool PurchaseItem(MarketItem item)
    {
        if (money >= item.price)
        {
            money -= item.price;
            inventory.Add(item);
            return true;
        }

        return false;
    }
}
