using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Rendering.Universal;

public class ChildAlphabeticalSorter : EditorWindow
{
    // serialized data
    private GameObject parentObject;

    private const string k_ImageDown = "Assets/Editor/Icons/down.png";
    private const string k_ImageUp = "Assets/Editor/Icons/up.png";
    private Texture2D _buttonIconDown;
    private Texture2D _buttonIconUp;

    private bool showHelp = false;
    

    double _successEndTime;

    // menu item to open window
    [MenuItem("Tools/Child Alphabetical Sorter")]
    static void OpenWindow()
    {
        var window = GetWindow<ChildAlphabeticalSorter>("Child Alphabetical Sorter");
        window.minSize = new Vector2(300, 200);
    }

    void OnEnable()
    {
        _buttonIconDown = AssetDatabase.LoadAssetAtPath<Texture2D>(k_ImageDown);
        _buttonIconUp = AssetDatabase.LoadAssetAtPath<Texture2D>(k_ImageUp);

    }

    void OnDisable()
    {
    }




    // main GUI loop
    void OnGUI()
    {
        //style definitions
        var layout_vertical_style = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 80,
            fixedHeight = 100
        };

        //help section
        showHelp = EditorGUILayout.Foldout(showHelp, "Help & Instructions");
        if (showHelp)
        {
            EditorGUILayout.HelpBox(
                "fuck you there's no help here", MessageType.Info
            );
            GUILayout.Space(1);
        }



        
        EditorGUILayout.LabelField("Sorter data", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        EditorGUIUtility.labelWidth = 100;
        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true, GUILayout.Width(150));
        EditorGUILayout.Space();

        var button_style = new GUIStyle(GUI.skin.button)
        {
            hover = {
                textColor = Color.red,
            },

            fontStyle = FontStyle.Bold
        };


        if (GUILayout.Button("CLEAR PARENT", button_style))
        {
            parentObject = null;
        }

        EditorGUIUtility.labelWidth = 100;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
        GUI.enabled = parentObject != null;

        var content_down = new GUIContent(_buttonIconDown);
        var buttonWidth = GUILayout.Width(60);
        var buttonHeight = GUILayout.Height(60);





        EditorGUILayout.BeginHorizontal();

        // sort descending
        GUILayout.BeginVertical(layout_vertical_style);
        if (GUILayout.Button(content_down, buttonWidth, buttonHeight))
        {
            SortChildrenAlphabetically();
        }
        GUILayout.Label("Sort\nDescending", EditorStyles.label);
        GUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();




        GUI.enabled = true;

        if (EditorApplication.timeSinceStartup < _successEndTime)
        {
            EditorGUILayout.HelpBox("Sort completed succesfully!", MessageType.Info);
            Repaint();
        }

        if (parentObject == null)
        {
            EditorGUILayout.HelpBox("Please assign a parent object with children to sort.", MessageType.Warning);
        }
    }

    // ----------------------------- GUI LAYOUT FUNCTIONS ----------------------------- //




    // --------------------- THE FUNCTIONS THAT ACTUALLY DO STUFF --------------------- //
    void SortChildrenAlphabetically()
    {
        if (parentObject == null) return;

        // Record an undo so the entire reorder can be undone in one step.
        Undo.RegisterCompleteObjectUndo(parentObject.transform, "Sort Children Alphabetically");

        // 1) Build a list of all child Transforms
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < parentObject.transform.childCount; i++)
        {
            children.Add(parentObject.transform.GetChild(i));
        }

        // 2) Sort that list with a custom comparer that handles "(number)" suffixes
        children.Sort(CompareByNameOrNumericSuffix);

        // 3) Re‐assign sibling indices in one pass
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(i);
        }

        Debug.Log($"Sorted {children.Count} children under '{parentObject.name}'.");
        _successEndTime = EditorApplication.timeSinceStartup + 2.0;
    }
    static int CompareByNameOrNumericSuffix(Transform a, Transform b)
    {
        string nameA = a.name;
        string nameB = b.name;

        // Regex to catch "BaseName (123)" or "FooBar(  45 )"
        var regex = new Regex(@"^(.+?)\s*\(\s*(\d+)\s*\)\s*$");

        var matchA = regex.Match(nameA);
        var matchB = regex.Match(nameB);

        bool hasNumA = matchA.Success;
        bool hasNumB = matchB.Success;

        // If both have a "(number)" suffix
        if (hasNumA && hasNumB)
        {
            string baseA = matchA.Groups[1].Value;
            string baseB = matchB.Groups[1].Value;

            int numA = int.Parse(matchA.Groups[2].Value);
            int numB = int.Parse(matchB.Groups[2].Value);

            // If the base names are identical, compare by numeric value
            if (string.Equals(baseA, baseB, System.StringComparison.OrdinalIgnoreCase))
            {
                return numA.CompareTo(numB);
            }
            // Otherwise, compare the base names alphabetically
            return string.Compare(baseA, baseB, System.StringComparison.OrdinalIgnoreCase);
        }

        // If only A has "(number)" but B does not
        if (hasNumA && !hasNumB)
        {
            // Extract A's base
            string baseA = matchA.Groups[1].Value;
            // If baseA equals the full B name, we want B (no suffix) to come first
            if (string.Equals(baseA, nameB, System.StringComparison.OrdinalIgnoreCase))
                return 1;  // B < A
            // Otherwise, compare A's base vs B normally
            return string.Compare(baseA, nameB, System.StringComparison.OrdinalIgnoreCase);
        }

        // If only B has "(number)" but A does not
        if (!hasNumA && hasNumB)
        {
            // Extract B's base
            string baseB = matchB.Groups[1].Value;
            // If baseB equals the full A name, we want A (no suffix) to come first
            if (string.Equals(nameA, baseB, System.StringComparison.OrdinalIgnoreCase))
                return -1;  // A < B
            // Otherwise, compare A vs B's base normally
            return string.Compare(nameA, baseB, System.StringComparison.OrdinalIgnoreCase);
        }

        // Neither has a numeric suffix—just do a normal alphabetical comparison
        return string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
    }

}
