using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour {
    [Header("Inventory UI Elements")]
    [SerializeField] private RawImage[] inventorySlotImages; // Assign 4 RawImage objects in order (slot 1 at index 0, etc.)
    [SerializeField] private TextMeshProUGUI[] inventorySlotQuantityTexts; // Assign 4 TMP text components corresponding to each slot

    [SerializeField] public Image weaponCooldownRadial;

    [Header("Highlight Settings")]
    [SerializeField] private float highlightScale = 1.5f; // Scale multiplier for the highlighted slot

    // Optional: If you need weapon cooldown UI, add a field here.
    // [SerializeField] private Image weaponCooldownRadial;

    /// <summary>
    /// Sets the texture of a specified inventory slot.
    /// </summary>
    public void SetInventorySlotTexture(int slot, Texture texture) {
        if (inventorySlotImages != null && slot >= 0 && slot < inventorySlotImages.Length) {
            inventorySlotImages[slot].texture = texture;
        }
    }

    /// <summary>
    /// Sets the quantity text for a specified slot.
    /// Displays the quantity if the item is stackable (stackLimit > 1) and quantity is greater than 1;
    /// otherwise, it clears the text.
    /// </summary>
    public void SetInventorySlotQuantity(int slot, int quantity, int stackLimit) {
        if (inventorySlotQuantityTexts != null && slot >= 0 && slot < inventorySlotQuantityTexts.Length) {
            if (stackLimit > 1 && quantity > 1) {
                inventorySlotQuantityTexts[slot].text = quantity.ToString();
            } else {
                inventorySlotQuantityTexts[slot].text = "";
            }
        }
    }

    /// <summary>
    /// Highlights the specified slot by resetting the scale on all slots and enlarging the selected one.
    /// </summary>
    public void HighlightSlot(int selectedSlot) {
        if (inventorySlotImages == null) return;
        for (int i = 0; i < inventorySlotImages.Length; i++) {
            inventorySlotImages[i].rectTransform.localScale = Vector3.one;
        }
        if (selectedSlot >= 0 && selectedSlot < inventorySlotImages.Length) {
            inventorySlotImages[selectedSlot].rectTransform.localScale = Vector3.one * highlightScale;
        }
    }
}






