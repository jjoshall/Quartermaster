using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ReplaceWithObject : EditorWindow
{
    //copies location of object #1, instantiates copy of object #2 at same position, deletes object #1

    private GameObject newObject;

    private string helpMsg1 = "Please select objects to replace.";
    private string helpMsg2 = "Please select an object to replace with.";

    [MenuItem("Tools/Object Replacement")]
    public static void ShowWindow()
    {
        ReplaceWithObject window = GetWindow<ReplaceWithObject>("Object Replacement");
        window.minSize = new Vector2(300, 400);
        window.Show();

    }

    void OnGUI()
    {
        newObject = (GameObject)EditorGUILayout.ObjectField(
            "Replacement Object", newObject, typeof(GameObject), false
        );



        if (!HasSelectedObjects() || newObject == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Replace Objects"))
        {
            ReplaceObjects();
        }
        
        if (!HasSelectedObjects() || newObject == null)
        {
            string output = (!HasSelectedObjects() ? helpMsg1 : "") + "\n" + (newObject == null ? helpMsg2 : "");

            EditorGUILayout.HelpBox(output, MessageType.Warning);
        }

    }

    private void ReplaceObjects()
    {
        List<GameObject> selected = new List<GameObject>(Selection.gameObjects);

        Undo.RegisterCompleteObjectUndo(selected.ToArray(), "Objects Replaced");

        foreach (GameObject obj in selected)
        {
            Transform objParent = obj.transform.parent;
            bool hasParent = objParent != null;

            Vector3 tmpPos = obj.transform.position;
            GameObject newObj;

            Undo.DestroyObjectImmediate(obj);


            newObj = PrefabUtility.InstantiatePrefab(newObject) as GameObject;

            if (hasParent) newObj.transform.SetParent(objParent, true);
            newObj.transform.position = tmpPos;



            Undo.RegisterCreatedObjectUndo(newObj, "Created Replacement Tile");


        }
    }

    private bool HasSelectedObjects()
    {
        return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
    }
}
