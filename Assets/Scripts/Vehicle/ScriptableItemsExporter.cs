using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ScriptableItemsExporter : ScriptableObject
{
    [MenuItem("Tools/Export All MArket Items To JSON")]
    public static void ExportAllItemsToJSON()
    {
        string[] guids = AssetDatabase.FindAssets("t:MarketItem"); 
        List<MarketItemDTO> allItems = new List<MarketItemDTO>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MarketItem item = AssetDatabase.LoadAssetAtPath<MarketItem>(path);
            allItems.Add(item.ToDTO());
        }

        string json = JsonUtility.ToJson(new ItemDTOListWrapper { items = allItems }, true);
        File.WriteAllText(Application.dataPath + "/market_items_database.json", json);

        Debug.Log("Exported " + allItems.Count + " Market Items to JSON.");
    }

    [System.Serializable]
    private class ItemDTOListWrapper
    {
        public List<MarketItemDTO> items;
    }
}
