using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CustomEditorIcon : MonoBehaviour
{
    [Header("Icon Settings")]
    public Texture2D icon;

    public float verticalOffset = 1.5f;

    [Header("Size Scaling")]
    public float minSize = 48f;
    public float maxSize = 48f;
    public float sizeMultiplier = 4f;


    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (icon == null) return;

        Vector3 worldPos = transform.position + Vector3.up * verticalOffset;

        Camera sceneCam = SceneView.currentDrawingSceneView?.camera;
        if (sceneCam == null) return;

        float dist = Vector3.Distance(sceneCam.transform.position, worldPos);

        float rawSize = dist * sizeMultiplier;
        float sizeOnScreen = Mathf.Clamp(rawSize, minSize, maxSize);

        Vector3 screenPos3D = sceneCam.WorldToScreenPoint(worldPos);
        if (screenPos3D.z <= 0f) return; // behind camera

        Vector2 screenPos = new Vector2(
            screenPos3D.x,
            sceneCam.pixelHeight - screenPos3D.y
        );

        Rect iconRect = new Rect(
            screenPos.x - sizeOnScreen * 0.5f,
            screenPos.y - sizeOnScreen * 0.5f,
            sizeOnScreen,
            sizeOnScreen
        );

        Handles.BeginGUI();
        GUI.DrawTexture(iconRect, icon);
        Handles.EndGUI();
#endif
    }
}
