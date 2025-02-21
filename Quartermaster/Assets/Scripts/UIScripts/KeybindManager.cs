using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyRebindManager : MonoBehaviour
{
    public TMP_Text buttonText;
    private bool isListeningForKey = false;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(StartRebinding);
    }

    void StartRebinding()
    {
        isListeningForKey = true;
        buttonText.text = "Press a Key...";
    }

    void Update()
    {
        if (isListeningForKey && Input.anyKeyDown)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    buttonText.text = key.ToString();
                    isListeningForKey = false;
                    break;
                }
            }
        }
    }
}
