using UnityEngine;
using UnityEngine.UI;

public class HPBarScript : MonoBehaviour {
    public PlayerHealth health;
    [SerializeField] private Slider _slider;

    void Update() {
        _slider.value = (float)health.currentHealth / health.maxHealth;
    }
}
