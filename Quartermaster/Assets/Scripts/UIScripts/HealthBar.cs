using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour {
    [Header("UI References")]
    // This image should be set to the bright red fill image in your health bar
    public Image fillImage;
    public TextMeshProUGUI lives;
    public float livesRemaining = 10000; // its just a random number

    [SerializeField] private GameObject respawnCanvas;
    [SerializeField] private GameObject playerUICanvas;

    // [Header("Player Health Reference")]
    // // Reference to the player's Health component; assign this via code or the inspector
    // public Health playerHealth;

    public static HealthBarUI instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateHealthBar(Health playerHealth) {
        if (playerHealth && fillImage) {
            float ratio = playerHealth.CurrentHealth.Value / playerHealth.MaxHealth;
            fillImage.fillAmount = ratio;
        }
    }

    public void UpdateLives(float livesCount) {
        if (lives) {
            lives.text = "Lives: " + livesCount.ToString();
        }
        livesRemaining = livesCount;
    }

    public void ToggleRespawnCanvas(bool state) {
        respawnCanvas.gameObject.SetActive(state);
        playerUICanvas.gameObject.SetActive(!state);
        PauseMenuToggler.IsPaused = state;
    }
    
    void Start() {
        // // If playerHealth isn't already assigned, search for the local player's Health
        // if (playerHealth == null) {
        //     // Get all objects tagged "Player"
        //     GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        //     foreach (GameObject playerObj in players) {
        //         Health h = playerObj.GetComponent<Health>();
        //         // Check if this is the local player's Health
        //         if (h != null && h.IsLocalPlayer) {
        //             playerHealth = h;
        //             Debug.Log("Found local player health on: " + playerObj.name);
        //             break;
        //         }
        //     }
        // }
        
        // if (playerHealth != null) {
        //     playerHealth.CurrentHealth.OnValueChanged += OnHealthChanged;
        //     UpdateHealthBar();
        // } else {
        //     Debug.LogError("Local player's Health component not found!");
        // }
    }

    // void OnDestroy() {
    //     if (playerHealth != null) {
    //         playerHealth.CurrentHealth.OnValueChanged -= OnHealthChanged;
    //     }
    // }

    // // This callback is triggered whenever CurrentHealth changes
    // private void OnHealthChanged(float previousValue, float newValue) {
    //     UpdateHealthBar();
    // }


    // // Optionally, if you want to subscribe to events instead:
    // private void OnEnable() {
    //     if (playerHealth != null) {
    //         playerHealth.OnDamaged += HandleDamage;
    //         playerHealth.OnHealed += HandleHeal;
    //     }
    // }

    // private void OnDisable() {
    //     if (playerHealth != null) {
    //         playerHealth.OnDamaged -= HandleDamage;
    //         playerHealth.OnHealed -= HandleHeal;
    //     }
    // }

    // private void HandleDamage(float damageAmount, GameObject source) {
    //     UpdateHealthBar();
    // }

    // private void HandleHeal(float healAmount) {
    //     UpdateHealthBar();
    // }

}
