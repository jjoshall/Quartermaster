using UnityEngine;

public class PlayerHealth : MonoBehaviour {
     public int maxHealth = 100;
     public int currentHealth;

     public FullScreenTestController myFullScreenTestController;
     public DamageIndicator myDamageIndicator;
     public Canvas myCanvas; // Reference to the Canvas

     void Start() {
          currentHealth = maxHealth;
     }

     public void Damage(int health, Vector3 damagePosition) {
          myDamageIndicator.damageLocation = damagePosition;
          
          // Instantiate inside the Canvas
          GameObject go = Instantiate(myDamageIndicator.gameObject, myCanvas.transform);
          go.transform.position = myDamageIndicator.transform.position;
          go.transform.rotation = myDamageIndicator.transform.rotation;
          go.SetActive(true);

          myFullScreenTestController.StartCoroutine(myFullScreenTestController.Hurt());

          currentHealth -= health;
          if (currentHealth <= 0) {
               Debug.Log("died. resetting hp");
               currentHealth = maxHealth;
          }
     }

     public void Heal(int health) {
          currentHealth += health;
          if (currentHealth >= maxHealth) {
               currentHealth = maxHealth;
          }
     }
}
