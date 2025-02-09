using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkManagerUI : MonoBehaviour {


    [SerializeField] private Button createRelayBtn;
    [SerializeField] private TMP_InputField joinRelayInput;

    [SerializeField] private Canvas canvas;

    private void Awake() {

        createRelayBtn.onClick.AddListener(() => {
            GameObject.Find("IPRelay").GetComponent<IPRelay>().CreateRelay();
        });

        joinRelayInput.onEndEdit.AddListener((string s) => {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return)) {
                GameObject.Find("IPRelay").GetComponent<IPRelay>().JoinRelay(s);
            }
        });
    }
}
