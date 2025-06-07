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
    [SerializeField] private Button pistolBtn;
    [SerializeField] private GameObject pistolPage;
    [SerializeField] private Button railgunBtn;
    [SerializeField] private GameObject railgunPage;
    [SerializeField] private Button flamethrowerBtn;
    [SerializeField] private GameObject flamethrowerPage;
    [SerializeField] private Button medkitBtn;
    [SerializeField] private GameObject medkitPage;
    [SerializeField] private Button grenadeBtn;
    [SerializeField] private GameObject grenadePage;
    [SerializeField] private Button slowTrapBtn;
    [SerializeField] private GameObject slowTrapPage;
    [SerializeField] private Button stimBtn;
    [SerializeField] private GameObject stimPage;
    [SerializeField] private Button healSpecBtn;
    [SerializeField] private GameObject healSpecPage;
    [SerializeField] private Button damageSpecBtn;
    [SerializeField] private GameObject damageSpecPage;
    [SerializeField] private Button turretBtn;
    [SerializeField] private GameObject turretPage;
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

    public void PistolButton() {
        if (pistolPage == null) {
            Debug.LogError("Pistol Page is not assigned.");
            return;
        }
        if (currentItemPage == pistolPage) return;

        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        pistolPage.SetActive(true);
        currentItemPage = pistolPage;
    }

    public void RailgunButton() {
        if (railgunPage == null) {
            Debug.LogError("Railgun Page is not assigned.");
            return;
        }
        if (currentItemPage == railgunPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        railgunPage.SetActive(true);
        currentItemPage = railgunPage;
    }

    public void FlamethrowerButton() {
        if (flamethrowerPage == null) {
            Debug.LogError("Flamethrower Page is not assigned.");
            return;
        }
        if (currentItemPage == flamethrowerPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        flamethrowerPage.SetActive(true);
        currentItemPage = flamethrowerPage;
    }

    public void MedkitButton() {
        if (medkitPage == null) {
            Debug.LogError("Medkit Page is not assigned.");
            return;
        }
        if (currentItemPage == medkitPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        medkitPage.SetActive(true);
        currentItemPage = medkitPage;
    }

    public void GrenadeButton() {
        if (grenadePage == null) {
            Debug.LogError("Grenade Page is not assigned.");
            return;
        }
        if (currentItemPage == grenadePage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        grenadePage.SetActive(true);
        currentItemPage = grenadePage;
    }

    public void SlowTrapButton() {
        if (slowTrapPage == null) {
            Debug.LogError("Slow Trap Page is not assigned.");
            return;
        }
        if (currentItemPage == slowTrapPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        slowTrapPage.SetActive(true);
        currentItemPage = slowTrapPage;
    }

    public void StimButton() {
        if (stimPage == null) {
            Debug.LogError("Stim Page is not assigned.");
            return;
        }
        if (currentItemPage == stimPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        stimPage.SetActive(true);
        currentItemPage = stimPage;
    }

    public void HealSpecButton() {
        if (healSpecPage == null) {
            Debug.LogError("Heal Spec Page is not assigned.");
            return;
        }
        if (currentItemPage == healSpecPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        healSpecPage.SetActive(true);
        currentItemPage = healSpecPage;
    }

    public void DamageSpecButton() {
        if (damageSpecPage == null) {
            Debug.LogError("Damage Spec Page is not assigned.");
            return;
        }
        if (currentItemPage == damageSpecPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        damageSpecPage.SetActive(true);
        currentItemPage = damageSpecPage;
    }

    public void TurretButton() {
        if (turretPage == null) {
            Debug.LogError("Turret Page is not assigned.");
            return;
        }
        if (currentItemPage == turretPage) return;
        if (currentItemPage != null) {
            currentItemPage.SetActive(false);
        }
        turretPage.SetActive(true);
        currentItemPage = turretPage;
    }

    #endregion
}
