using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabGroup : MonoBehaviour {
    public List<CustomTabButton> customTabButtons;
    public Color tabIdleColor;
    public Color tabHoverColor;
    public Color tabActiveColor;
    public CustomTabButton selectedTabRef;
    public List<GameObject> objectsToSwap;

    void Start() {
        if (customTabButtons != null && customTabButtons.Count > 0 && selectedTabRef == null) {
            OnTabSelected(customTabButtons[0]);
        }
    }

    public void Subscribe(CustomTabButton button) {
        if (customTabButtons == null) {
            customTabButtons = new List<CustomTabButton>();
        }
        customTabButtons.Add(button);
        Debug.Log($"Subscribed {button.name} to TabGroup {gameObject.name}");
        button.background.color = tabIdleColor;
    }

    public void OnTabEnter(CustomTabButton button) {
        if (selectedTabRef == null || button != selectedTabRef) {
            button.background.color = tabHoverColor;
        }
    }

    public void OnTabExit(CustomTabButton button) {
        if (selectedTabRef == null || button != selectedTabRef) {
            button.background.color = tabIdleColor;
        }
    }

    public void OnTabSelected(CustomTabButton button) {
        selectedTabRef = button;


        ResetTabs();

        // Reset text color for the newly selected tab.
        TabHoverColor_TMP hoverScript = button.GetComponentInChildren<TabHoverColor_TMP>();
        if (hoverScript != null) {
            hoverScript.ResetTextColor();
        }

        button.background.color = tabActiveColor;

        int index = button.transform.GetSiblingIndex();
        for (int i = 0; i < objectsToSwap.Count; i++) {
            objectsToSwap[i].SetActive(i == index);
        }
    }


    public void ResetTabs() {
        foreach (CustomTabButton button in customTabButtons) {
            if (selectedTabRef != null && button == selectedTabRef)
                continue;
            button.background.color = tabIdleColor;

            // Reset the text color for each non-selected tab.
            TabHoverColor_TMP hoverScript = button.GetComponentInChildren<TabHoverColor_TMP>();
            if (hoverScript != null) {
                hoverScript.ResetTextColor();
            }
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            int direction = Input.GetKey(KeyCode.LeftShift) ? 1 : -1;
            SwitchTab(direction);
        }
    }

    private void SwitchTab(int direction) {
        if (customTabButtons.Count == 0) return;

        int currIndex = selectedTabRef != null ? customTabButtons.IndexOf(selectedTabRef) : -1;
        int nextIndex = (currIndex + direction + customTabButtons.Count) % customTabButtons.Count;
        OnTabSelected(customTabButtons[nextIndex]);
    }
}
