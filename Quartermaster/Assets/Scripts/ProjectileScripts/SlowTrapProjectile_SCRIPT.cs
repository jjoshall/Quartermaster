using UnityEngine;

public class SlowTrapProjectile : IProjectile
{
    [SerializeField] private GameObject childSlowTrap; // trigger volume for slows

    protected override void Start() {
        _expireTimer = GameManager.instance.SlowTrap_Duration;
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        // check collision layer is whatIsGround
        if (collision.gameObject.layer == LayerMask.NameToLayer("whatIsGround")){
            ArmSlowTrap(collision.GetContact(0).point);
        }
    }

    protected override void Update()
    {
        if (_projectileCollided){
            _expireTimer -= Time.deltaTime;
            if (_expireTimer <= 0){
                DestroySlowTrap();
            }
        }
    }

    void ArmSlowTrap(Vector3 trapPosition){
        _projectileCollided = true;                                         // Begins timer

        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));        // Fix position
        float yOffset = this.transform.localScale.y / 2;
        transform.position = trapPosition + new Vector3(0, yOffset, 0);

        Rigidbody rb = this.GetComponent<Rigidbody>();                      // Freeze Position
        rb.constraints = RigidbodyConstraints.FreezeAll;
        
        Collider col = this.GetComponent<Collider>();                       // Remove Collider
        col.enabled = false;

        childSlowTrap.SetActive(true); 
        childSlowTrap.GetComponent<SlowTrapChildTrigger>().ActivateSlow();                                     // Activate slow trigger volume
        
    }

    void DestroySlowTrap(){
        // call childSlowTrap un-slow enemies in range.
        childSlowTrap.GetComponent<SlowTrapChildTrigger>().DeactivateSlow();
        Destroy(this.gameObject);
    }

}
