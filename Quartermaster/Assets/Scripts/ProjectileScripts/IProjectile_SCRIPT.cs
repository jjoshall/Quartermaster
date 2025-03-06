using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class IProjectile : MonoBehaviour
{
    public GameObject sourcePlayer { get; set; } = null;

    protected virtual float _expireTimer { get; set; } = 0f;

    protected bool _projectileCollided = false;

    protected virtual void Start(){
        _expireTimer = GameManager.instance.Grenade_ExpireTimer;
    }

    protected virtual void OnCollisionEnter(Collision collision){
        // if the grenade hits a player, it should explode.
        if (collision.gameObject.CompareTag("Enemy")){
            Expire();
        } else {
            _projectileCollided = true;
        }

    }

    protected virtual void Update(){
        
        if (_projectileCollided){
            _expireTimer -= Time.deltaTime;
            if (_expireTimer <= 0){
                Expire();
            }
        }
    }


    protected abstract void Expire();
}
