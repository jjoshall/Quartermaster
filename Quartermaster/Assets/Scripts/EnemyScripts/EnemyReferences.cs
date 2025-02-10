using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyReferences : MonoBehaviour
{
     [HideInInspector] public NavMeshAgent agent;
     // [HideInInspector] public Animator animator;

     [Header("Stats")]
     public float pathUpdateDelay = 0.2f;

     private void Awake()
     {
          agent = GetComponent<NavMeshAgent>();
          // animator = GetComponent<Animator>();
     }
}
