using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class MeleeEnemyInherited_SCRIPT : BaseEnemyClass_SCRIPT {
    private bool _canAttack = true;
    protected override float attackCooldown => 2f;
    protected override float attackRange => 10f;
    protected override int damage => 15;
    protected override bool useGlobalTarget => false;

    private SoundEmitter[] soundEmitters;

    private Animator animator;

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        animator = GetComponentInChildren<Animator>();

        soundEmitters = GetComponents<SoundEmitter>();
    }

    //public override void InitializeFromGameManager() {
        
    //}

    protected override void Attack() {
        if (!_canAttack) return;
        _canAttack = false;

        if (IsServer) {
            Debug.Log("Melee enemy starting attack animation");
            animator.SetBool("IsAttacking", true);
            StartCoroutine(TriggerPunchSFX());

            //StartCoroutine(DebugAttackState());
            AttackServerRpc(false);
        }

        StartCoroutine(ResetAttackCooldown());
    }

    protected override void OnDamaged(float damage, GameObject damageSource)
    {
        base.OnDamaged(damage, damageSource);
        PlaySoundForEmitter("melee_damaged", transform.position);
    }

    private IEnumerator TriggerPunchSFX() {
        PlaySoundForEmitter("melee_punch", transform.position);
        yield return new WaitForSeconds(0.2f);
        PlaySoundForEmitter("melee_punch", transform.position);
    }

    private IEnumerator ResetAttackCooldown() {
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Melee enemy starting idle animation");
        animator.SetBool("IsAttacking", false);
        Debug.Log("Animator state after resetting: " + animator.GetCurrentAnimatorStateInfo(0).fullPathHash);
        yield return new WaitForSeconds(attackCooldown - 0.5f);
        _canAttack = true;
    }

    private IEnumerator DebugAttackState() {
        yield return null;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        Debug.Log("Current animator state: " + stateInfo.fullPathHash);
        if (stateInfo.IsName("Attack")) {
            Debug.Log("Attack animation is playing");
        } else {
            Debug.Log("Attack animation is NOT playing");
        }
    }

    public void PlaySoundForEmitter(string emitterId, Vector3 position) {
        foreach (SoundEmitter emitter in soundEmitters) {
            if (emitter.emitterID == emitterId) {
                emitter.PlayNetworkedSound(position);
                return;
            }
        }
    }
}
