using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerUIManager : MonoBehaviour
{
    [Header("inventoryUI")]
    [SerializeField] private RawImage[] inventorySlotImages;
    [SerializeField] private TextMeshProUGUI[] inventorySlotQuantityTexts;
    [SerializeField] private float highlightScale = 1.5f;
    [SerializeField] private Image weaponCooldownRadial;

    [Header("DMG indicator")]
    [SerializeField] private Transform damageImagePivot;
    [SerializeField] private CanvasGroup damageImageCanvas;
    [SerializeField] private float fadeStartTime = 1.5f;
    [SerializeField] private float fadeDuration = 1.5f;

    private Transform playerObj;
    private Vector3 damageLocation;
    private float currentFadeStartTime;
    private float currentFadeTime;
    private bool damageIndicatorActive = false;

    void Start()
    {
        StartCoroutine(FindLocalPlayer());
    }

    void Update()
    {
        UpdateDamageIndicator();
    }

    #region Inventory UI

    public void SetInventorySlotTexture(int slot, Texture texture)
    {
        if (inventorySlotImages != null && slot >= 0 && slot < inventorySlotImages.Length){
            inventorySlotImages[slot].texture = texture;
        }
    }

    public void SetInventorySlotQuantity(int slot, int quantity, int stackLimit)
    {
        if (inventorySlotQuantityTexts != null && slot >= 0 && slot < inventorySlotQuantityTexts.Length) {
            if (stackLimit > 1 && quantity > 1) {
                inventorySlotQuantityTexts[slot].text = quantity.ToString();
            }
            else {
                inventorySlotQuantityTexts[slot].text = "";
            }
        }
    }

    public void HighlightSlot(int selectedSlot)
    {
        if (inventorySlotImages == null) return;

        for (int i = 0; i < inventorySlotImages.Length; i++) {
            inventorySlotImages[i].rectTransform.localScale = Vector3.one;
        }

        if (selectedSlot >= 0 && selectedSlot < inventorySlotImages.Length){
            inventorySlotImages[selectedSlot].rectTransform.localScale = Vector3.one * highlightScale;
        }
    }

    public void SetWeaponCooldown(float fillAmount)
    {
        if (weaponCooldownRadial != null) {
            weaponCooldownRadial.fillAmount = fillAmount;
        }
    }

    #endregion

    #region Damage Indicator

    public void ShowDamageIndicator(Vector3 damagePos)
    {
        damageLocation = damagePos;
        currentFadeStartTime = fadeStartTime;
        currentFadeTime = fadeDuration;
        damageIndicatorActive = true;

        if (damageImageCanvas != null) {
            damageImageCanvas.alpha = 1f;
        }

        if (damageImagePivot != null) {
            damageImagePivot.localEulerAngles = Vector3.zero;
        }
    }

    private void UpdateDamageIndicator()
    {
        if (!damageIndicatorActive || playerObj == null) return;

        if (currentFadeStartTime > 0) {
            currentFadeStartTime -= Time.deltaTime;
            if (damageImageCanvas != null)
                damageImageCanvas.alpha = 1.0f;
        }
        else  {
            currentFadeTime -= Time.deltaTime;

            if (damageImageCanvas != null){
                float alphaValue = Mathf.Clamp01(currentFadeTime / fadeDuration);
                damageImageCanvas.alpha = alphaValue;
            }

            if (currentFadeTime <= 0) {
                damageIndicatorActive = false;
                damageImageCanvas.alpha = 0f;
            }
        }

        // Rotation logic
        damageLocation.y = playerObj.position.y;
        Vector3 directionToDamage = (damageLocation - playerObj.position).normalized;
        float angle = Vector3.SignedAngle(directionToDamage, playerObj.forward, Vector3.up);
        damageImagePivot.localEulerAngles = new Vector3(0, 0, angle);
    }

    private IEnumerator FindLocalPlayer()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);

        while (playerObj == null) {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject player in players) {
                NetworkObject netObj = player.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsLocalPlayer) {
                    playerObj = player.transform;
                    break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    #endregion
}