using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour {
    //private Coroutine _damageCoroutine;
    public int damage = 1;
    public int attackCooldown = 1;   // wait time between attacks

    public GameObject enemy;

    void Start() {
        //_damageCoroutine = null;
        if (!enemy) enemy = GetComponentInParent<GameObject>();
        InvokeRepeating("CheckForPlayersInRange", 0f, 1f);
    }



    /*private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            if (_damageCoroutine == null) {
                _damageCoroutine = StartCoroutine(DamageInterval(other));
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            if (_damageCoroutine != null) {
                StopCoroutine(_damageCoroutine);
                _damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageInterval(Collider player) {
        while (true) {
            yield return new WaitForSeconds(attackCooldown);
            player.GetComponent<Damageable>().InflictDamage(damage, false, enemy);
        }
    }*/

    void CheckForPlayersInRange()
    {
        // Perform the overlap sphere cast
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);

        // Loop through all the colliders that were detected within the sphere
        foreach (var hitCollider in hitColliders)
        {
            // Check if the collider has the "Player" tag
            if (hitCollider.CompareTag("Player"))
            {
                hitCollider.GetComponent<Damageable>().InflictDamage(damage, false, enemy);
            }
        }
    }
}
