using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabGroup : MonoBehaviour {
    public List<TabButton> tabButtons;

    public Color tabIdleColor;
    public Color tabHoverColor;
    public Color tabActiveColor;

    public TabButton selectedTabRef;
    public List<GameObject> objectsToSwap;

    void Start() {
        OnTabSelected(selectedTabRef);
    }

    public void Subscribe(TabButton button) {
        if (tabButtons == null) {
            tabButtons = new List<TabButton>();
        }

        tabButtons.Add(button);
        button.background.color = tabIdleColor;
    }

    public void OnTabEnter(TabButton button) {
        if (selectedTabRef == null || button != selectedTabRef) {
            button.background.color = tabHoverColor;
        }
    }

    public void OnTabExit(TabButton button) {
        if (selectedTabRef == null || button != selectedTabRef) {
            button.background.color = tabIdleColor;
        }
    }

    public void OnTabSelected(TabButton button) {
        selectedTabRef = button;
        ResetTabs();
        button.background.color = tabActiveColor;

        int index = button.transform.GetSiblingIndex();
        for (int i = 0; i < objectsToSwap.Count; i++) {
            objectsToSwap[i].SetActive(i == index);
        }
    }

    public void ResetTabs() {
        foreach (TabButton button in tabButtons) {
            if (selectedTabRef != null && button == selectedTabRef) { continue; }

            button.background.color = tabIdleColor;
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            int direction = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
            SwitchTab(direction);
        }
    } 

    private void SwitchTab(int direction) {
        if (tabButtons.Count == 0) return;

        int currIndex = selectedTabRef != null ? tabButtons.IndexOf(selectedTabRef) : -1;
        int nextIndex = (currIndex + direction + tabButtons.Count) % tabButtons.Count;
        OnTabSelected(tabButtons[nextIndex]);
    }

}
