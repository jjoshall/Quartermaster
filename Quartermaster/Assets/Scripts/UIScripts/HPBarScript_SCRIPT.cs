using UnityEngine;
using UnityEngine.UI;

public class HPBarScript : MonoBehaviour
{
    public PlayerHealth health;
    [SerializeField] private Slider slider;

    void Update()
    {
        slider.value = (float)health.currentHealth / health.maxHealth;
    }
}
