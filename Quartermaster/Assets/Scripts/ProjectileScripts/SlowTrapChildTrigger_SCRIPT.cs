using UnityEngine;

public class SlowTrapChildTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other){
        if (other.CompareTag("Enemy")){
            BaseEnemyClass_SCRIPT baseEnemy = other.GetComponent<BaseEnemyClass_SCRIPT>();
            if (baseEnemy == null){
                Debug.LogError ("slowtrap: enemy tag without BaseEnemyClass_SCRIPT component: " + other.name);
                return;
            }
            baseEnemy.ApplySlowDebuffServerRpc();
        }
    }

    void OnTriggerExit(Collider other){
        if (other.CompareTag("Enemy")){
            BaseEnemyClass_SCRIPT baseEnemy = other.GetComponent<BaseEnemyClass_SCRIPT>();
            if (baseEnemy == null){
                Debug.LogError ("slowtrap: enemy tag without BaseEnemyClass_SCRIPT component: " + other.name);
                return;
            }
            baseEnemy.RemoveSlowDebuffServerRpc();
        }
    }

    public void ActivateSlow (){
        float slowAoE = GameManager.instance.SlowTrap_AoERadius;
        
        SphereCollider slowCollider = GetComponent<SphereCollider>();
        slowCollider.radius = slowAoE;

        LayerMask enemiesMask = LayerMask.GetMask("Enemy");
        Collider[] hits = Physics.OverlapSphere (transform.position, slowAoE, enemiesMask);
        foreach (Collider hit in hits){
            if (hit.CompareTag("Enemy")){
                BaseEnemyClass_SCRIPT baseEnemy = hit.GetComponent<BaseEnemyClass_SCRIPT>();
                if (baseEnemy == null){
                    Debug.LogError ("slowtrap: enemy tag without BaseEnemyClass_SCRIPT component: " + hit.name);
                    return;
                }
                baseEnemy.ApplySlowDebuffServerRpc();
            }
        }
    }

    public void DeactivateSlow(){
        float slowAoE = GameManager.instance.SlowTrap_AoERadius;
        LayerMask enemiesMask = LayerMask.GetMask("Enemy");
        Collider[] hits = Physics.OverlapSphere (transform.position, slowAoE, enemiesMask);
        foreach (Collider hit in hits){
            if (hit.CompareTag("Enemy")){
                BaseEnemyClass_SCRIPT baseEnemy = hit.GetComponent<BaseEnemyClass_SCRIPT>();
                if (baseEnemy == null){
                    Debug.LogError ("slowtrap: enemy tag without BaseEnemyClass_SCRIPT component: " + hit.name);
                    return;
                }
                baseEnemy.RemoveSlowDebuffServerRpc();
            }
        }

    }
}
