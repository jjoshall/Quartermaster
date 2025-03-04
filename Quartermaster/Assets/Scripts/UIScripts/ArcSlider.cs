using UnityEngine;
using UnityEngine.UI;

public class ArcSlider : MonoBehaviour {
    public Slider slider;
    public RectTransform sliderRect;

    void Update() {
        float arcOffset = Mathf.Sin(slider.value * -0.0175f) * 30f;
        sliderRect.anchoredPosition = new Vector2(sliderRect.anchoredPosition.x, 135 + arcOffset);
    }
}
