using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuidebookMenu_SCRIPT : MonoBehaviour
{
    [SerializeField] private Button returnToMainMenuBtn;
    [SerializeField] private GameObject guidebookCanvas;
    [SerializeField] private GameObject mainMenuCanvas;

    [Header("Bot Codex")]
    [SerializeField] private Button punchBotBtn;
    [SerializeField] private Button gunBotBtn;
    [SerializeField] private Button boomBotBtn;
    [SerializeField] private Button bigPunchBotBtn;
    [SerializeField] private Button smallBoomBotBtn;
    [SerializeField] private GameObject punchBotPage;
    [SerializeField] private GameObject gunBotPage;
    [SerializeField] private GameObject boomBotPage;
    [SerializeField] private GameObject bigPunchBotPage;
    [SerializeField] private GameObject smallBoomBotPage;
    private GameObject currentBotPage = null;

    [Header("Items Section")]
    [SerializeField] private Button QSRBtn;
    [SerializeField] private GameObject QSRPage;
    private GameObject currentItemPage = null;

    public void ReturnToMainMenu() {
        if (guidebookCanvas == null || mainMenuCanvas == null) {
            Debug.LogError("Tutorial or Main Menu canvas is not assigned.");
            return;
        }
        
        mainMenuCanvas.SetActive(true);
        guidebookCanvas.SetActive(false);
    }

    #region Bot Codex

    public void PunchBotButton() {
        if (punchBotPage == null) {
            Debug.LogError("Punch Bot Page is not assigned.");
            return;
        }
        if (currentBotPage == punchBotPage) return;
        
        if (currentBotPage != null) {
            currentBotPage.SetActive(false);
        }

        punchBotPage.SetActive(true);
        currentBotPage = punchBotPage;
    }

    public void GunBotButton() {
        if (gunBotPage == null) {
            Debug.LogError("Gun Bot Page is not assigned.");
            return;
        }
        if (currentBotPage == gunBotPage) return;

        if (currentBotPage != null) {
            currentBotPage.SetActive(false);
        }

        gunBotPage.SetActive(true);
        currentBotPage = gunBotPage;
    }

    public void BoomBotButton() {
        if (boomBotPage == null) {
            Debug.LogError("Boom Bot Page is not assigned.");
            return;
        }
        if (currentBotPage == boomBotPage) return;
        
        if (currentBotPage != null) {
            currentBotPage.SetActive(false);
        }

        boomBotPage.SetActive(true);
        currentBotPage = boomBotPage;
    }

    public void BigPunchBotButton() {
        if (bigPunchBotPage == null) {
            Debug.LogError("Big Punch Bot Page is not assigned.");
            return;
        }
        if (currentBotPage == bigPunchBotPage) return;
        if (currentBotPage != null) {
            currentBotPage.SetActive(false);
        }
        bigPunchBotPage.SetActive(true);
        currentBotPage = bigPunchBotPage;
    }

    public void SmallBoomBotButton() {
        if (smallBoomBotPage == null) {
            Debug.LogError("Small Boom Bot Page is not assigned.");
            return;
        }
        if (currentBotPage == smallBoomBotPage) return;

        if (currentBotPage != null) {
            currentBotPage.SetActive(false);
        }
        smallBoomBotPage.SetActive(true);
        currentBotPage = smallBoomBotPage;
    }
    #endregion

    #region Items Section

    public void QSRButton() {
        if (QSRPage == null) {
            Debug.LogError("QSR Page is not assigned.");
            return;
        }
        if (currentItemPage == QSRPage) return;

        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        QSRPage.SetActive(true);
        currentItemPage = QSRPage;
    }

    #endregion
}
