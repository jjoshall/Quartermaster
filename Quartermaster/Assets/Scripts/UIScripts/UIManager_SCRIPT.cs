using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour {
    public static UIManager instance;
    private void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    [SerializeField] public GameObject playerDrawCanvas;

    [Header("Inventory UI Elements")]
    [SerializeField] private RawImage[] inventorySlotImages;
    [SerializeField] private TextMeshProUGUI[] inventorySlotQuantityTexts;
    [SerializeField] private TextMeshProUGUI inventorySlotLabel;

    [SerializeField] public Image weaponCooldownRadial;

    [Header("Highlight Settings")]
    [SerializeField] private float highlightScale = 1.5f;
    public void SetInventorySlotTexture(int slot, Texture texture) {
        if (inventorySlotImages != null && slot >= 0 && slot < inventorySlotImages.Length) {
            inventorySlotImages[slot].texture = texture;
        }
    }
    public void SetInventorySlotQuantity(int slot, int quantity, int stackLimit) {
        if (inventorySlotQuantityTexts != null && slot >= 0 && slot < inventorySlotQuantityTexts.Length) {
            if (stackLimit > 1 && quantity > 1) {
                inventorySlotQuantityTexts[slot].text = quantity.ToString();
            } else {
                inventorySlotQuantityTexts[slot].text = "";
            }
        }
    }
    public void HighlightSlot(int selectedSlot) {
        if (inventorySlotImages == null) return;
        for (int i = 0; i < inventorySlotImages.Length; i++) {
            inventorySlotImages[i].rectTransform.localScale = Vector3.one;
        }
        if (selectedSlot >= 0 && selectedSlot < inventorySlotImages.Length) {
            inventorySlotImages[selectedSlot].rectTransform.localScale = Vector3.one * highlightScale;
        }
    }
    public void WriteLabel(Item item) {
        if (item) {
            inventorySlotLabel.text = item.ToString();
            int underscoreIndex = inventorySlotLabel.text.IndexOf('_');
            string cleaned = underscoreIndex >= 0 ? inventorySlotLabel.text.Substring(0, underscoreIndex) : inventorySlotLabel.text;
            inventorySlotLabel.text = cleaned;
        }
        else {
            inventorySlotLabel.text = "";
        }
    }
}






