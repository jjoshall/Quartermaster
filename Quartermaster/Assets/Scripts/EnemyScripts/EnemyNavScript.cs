using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavScript : NetworkBehaviour
{
     public Transform target;
     public GameObject enemyPrefab;
     private EnemyReferences enemyReferences;
     private float pathUpdateDeadline;
     private float attackDistance;

     private void Awake()
     {
          enemyReferences = GetComponent<EnemyReferences>();
     }

     private void Start()
     {
          attackDistance = enemyReferences.agent.stoppingDistance;
     }

     private void Update()
     {
          if (!NetworkManager.Singleton.IsServer)
          {
               return;
          }

          if (target != null)
          {
               bool inRange = Vector3.Distance(transform.position, target.position) <= attackDistance;

               if (inRange)
               {
                    LookAtTarget();
               }
               else
               {
                    UpdatePath();
               }
          }

          if (Input.GetKeyDown(KeyCode.N))
          {
               NetworkObjectPool.Singleton.ReturnNetworkObject(NetworkObject, enemyPrefab);
               //NetworkObject.Despawn();
          }
     }

     private void LookAtTarget()
     {
          Vector3 lookPos = target.position - transform.position;
          lookPos.y = 0;
          Quaternion rotation = Quaternion.LookRotation(lookPos);
          transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.2f);
     }
     
     private void UpdatePath()
     {
          if (Time.time >= pathUpdateDeadline)
          {
               pathUpdateDeadline = Time.time + enemyReferences.pathUpdateDelay;
               enemyReferences.agent.SetDestination(target.position);
          }
     }
}
