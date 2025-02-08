using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 10;
    public int attackCooldown = 1;   // wait time between attacks
    private Coroutine damageCoroutine;
    private Transform enemy;

    void Start(){
        damageCoroutine = null;
        // enemy is parent of the attack object
        enemy = transform.parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (damageCoroutine == null)
            {
                damageCoroutine = StartCoroutine(DamageInterval(other));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageInterval(Collider player)
    {
        while (true)
        {
            yield return new WaitForSeconds(attackCooldown);
            if (enemy == null)
            {
                Debug.Log ("enemy reference is null for some reason");
            }
            player.GetComponent<PlayerHealth>().Damage(damage, enemy.position);
          }
    }
}
