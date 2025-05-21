using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LobbyManagerUI : MonoBehaviour {
    [SerializeField] private Button createRelayBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private TMP_InputField joinRelayInput;
    [SerializeField] private Canvas lobbyMenuCanvas;

    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private IPRelay IPRelay;

    public UnityEvent onQuitToMainMenu;

    private void Awake()
    {

        joinRelayInput.onEndEdit.AddListener((string s) =>
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                s = s.ToUpper();
                IPRelay.JoinRelay(s);
                HideLobbyUI();
            }
        });

        quitBtn.onClick.AddListener(() =>
        {
            //return to main menu scene
            SceneManager.LoadScene("MainMenu_SCENE");
            onQuitToMainMenu?.Invoke();
        });


        AddHoverEffect(createRelayBtn);
        AddHoverEffect(quitBtn);
    }

    public IEnumerator HideLobbyUI() {
        Debug.LogError("Starting hide lobby ui");
        lobbyMenuCanvas.enabled = false;
        playerUICanvas.enabled = true;

        yield return new WaitUntil(() => Camera.main != null && Camera.main.enabled);
        Debug.LogError("main cam enabled upon join");

        yield return new WaitForEndOfFrame();
    }

    private void AddHoverEffect(Button button) {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText == null) {
            Debug.LogWarning($"No TextMeshProUGUI found in {button.name}'s children.");
            return;
        }

        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { ChangeTextColor(buttonText, Color.white); });

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { ChangeTextColor(buttonText, Color.black); });

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    private void ChangeTextColor(TextMeshProUGUI text, Color color) {
        text.color = color;
    }
}
