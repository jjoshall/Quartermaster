using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    public int damage = 10;
    public int attackCooldown = 1;   // wait time between attacks
    private Coroutine damageCoroutine;

    void Start(){
        damageCoroutine = null;
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
            player.GetComponent<PlayerHealth>().Damage(damage);
        }
    }
}
