using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingTextAnimator : MonoBehaviour
{
    [SerializeField] private TMP_Text loadingText;  // Reference to the TextMeshProUGUI component.
    [SerializeField] private string baseText = "Loading";
    [SerializeField] private float delay = 0.01f;      // Delay in seconds between updates.

    private void Start()
    {
        // If loadingText isn't assigned in the Inspector, try to get the component from this GameObject.
        if (loadingText == null)
        {
            loadingText = GetComponent<TMP_Text>();
        }

        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        int dotCount = 0;
        while (true)
        {
            dotCount = (dotCount + 1) % 4; // Cycles through 0, 1, 2, 3 dots.
            string dots = new string('.', dotCount);
            loadingText.text = baseText + dots;
            yield return new WaitForSeconds(delay);
        }
    }
}


