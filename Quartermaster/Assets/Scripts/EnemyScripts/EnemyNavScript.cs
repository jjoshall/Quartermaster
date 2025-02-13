using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavScript : NetworkBehaviour
{
     public EnemySpawner enemySpawner;
     private Transform player;
     private NavMeshAgent agent;

     [SerializeField] private float attackDistance = 2;
     [Tooltip("Path will update at a random float time between 0.05 - 0.1 seconds so that all enemies don't update at the same time causing performance issues")]
     private float pathUpdateDelay;

     public override void OnNetworkSpawn()
     {
          if (!IsServer)
          {
               enabled = false;
               GetComponent<NavMeshAgent>().enabled = false;
               return;
          }
          NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

          agent = GetComponent<NavMeshAgent>();
          // animator = GetComponent<Animator>();

          pathUpdateDelay = Random.Range(0.05f, 0.1f);
     }

     private void ClientDisconnected(ulong u)
     {
          player = null;
     }

     private void Update()
     {
          //if (!NetworkManager.Singleton.IsServer)
          //{
          //     return;
          //}

          foreach (GameObject obj in enemySpawner.playerList)
          {
               if (player == null || Vector3.Distance(transform.position, obj.transform.position) < Vector3.Distance(transform.position, player.position))
               {
                    player = obj.transform;
               }
          }

          if (player != null)
          {
               bool inRange = Vector3.Distance(transform.position, player.position) <= attackDistance;

               if (inRange)
               {
                    LookAtTarget();
               }
               else
               {
                    UpdatePath();
               }
          }
     }

     private void LookAtTarget()
     {
          Vector3 lookPos = player.position - transform.position;
          lookPos.y = 0;
          Quaternion rotation = Quaternion.LookRotation(lookPos);
          transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.2f);
     }
     
     private void UpdatePath()
     {
          if (Time.time >= pathUpdateDelay)
          {
               pathUpdateDelay = Time.time + Random.Range(0.2f, 0.5f);
               agent.SetDestination(player.position);
          }
     }
}
