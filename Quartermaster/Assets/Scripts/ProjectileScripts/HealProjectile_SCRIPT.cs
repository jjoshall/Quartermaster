using UnityEngine;

public class HealProjectile : IProjectile
{
    protected override void Start()
    {
        _expireTimer = GameManager.instance.MedKit_ExpireTimer;
    }

    protected override void Update()
    {
        if (_projectileCollided){
            _expireTimer -= Time.deltaTime;
            if (_expireTimer <= 0){
                Destroy(gameObject);
            }
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        Debug.Log ("healprojectile collision: " + collision.gameObject.name);
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        Debug.Log("healprojectile triggered with: " + collision.gameObject.name);
        GameObject thisObj = collision.gameObject;
        if (collision.gameObject.CompareTag("PlayerHealCollider")){
            thisObj = collision.gameObject.transform.parent.gameObject;
        }
        if (thisObj.CompareTag("Player")){
            if (thisObj == sourcePlayer && _projectileCollided == false){
                Debug.Log ("thisObj == sourcePlayer && _projectileCollided == false");
                return;
            }
            HealPlayer(thisObj);
        } else {
            _projectileCollided = true;
        }

    }

    private void HealPlayer(GameObject player){
        Health playerHp = player.GetComponent<Health>();
        int healSpec = sourcePlayer.GetComponent<PlayerStatus>().GetHealSpecLvl();
        float bonusPerSpec = GameManager.instance.HealSpec_MultiplierPer;
        float total = bonusPerSpec * healSpec + 1.0f;

        float totalHeal = GameManager.instance.MedKit_HealAmount * total;

        playerHp.HealServerRpc(totalHeal);
        ParticleManager.instance.SpawnSelfThenAll("Healing", player.transform.position, Quaternion.Euler(-90, 0, 0));
        
        Destroy(gameObject);
    }
    
}
