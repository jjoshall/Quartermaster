using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] private Image selectedItemImage;

    public void SetSelectedItemMaterial(Material mat)
    {
        if (selectedItemImage != null)
        {
            // Assign the material to the Image
            selectedItemImage.material = mat;
        }
    }

    public Material getSelectedItemMaterial()
    {
        if (selectedItemImage != null)
        {
            return selectedItemImage.material;
        }
        return null;
    }
}
