using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(IssueDataBase))]
public class IssueDataBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        IssueDataBase db = (IssueDataBase)target;

        if (GUILayout.Button("Tüm Issue'ları Otomatik Ekle"))
        {
            AddAllIssuesToDatabase(db);
        }
    }

    private void AddAllIssuesToDatabase(IssueDataBase db)
    {
        string[] guids = AssetDatabase.FindAssets("t:Issue");
        List<Issue> foundIssues = new List<Issue>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Issue issue = AssetDatabase.LoadAssetAtPath<Issue>(path);
            if (issue != null)
            {
                foundIssues.Add(issue);
            }
        }

        db.issues = foundIssues;
        EditorUtility.SetDirty(db); // Değişiklikleri kaydet
        AssetDatabase.SaveAssets();
        GameLogger.Log($"A total of {foundIssues.Count} issues have been added to the database.");
    }
}