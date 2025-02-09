using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void Damage (int health){
        currentHealth -= health;
        if (currentHealth <= 0){
            Debug.Log("died. resetting hp");
            currentHealth = maxHealth;
        }
    }
}
