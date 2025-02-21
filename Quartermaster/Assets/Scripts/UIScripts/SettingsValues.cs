using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingValueButton : MonoBehaviour
{
    public TMP_Text buttonText;        // Reference to the TextMeshPro text on the button

    private bool isEditing = false;
    private string inputValue = "";
    private string previousValue = "100%";

    void Start()
    {
        // Ensure buttonText is assigned.
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        previousValue = "100%";
        buttonText.text = previousValue;

        // Add click listener to start editing.
        GetComponent<Button>().onClick.AddListener(StartEditing);
    }

    void StartEditing()
    {
        isEditing = true;
        inputValue = "";
        buttonText.text = "Enter a value";
    }

    void Update()
    {
        if (!isEditing)
            return;

        // Confirm the value if Enter is pressed.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmValue();
            return;
        }
        
        // Cancel editing if Escape is pressed.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelEditing();
            return;
        }
    }

    // Use OnGUI to display a temporary text field over the button for input.
    void OnGUI()
    {
        if (isEditing)
        {
            // Convert the button's world position to screen coordinates.
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);
            // Adjust for GUI coordinates (y is inverted).
            screenPos.y = Screen.height - screenPos.y;
            
            // Define a rectangle for the text field.
            // You can adjust the width/height as needed.
            Rect textFieldRect = new Rect(screenPos.x, screenPos.y, 100, 30);

            // Ensure focus on this control.
            GUI.SetNextControlName("NumericInput");
            inputValue = GUI.TextField(textFieldRect, inputValue, 3); // Limit to 3 characters

            GUI.FocusControl("NumericInput");
        }
    }

    void ConfirmValue()
    {
        isEditing = false;
        if (int.TryParse(inputValue, out int value))
        {
            // Clamp the value between 1 and 100.
            value = Mathf.Clamp(value, 1, 100);
            buttonText.text = value.ToString() + "%";
            previousValue = buttonText.text;
        }
        else
        {
            // If the input is not a valid integer, reset to 100%.
            buttonText.text = "100%";
            previousValue = "100%";
        }
    }

    void CancelEditing()
    {
        isEditing = false;
        buttonText.text = previousValue;
    }
}

