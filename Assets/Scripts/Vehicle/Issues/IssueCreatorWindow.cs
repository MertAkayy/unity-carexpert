using UnityEngine;
using UnityEditor;

public class IssueCreatorWindow : EditorWindow
{
    string failureName = "Yeni Arıza";
    string description = "Açıklama";
    int level = 1;
    int possibilityWeight = 1;
    AffectedPartType selectedPartType = AffectedPartType.None;
    string obdCode = "";

    [MenuItem("Tools/Issue Creator")]
    public static void ShowWindow()
    {
        GetWindow<IssueCreatorWindow>("Issue Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Yeni Arıza Oluştur", EditorStyles.boldLabel);

        failureName = EditorGUILayout.TextField("Arıza Adı", failureName);
        description = EditorGUILayout.TextField("Açıklama", description);
        level = EditorGUILayout.IntField("Seviye", level);
        possibilityWeight = EditorGUILayout.IntField("Olasılık Ağırlığı", possibilityWeight);
        selectedPartType = (AffectedPartType)EditorGUILayout.EnumPopup("Etkilenen Parça", selectedPartType);
        obdCode =EditorGUILayout.TextField("OBD Code", obdCode);

        EditorGUILayout.Space();

        if (GUILayout.Button("ScriptableObject Oluştur"))
        {
            if (string.IsNullOrWhiteSpace(failureName))
            {
                GameLogger.LogWarning("Arıza adı boş olamaz.");
                return;
            }

            Issue issue = ScriptableObject.CreateInstance<Issue>();
            issue.FailureName = failureName;
            issue.Description = description;
            issue.AvailableLevel = level;
            issue.PossibilityWeight = possibilityWeight;
            issue.AffectedPartType = selectedPartType;
            issue.ObdCode= obdCode;

            string safeName = failureName.Replace(" ", "_");
            string path = $"Assets/Scripts/Vehicle/Issues/{safeName}.asset";

            AssetDatabase.CreateAsset(issue, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Yeni arıza oluşturuldu: {safeName}");
        }
    }
}