using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

namespace temp {
public class EnemyNavScript : MonoBehaviour
{
     private Transform player;
     private NavMeshAgent agent;

     // Start is called once before the first execution of Update after the MonoBehaviour is created
     void Start()
     {
          agent = GetComponent<NavMeshAgent>();
          FindLocalPlayer();
     }

     // Update is called once per frame
     void Update()
     {
          if (player != null) {
               agent.destination = player.position;
          }
     }


     private void FindLocalPlayer() {
          foreach (var obj in FindObjectsOfType<NetworkObject>()) {
               if (obj.IsLocalPlayer) {
                    player = obj.transform;
                    break;
               }
          }
     }
}
}
