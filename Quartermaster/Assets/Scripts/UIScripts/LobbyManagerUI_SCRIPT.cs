using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class LobbyManagerUI : MonoBehaviour {
    [SerializeField] private Button createRelayBtn;
    [SerializeField] private TMP_InputField joinRelayInput;
    [SerializeField] private Canvas lobbyMenuCanvas;

    [SerializeField] private Canvas playerUICanvas;
    [SerializeField] private IPRelay IPRelay;

    private void Awake() {

        createRelayBtn.onClick.AddListener(() => {
            IPRelay.CreateRelay();
            StartCoroutine(HideLobbyUI());
        });

        joinRelayInput.onEndEdit.AddListener((string s) => {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return)) {
                s = s.ToUpper();
                IPRelay.JoinRelay(s);
                HideLobbyUI();
            }
        });

        AddHoverEffect(createRelayBtn);
    }

    public IEnumerator HideLobbyUI() {
        lobbyMenuCanvas.enabled = false;
        playerUICanvas.enabled = true;

        yield return new WaitUntil(() => Camera.main != null && Camera.main.enabled);
        
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
