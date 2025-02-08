using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.Windows;
using System;
using TMPro;

public class NetworkManagerUI : MonoBehaviour
{

    [SerializeField] private Button serverBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button hostBtn;

    [SerializeField] private Button createRelayBtn;
    [SerializeField] private TMP_InputField joinRelayInput;

    [SerializeField] private Canvas canvas;

    private void Awake() {
        serverBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartServer();
            canvas.enabled = false;
        });

        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            canvas.enabled = false;
        });

        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            canvas.enabled = false;
        });

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
