using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    }

    public IEnumerator HideLobbyUI() {
        lobbyMenuCanvas.enabled = false;
        playerUICanvas.enabled = true;

        yield return new WaitUntil(() => Camera.main != null && Camera.main.enabled);
        
        yield return new WaitForEndOfFrame();
    }
}
