using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class JoinRelayHandler : MonoBehaviour
{
    // Main menu buttons
    public Button createRelayButton;        // "Play" button
    public Button joinLobbyButton;          // "Join Lobby" button
    public Button controlsRelayButton;      // "Controls" button
    public Button settingsRelayButton;      // "Settings" button

    // JoinRelay panel
    public Button joinRelayButton;          // Button that opens the join relay panel
    public GameObject joinRelayByCode;      // The join relay panel

    // Controls panel
    public GameObject controlsSubPanel;     // The controls panel (initially inactive)

    // Settings panel
    public GameObject settingsPanel;        // The settings panel (initially inactive)

    // Return button (used to go back to the main menu)
    public Button returnRelayButton;        // "Return to Main Menu" button

    // (Optional) Text hover reference
    public TextHoverColor joinLobbyTextHover;

    void Start()
    {
        // Ensure that panels and the Return button are hidden initially.
        joinRelayByCode.SetActive(false);
        controlsSubPanel.SetActive(false);
        settingsPanel.SetActive(false);
        returnRelayButton.gameObject.SetActive(false);

        // Set up listeners
        joinRelayButton.onClick.AddListener(ShowJoinRelayByCode);
        controlsRelayButton.onClick.AddListener(ShowControlsMenu);
        joinLobbyButton.onClick.AddListener(HideControlsRelay);
        settingsRelayButton.onClick.AddListener(ShowSettingsMenu);
        returnRelayButton.onClick.AddListener(ReturnToMainMenu);
    }

    // --- JOIN RELAY FLOW ---
    void ShowJoinRelayByCode()
    {
        // Show join relay panel and the Return button.
        joinRelayByCode.SetActive(true);
        returnRelayButton.gameObject.SetActive(true);

        // Hide all main menu buttons.
        createRelayButton.gameObject.SetActive(false);
        joinRelayButton.gameObject.SetActive(false);
        controlsRelayButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
        settingsRelayButton.gameObject.SetActive(false);
    }

    // Called when Join Lobby is clicked.
    // Hides extra buttons (like Controls and Settings).
    void HideControlsRelay()
    {
        controlsRelayButton.gameObject.SetActive(false);
        settingsRelayButton.gameObject.SetActive(false);
    }

    // --- CONTROLS FLOW ---
    public void ShowControlsMenu()
    {
        Debug.Log("ShowControlsMenu called");
        // Activate the Controls panel and the Return button.
        controlsSubPanel.SetActive(true);
        returnRelayButton.gameObject.SetActive(true);

        // Hide all main menu buttons.
        createRelayButton.gameObject.SetActive(false);
        joinRelayButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
        controlsRelayButton.gameObject.SetActive(false);
        settingsRelayButton.gameObject.SetActive(false);
    }

    // --- SETTINGS FLOW ---
    public void ShowSettingsMenu()
    {
        Debug.Log("ShowSettingsMenu called");
        // Activate the Settings panel and the Return button.
        settingsPanel.SetActive(true);
        returnRelayButton.gameObject.SetActive(true);

        // Hide all main menu buttons.
        createRelayButton.gameObject.SetActive(false);
        joinRelayButton.gameObject.SetActive(false);
        joinLobbyButton.gameObject.SetActive(false);
        controlsRelayButton.gameObject.SetActive(false);
        settingsRelayButton.gameObject.SetActive(false);
    }

    // --- RETURN TO MAIN MENU ---
    void ReturnToMainMenu()
    {
        // Hide all sub-panels.
        joinRelayByCode.SetActive(false);
        controlsSubPanel.SetActive(false);
        settingsPanel.SetActive(false);
        returnRelayButton.gameObject.SetActive(false);

        // Show all main menu buttons.
        createRelayButton.gameObject.SetActive(true);
        joinRelayButton.gameObject.SetActive(true);
        joinLobbyButton.gameObject.SetActive(true);
        controlsRelayButton.gameObject.SetActive(true);
        settingsRelayButton.gameObject.SetActive(true);

        // Clear UI selection.
        EventSystem.current.SetSelectedGameObject(null);

        // Reset text hover colors if needed.
        if (joinLobbyTextHover != null)
        {
            StartCoroutine(ResetJoinLobbyTextColor());
        }
    }

    IEnumerator ResetJoinLobbyTextColor()
    {
        yield return null;
        joinLobbyTextHover.ResetColor();
    }
}

