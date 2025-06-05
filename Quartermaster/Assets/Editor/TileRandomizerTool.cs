using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TileRandomizerTool : EditorWindow
{
    private static TileRandomizerSettings settings;
    private const string SETTINGS_PATH = "Assets/Editor/TileRandomizerSettings.asset";

    private Vector2 scrollPosition;
    private bool showHelp = false;

    [MenuItem("Tools/Tile Randomizer")]
    public static void ShowWindow()
    {
        TileRandomizerTool window = GetWindow<TileRandomizerTool>("Tile Randomizer");
        window.minSize = new Vector2(300, 400);
        LoadSettings();
        window.Show();
    }

    private static void LoadSettings()
    {
        settings = AssetDatabase.LoadAssetAtPath<TileRandomizerSettings>(SETTINGS_PATH);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<TileRandomizerSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();
        }
    }

    void OnGUI()
    {
        if (settings == null)
            LoadSettings();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Tile Randomizer Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        showHelp = EditorGUILayout.Foldout(showHelp, "Help & Instructions");
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "1. Assign your 4 tile prefabs below\n" +
                "2. Select tiles in the scene you want to randomize\n" +
                "3. Click 'Randomize Selected Tiles'\n\n" +
                "The tool will replace selected tiles with random variants, " +
                "adding height and rotation variations based on your settings.",
                MessageType.Info
            );
            GUILayout.Space(10);
        }

        GUILayout.Label("Tile Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign your 4 tile prefabs here:", MessageType.None);

        for (int i = 0; i < settings.tilePrefabs.Length; i++)
        {
            settings.tilePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Tile {i + 1}", settings.tilePrefabs[i], typeof(GameObject), false);
        }

        GUILayout.Space(10);

        GUILayout.Label("Variation Settings", EditorStyles.boldLabel);
        settings.heightVariationChance = EditorGUILayout.Slider("Height Variation Chance", settings.heightVariationChance, 0f, 1f);
        settings.rotationVariationChance = EditorGUILayout.Slider("Rotation Variation Chance", settings.rotationVariationChance, 0f, 1f);
        settings.maxHeightVariation = EditorGUILayout.Slider("Max Height Variation", settings.maxHeightVariation, 0f, 0.1f);

        GUILayout.Label("Rotation Axis Locks", EditorStyles.miniBoldLabel);
        settings.lockRotationX = EditorGUILayout.Toggle("Lock X Rotation", settings.lockRotationX);
        settings.lockRotationY = EditorGUILayout.Toggle("Lock Y Rotation", settings.lockRotationY);
        settings.lockRotationZ = EditorGUILayout.Toggle("Lock Z Rotation", settings.lockRotationZ);

        GUILayout.Space(5);
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        settings.baseHeight = EditorGUILayout.FloatField("Base Height", settings.baseHeight);
        settings.gridSize = EditorGUILayout.FloatField("Grid Size", settings.gridSize);

        GUILayout.Space(20);

        GUI.enabled = HasValidPrefabs() && HasSelectedTiles();
        if (GUILayout.Button("Randomize Selected Tiles", GUILayout.Height(30)))
        {
            RandomizeSelectedTiles();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        GUILayout.Label("Status", EditorStyles.boldLabel);

        if (!HasValidPrefabs())
        {
            EditorGUILayout.HelpBox("Please assign all 4 tile prefabs!", MessageType.Warning);
        }
        else if (!HasSelectedTiles())
        {
            EditorGUILayout.HelpBox("Select tiles in the scene to randomize.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"Ready to randomize {Selection.gameObjects.Length} selected tiles.", MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private bool HasValidPrefabs()
    {
        return settings.tilePrefabs.All(prefab => prefab != null);
    }

    private bool HasSelectedTiles()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }

    private void RandomizeSelectedTiles()
    {
        if (!HasValidPrefabs() || !HasSelectedTiles())
            return;

        List<GameObject> selected = new List<GameObject>(Selection.gameObjects);
        List<GameObject> newSelection = new List<GameObject>();

        Undo.RegisterCompleteObjectUndo(selected.ToArray(), "Randomize Tiles");

        foreach (GameObject oldTile in selected)
        {
            Transform oldTransform = oldTile.transform;
            Transform parent = oldTransform.parent;

            Vector3 snappedPosition = SnapToGrid(oldTransform.position);
            if (Random.value < settings.heightVariationChance)
            {
                snappedPosition.y += Random.Range(-settings.maxHeightVariation, settings.maxHeightVariation);
            }

            Quaternion rotation = GetRandomizedRotation();

            Undo.DestroyObjectImmediate(oldTile);

            GameObject newTile = PrefabUtility.InstantiatePrefab(
                settings.tilePrefabs[Random.Range(0, settings.tilePrefabs.Length)]
            ) as GameObject;

            newTile.transform.position = snappedPosition;
            newTile.transform.rotation = rotation;
            if (parent != null)
                newTile.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(newTile, "Create Randomized Tile");

            newSelection.Add(newTile);
        }

        Selection.objects = newSelection.ToArray();
        Debug.Log($"Randomized {newSelection.Count} tiles successfully!");
    }

    private Quaternion GetRandomizedRotation()
    {
        float xRot = settings.lockRotationX ? -90f : 90f * Random.Range(0, 4);
        float yRot = settings.lockRotationY ? 0f : 90f * Random.Range(0, 4);
        float zRot = settings.lockRotationZ ? 0f : 90f * Random.Range(0, 4);
        return Quaternion.Euler(xRot, yRot, zRot);
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        float snappedX = Mathf.Round(position.x / settings.gridSize) * settings.gridSize;
        float snappedZ = Mathf.Round(position.z / settings.gridSize) * settings.gridSize;
        return new Vector3(snappedX, settings.baseHeight, snappedZ);
    }
}
