using UnityEngine;
using UnityEngine.UI;

public class StartingCanvas_SCRIPT : MonoBehaviour
{
    [SerializeField] private GameObject tutorialCanvas;
    [SerializeField] private GameObject playerCanvas;

    [SerializeField] private Button startButton;

    public void StartButtonClicked() {
        // Hide the tutorial canvas and show the player canvas
        tutorialCanvas.SetActive(false);
        playerCanvas.SetActive(true);
    }
}
