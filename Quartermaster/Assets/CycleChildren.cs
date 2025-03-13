using UnityEngine;

public class CycleChildren : MonoBehaviour {
    private int currentIndex = 0;

    void Start() {
        // Disable all children except the first one
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(i == 0);
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.P)) {
            CycleNextChild();
        }
    }

    void CycleNextChild() {
        if (transform.childCount == 0) return;

        // Disable current child
        transform.GetChild(currentIndex).gameObject.SetActive(false);

        // Move to next child (loop back if at the end)
        currentIndex = (currentIndex + 1) % transform.childCount;

        // Enable new current child
        transform.GetChild(currentIndex).gameObject.SetActive(true);
    }
}
