using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class SimpleMoveScript : NetworkBehaviour
{
    // Make the navmesh agent path toward a point
    public Vector3 target;
    [SerializeField] private NavMeshAgent agent;

    // Update is called once per frame
    void Update() {
        agent.SetDestination(target);
    }
}
