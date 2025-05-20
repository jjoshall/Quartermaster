using UnityEngine;

public class HealProjectile : IProjectile
{
    private float _healAmount = 0f;

    protected override void Start()
    {
        _expireTimer = 10f; // generic value to avoid immediate destruction.
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

    public override void InitializeData(float expireTimer, params object[] args)
    {
        base.InitializeData(expireTimer, args);
        if (args.Length < 1)
        {
            Debug.LogError("HealProjectile.InitializeData() - not enough args");
        }
        else
        {
            _healAmount = (float)args[0];

        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        //Debug.Log ("healprojectile collision: " + collision.gameObject.name);
    }

    protected override void OnTriggerEnter(Collider collision)
    {
        //Debug.Log("healprojectile triggered with: " + collision.gameObject.name);
        GameObject thisObj = collision.gameObject;
        if (collision.gameObject.CompareTag("PlayerHealCollider")){
            thisObj = collision.gameObject.transform.parent.gameObject;
        }
        if (thisObj.CompareTag("Player")){
            Debug.Log ("healprojectile collided with: " + thisObj.name);
            if (thisObj == sourcePlayer && _projectileCollided == false){
                //Debug.Log ("thisObj == sourcePlayer && _projectileCollided == false");
                return;
            }
            HealPlayer(thisObj);
        } else {
            _projectileCollided = true;
        }

    }

    private void HealPlayer(GameObject player){
        Health playerHp = player.GetComponent<Health>();
        // int healSpec = sourcePlayer.GetComponent<PlayerStatus>().GetHealSpecLvl();
        // float bonusPerSpec = GameManager.instance.HealSpec_MultiplierPer;
        // float total = bonusPerSpec * healSpec + 1.0f;

        // float totalHeal = GameManager.instance.MedKit_HealAmount * total;

        playerHp.HealServerRpc(_healAmount);
        ParticleManager.instance.SpawnSelfThenAll("Healing", player.transform.position, Quaternion.Euler(-90, 0, 0));
        
        // reenable physics collision if pooling instead of destroying
        // Physics.IgnoreCollision(projectileObj.GetComponent<Collider>(), user.GetComponent<Collider>(), false);
        Destroy(gameObject);
    }
    
}
