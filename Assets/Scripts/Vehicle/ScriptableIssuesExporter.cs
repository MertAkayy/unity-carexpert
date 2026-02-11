using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ScriptableIssuesExporter
{
    [MenuItem("Tools/Export All Issues To JSON")]
    public static void ExportAllItemsToJSON()
    {
        string[] guids = AssetDatabase.FindAssets("t:Issue");
        List<IssueDto> allItems = new List<IssueDto>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Issue item = AssetDatabase.LoadAssetAtPath<Issue>(path);
            if (item != null)
            {
                allItems.Add(item.ToDTO()); // Convert to DTO before serialization
            }
        }

        string json = JsonUtility.ToJson(new ItemDTOListWrapper { items = allItems }, true);
        string exportPath = Path.Combine(Application.dataPath, "Resources/issue_database.json");
        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)); // Ensure directory exists
        File.WriteAllText(exportPath, json);
        AssetDatabase.Refresh(); // Refresh Unity to recognize the new file

        Debug.Log("Exported " + allItems.Count + " items to JSON at: " + exportPath);
    }

    [System.Serializable]
    private class ItemDTOListWrapper
    {
        public List<IssueDto> items;
    }
}