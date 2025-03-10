using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LoadingTextAnimator : MonoBehaviour
{
    [SerializeField] private TMP_Text loadingText;   // Reference to the TMP component.
    [SerializeField] private LocalizedString baseText; // Localized string for the base text.
    [SerializeField] private float delay = 0.5f;         // Delay between updates.

    private string localizedBaseText = "";
    private bool prependDots = false;                  // Flag to decide dot placement.

    private IEnumerator Start()
    {
        // Ensure we have a reference to the TMP component.
        if (loadingText == null)
        {
            loadingText = GetComponent<TMP_Text>();
        }

        // Determine language-specific formatting. For example, if the locale is Arabic:
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale != null && currentLocale.Identifier.Code == "ar")
        {
            prependDots = true;
        }
        else
        {
            prependDots = false;
        }

        // Wait for the localized string to load asynchronously.
        AsyncOperationHandle<string> handle = baseText.GetLocalizedStringAsync();
        yield return handle;
        localizedBaseText = handle.Result;

        // Now that we have the localized text, start the animation.
        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        int dotCount = 0;
        while (true)
        {
            dotCount = (dotCount + 1) % 4; // Cycle through 0 to 3 dots.
            string dots = new string('.', dotCount);

            // For RTL languages like Arabic, you might want the dots to come first.
            if (prependDots)
            {
                loadingText.text = dots + localizedBaseText;
            }
            else
            {
                loadingText.text = localizedBaseText + dots;
            }
            yield return new WaitForSeconds(delay);
        }
    }
}



