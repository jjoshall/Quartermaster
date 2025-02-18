using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavScript : NetworkBehaviour {
     [HideInInspector] public EnemySpawner enemySpawner;
     private Transform _player;
     private NavMeshAgent _agent;

     [Tooltip("Path will update at a random float time between 0.05 - 0.1 seconds so that all enemies don't update at the same time causing performance issues")]
     [SerializeField] private float _attackDistance = 2;
     private float _pathUpdateDelay;

     public override void OnNetworkSpawn() {
          if (!IsServer) {
               enabled = false;
               GetComponent<NavMeshAgent>().enabled = false;
               return;
          }

          NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

          _agent = GetComponent<NavMeshAgent>();
          // animator = GetComponent<Animator>();

          _pathUpdateDelay = Random.Range(0.05f, 0.1f);
     }

     private void ClientDisconnected(ulong u) {
          _player = null;
     }

     private void Update() {
          //if (!NetworkManager.Singleton.IsServer) {
          //     return;
          //}

          foreach (GameObject obj in enemySpawner.playerList) {
               if (_player == null || Vector3.Distance(transform.position, obj.transform.position) 
                                   < Vector3.Distance(transform.position, _player.position)) {
                    _player = obj.transform;
               }
          }

          if (_player != null) {
               bool inRange = Vector3.Distance(transform.position, _player.position) <= _attackDistance;

               if (inRange) {
                    LookAtTarget();
               } else {
                    UpdatePath();
               }
          }
     }

     private void LookAtTarget() {
          Vector3 lookPos = _player.position - transform.position;
          lookPos.y = 0;
          Quaternion rotation = Quaternion.LookRotation(lookPos);
          transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.2f);
     }
     
     private void UpdatePath() {
          if (Time.time >= _pathUpdateDelay) {
               _pathUpdateDelay = Time.time + Random.Range(0.2f, 0.5f);
               _agent.SetDestination(_player.position);
          }
     }
}
