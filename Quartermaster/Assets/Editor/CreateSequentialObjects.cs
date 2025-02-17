using UnityEditor;
using UnityEngine;

public class CreateSequentialObjects : EditorWindow
{
    private string baseName = "Barricade";
    private int count = 5;

    [MenuItem("Tools/Create Sequential Empty Objects")]
    public static void ShowWindow()
    {
        GetWindow<CreateSequentialObjects>("Create Sequential Objects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Sequential Empty Objects", EditorStyles.boldLabel);
        baseName = EditorGUILayout.TextField("Base Name", baseName);
        count = EditorGUILayout.IntField("Count", count);

        if (GUILayout.Button("Generate"))
        {
            GenerateObjects();
        }
    }

    private void GenerateObjects()
    {
        for (int i = 1; i <= count; i++)
        {
            GameObject obj = new GameObject($"{baseName}{i}_PREFAB");
            Undo.RegisterCreatedObjectUndo(obj, "Create Sequential Objects");
        }
    }
}
