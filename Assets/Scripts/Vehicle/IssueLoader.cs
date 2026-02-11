using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    [System.Serializable]
    public class IssueDatabase
    {
        public IssueDTO[] items;
    }

    [System.Serializable]
    public class IssueDTO
    {
        public string FailureName;
        public string Description;
        public int AvailableLevel;
        public AffectedPartType AffectedPartType;
        public int PossibilityWeight;
        public string ObdCode;
        public string RequiredTool; // Geçici olarak string, JSON'dan gelen değeri alacak
    }

    public static class IssueLoader
    {
        private const string JsonResourcePath = "issue_database"; // JSON dosyasının Resources klasöründeki adı (uzantısız)
        private const string AssetSavePath = "Assets/Scripts/Vehicle/Issues/"; // ScriptableObject’ların kaydedileceği klasör

        [MenuItem("Tools/Load Issues to ScriptableObjects")]
        public static void LoadAndCreateScriptableObjects()
        {
            // Hedef klasörü oluştur (eğer yoksa)
            if (!AssetDatabase.IsValidFolder(AssetSavePath))
            {
                AssetDatabase.CreateFolder("Assets/Scripts/Vehicle", "Issues");
            }

            List<Issue> createdIssues = new List<Issue>();

            try
            {
                // JSON dosyasını Resources’tan oku
                TextAsset jsonFile = Resources.Load<TextAsset>(JsonResourcePath);
                if (jsonFile == null)
                {
                    Debug.LogError($"JSON file not found in Resources: {JsonResourcePath}.json");
                    return;
                }

                // JSON’u parse et
                IssueDatabase database = JsonUtility.FromJson<IssueDatabase>(jsonFile.text);
                if (database == null || database.items == null)
                {
                    Debug.LogError("Failed to parse JSON or no items found.");
                    return;
                }

                // Her bir IssueDTO’yu ScriptableObject’a dönüştür
                foreach (var item in database.items)
                {
                    // Yeni ScriptableObject oluştur
                    Issue issueAsset = ScriptableObject.CreateInstance<Issue>();
                    issueAsset.FailureName = item.FailureName;
                    issueAsset.Description = item.Description;
                    issueAsset.AvailableLevel = item.AvailableLevel;
                    issueAsset.AffectedPartType = item.AffectedPartType;
                    issueAsset.PossibilityWeight = item.PossibilityWeight;
                    issueAsset.ObdCode = string.IsNullOrEmpty(item.ObdCode) ? "PXXXX" : item.ObdCode;

                    // RequiredTool’u string’den Tool enum’una dönüştür
                    try
                    {
                        Debug.Log($"Parsing RequiredTool for {item.FailureName}: {item.RequiredTool}");
                        if (string.IsNullOrEmpty(item.RequiredTool))
                        {
                            Debug.LogWarning($"RequiredTool is empty or null for {item.FailureName}. Setting to Tool.Null.");
                            issueAsset.RequiredTool = Tool.Null;
                        }
                        else if (Enum.TryParse<Tool>(item.RequiredTool, true, out Tool parsedTool))
                        {
                            issueAsset.RequiredTool = parsedTool;
                        }
                        else
                        {
                            Debug.LogWarning($"Invalid RequiredTool value '{item.RequiredTool}' for {item.FailureName}. Setting to Tool.Null.");
                            issueAsset.RequiredTool = Tool.Null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing RequiredTool for {item.FailureName}: {ex.Message}. Setting to Tool.Null.");
                        issueAsset.RequiredTool = Tool.Null;
                    }

                    // Benzersiz dosya adı oluştur (FailureName temel alınarak)
                    string assetPath = $"{AssetSavePath}{item.FailureName}.asset";
                    // Aynı isimde bir asset varsa, üzerine yazmayı önlemek için numaralandır
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                    // ScriptableObject’ı kaydet
                    AssetDatabase.CreateAsset(issueAsset, assetPath);
                    createdIssues.Add(issueAsset);
                }

                // AssetDatabase’i yenile
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Created {createdIssues.Count} Issue ScriptableObjects in {AssetSavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing JSON or creating ScriptableObjects: {ex.Message}");
            }
        }

        // Oyun içinde ScriptableObject’ları yüklemek için yardımcı metod
        public static List<Issue> LoadAllIssues()
        {
            List<Issue> issues = new List<Issue>();
            string[] guids = AssetDatabase.FindAssets("t:Issue", new[] { "Assets/Scripts/Vehicle/Issues" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Issue issue = AssetDatabase.LoadAssetAtPath<Issue>(path);
                if (issue != null)
                {
                    issues.Add(issue);
                }
            }
            return issues;
        }
    }
}