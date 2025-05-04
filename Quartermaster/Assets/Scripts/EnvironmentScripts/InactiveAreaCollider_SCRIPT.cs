using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InactiveAreaCollider : MonoBehaviour
{
    // THIS SCRIPT IS USED TO ADD OR REMOVE FROM ENEMYSPAWNER'S ACTIVEPLAYERLIST.

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player has entered the InactiveAreaCollider. Removing from EnemySpawner active player list");
            EnemySpawner.instance.activePlayerList.Remove(other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player has exited the InactiveAreaCollider. Adding to EnemySpawner active player list.");
            EnemySpawner.instance.activePlayerList.Add(other.gameObject);
        }
    }
}
