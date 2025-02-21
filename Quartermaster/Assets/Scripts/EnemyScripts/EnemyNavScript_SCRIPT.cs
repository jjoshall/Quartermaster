using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavScript : NetworkBehaviour {
    [HideInInspector] public EnemySpawner enemySpawner;
    private Transform _targetPlayer;
    private NavMeshAgent _agent;

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
        _targetPlayer = null;
    }

    private void Update() {
        if (!IsServer) return;

        FindClosestPlayer();

        if (_targetPlayer != null) {
            bool inRange = Vector3.Distance(transform.position, _targetPlayer.position) <= _attackDistance;

            if (inRange) {
                LookAtTarget();
            } else {
                UpdatePath();
            }
        }
    }

    private void FindClosestPlayer()
    {
        float closestDistance = float.MaxValue;
        Transform closestPlayer = null;

        foreach (var playerRef in enemySpawner.playerList)
        {
            if (playerRef.TryGet(out NetworkObject playerObj))
            {
                float distance = Vector3.Distance(transform.position, playerObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = playerObj.transform;
                }
            }
        }

        _targetPlayer = closestPlayer;
    }

    private void LookAtTarget() {
        Vector3 lookPos = _targetPlayer.position - transform.position;
        lookPos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.2f);
    }
     
    private void UpdatePath() {
        if (Time.time >= _pathUpdateDelay) {
            _pathUpdateDelay = Time.time + Random.Range(0.2f, 0.5f);
            _agent.SetDestination(_targetPlayer.position);

            // Show a debug line on what position the enemy is moving to
            Debug.DrawLine(transform.position, _agent.destination, Color.red);
        }
    }
}
