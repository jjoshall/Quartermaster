using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] private RawImage selectedItemImage;

    public void SetSelectedItemTexture(Texture texture)
    {
        if (selectedItemImage != null)
        {
            selectedItemImage.texture = texture;
        }
    }

    public Texture GetSelectedItemTexture()
    {
        if (selectedItemImage != null)
        {
            return selectedItemImage.texture;
        }
        return null;
    }
}
