using UnityEngine;

public class EnemyNeighborsTriggerVolume : MonoBehaviour {
    private void OnCollisionEnter(Collision collision) {
        if (collision.collider == null || collision.gameObject == null) return;
        

    }
}
