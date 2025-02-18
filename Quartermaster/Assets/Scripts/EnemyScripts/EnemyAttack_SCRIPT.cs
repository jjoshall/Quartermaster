using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour {
    private Coroutine _damageCoroutine;
    public int damage = 10;
    public int attackCooldown = 1;   // wait time between attacks

    public Transform enemy;

    void Start() {
        _damageCoroutine = null;
    }

    private void OnTriggerEnter(Collider other) {
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
            player.GetComponent<PlayerHealth>().Damage(damage, enemy.position);
        }
    }
}
