using System.Collections;
using UnityEngine;
using TMPro;

public class GlitchOutlineEffect : MonoBehaviour
{
    public TextMeshProUGUI textMesh;  
    public float glitchInterval = 2f;  // Time between glitches
    public float glitchDuration = 0.2f; // How long the glitch lasts
    public float maxOutlineThickness = 1f; // How thick the outline can get during glitch

    private Color[] glitchColors = new Color[]
    {
        new Color(0.1f, 1f, 1f),  // Neon Blue
        new Color(1f, 0.3f, 0.9f), // Neon Pink
        new Color(1f, 1f, 0.3f)  // Neon Yellow
    };

    private bool isGlitching = false;

    void Start()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshProUGUI>();

        // Ensure outline is enabled
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = Color.black; // Default outline color

        StartCoroutine(GlitchEffect());
    }

    IEnumerator GlitchEffect()
    {
        while (true)
        {
            yield return new WaitForSeconds(glitchInterval);

            if (!isGlitching)
            {
                isGlitching = true;
                StartCoroutine(ApplyGlitch());
            }
        }
    }

    IEnumerator ApplyGlitch()
    {
        float originalOutline = textMesh.outlineWidth;
        Color originalOutlineColor = textMesh.outlineColor;

        int glitchCount = Random.Range(1, 3); // Number of times to glitch

        for (int i = 0; i < glitchCount; i++)
        {
            textMesh.outlineColor = glitchColors[Random.Range(0, glitchColors.Length)];
            textMesh.outlineWidth = Random.Range(originalOutline, maxOutlineThickness);

            yield return new WaitForSeconds(glitchDuration / glitchCount);
        }

        // Restore original outline
        textMesh.outlineWidth = originalOutline;
        textMesh.outlineColor = originalOutlineColor;

        isGlitching = false;
    }
}
