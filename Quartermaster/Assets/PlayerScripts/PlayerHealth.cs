using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
     public int maxHealth = 100;
     public int currentHealth;

     public DamageIndicator myDamageIndicator;
     public Canvas myCanvas; // Reference to the Canvas

     void Start()
     {
          currentHealth = maxHealth;
     }

     public void Damage(int health, Vector3 damagePosition)
     {
          myDamageIndicator.damageLocation = damagePosition;
          GameObject go = Instantiate(myDamageIndicator.gameObject, myCanvas.transform); // Instantiate inside the Canvas
          go.transform.position = myDamageIndicator.transform.position;
          go.transform.rotation = myDamageIndicator.transform.rotation;
          go.SetActive(true);

          currentHealth -= health;
          if (currentHealth <= 0)
          {
               Debug.Log("died. resetting hp");
               currentHealth = maxHealth;
          }
     }
}
