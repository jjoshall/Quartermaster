using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HPBarScript : NetworkBehaviour {
    public Health health;
    [SerializeField] private Slider _slider;

    void Update() {
        _slider.value = health.GetRatio();
    }
}
