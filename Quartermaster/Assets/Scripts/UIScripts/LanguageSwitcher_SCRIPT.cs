using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageSwitcher : MonoBehaviour
{
    public void SetLocale(string localeCode)
    {
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            if (locale.Identifier.Code.Equals(localeCode, System.StringComparison.OrdinalIgnoreCase))
            {
                LocalizationSettings.SelectedLocale = locale;
                return;
            }
        }
        Debug.LogError("Locale not found: " + localeCode);
    }
    public void SetEnglish() 
    {
        SetLocale("en");
    }

    public void SetChinese() 
    {
        SetLocale("zh-Hans");
    }

    public void SetArabic() 
    {
        SetLocale("ar");
    }
}