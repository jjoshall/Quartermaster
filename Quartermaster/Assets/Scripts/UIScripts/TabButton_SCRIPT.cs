using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CustomTabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [SerializeField] public TabGroup tabGroup;
    [SerializeField] public Image background;

    public void OnPointerClick(PointerEventData eventData) {
        tabGroup.OnTabSelected(this);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        tabGroup.OnTabEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData) {
        tabGroup.OnTabExit(this);
    }

    void Start() {
        background = GetComponent<Image>();
        tabGroup.Subscribe(this);
    }
}
