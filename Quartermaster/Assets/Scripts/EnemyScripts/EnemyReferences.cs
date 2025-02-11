using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyReferences : MonoBehaviour
{
     [HideInInspector] public NavMeshAgent agent;
     // [HideInInspector] public Animator animator;

     [Header("Stats")]
     // Path will update at a random float time between 0.2 - 0.5 seconds so that all enemies don't update at the same time causing performance issues
     public float pathUpdateDelay;

     private void Awake()
     {
          agent = GetComponent<NavMeshAgent>();
          // animator = GetComponent<Animator>();

          pathUpdateDelay = Random.Range(0.2f, 0.5f);
     }
}
