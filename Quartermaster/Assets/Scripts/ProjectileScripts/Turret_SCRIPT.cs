using System;
using System.Collections.Generic;
using UnityEngine;

public class Turret : IProjectile
{

    [Header("Turret Settings")]
    private float _turretAttackCooldown = 0.3f;
    private float _turretAttackRange = 10.0f;

    
    [Header("Runtime Vars")]
    public float batteryChargePercentage = 1f; // 0-1f

    public delegate void Animation();
    private List<Animation> _buildSequence = new List<Animation>();

    // use this function to inherit variables from projectilemanager spawn. 
    public override void InitializeData(params object[] args){

        var argLength = 0; // change this. 

        if (args.Length < argLength){
            Debug.LogError("GrenadeProjectile.InitializeData() - not enough args");
        } else {
            // _explosionDamage = (float)args[0];
            // _explosionRadius = (float)args[1];
        }
    }

    protected override void Start()
    {
        _expireTimer = Mathf.Infinity;
    }

    // Examples.
    public void ScaleBaseXZ(){
        // leantween animation scale.
    }

    public void ScaleBaseY(){
        // leantween animation scale y.
    }

    public void ScaleTrunkY(){

    }

    public void ScalePiece(){

    }


    private void TargetEnemy(){
        // sort target list by distance.
        // iterate through target list checking for line of sight.
        // target the closest enemy for which the turret has a direct line of sight.
        RotateX(0.0f);
        RotateY(0.0f);

    }

    private void RotateX(float animationDuration){
        // leantween a specific section of the turret.
        // set duration equal to time until next attack cooldown.
        // with minimum animationDuration time. 

    }

    private void RotateY(float animationDuration){
        // leantween animate a vertically rotating part.
    }


    private void Attack(){
        // verify target,
        // attack enemy. 
    }

    protected void OnTriggerEnter (Collision collision)
    {
        // add to target list.
    }
    
    protected override void OnCollisionEnter(Collision collision)
    {
        // if not already built. check for collision with ground.
        // build turret.
    }

    protected override void Update()
    {
        // if attack cooldown is up, attack target.

    }

    
    private void DoDamage (Damageable d, float dmg, bool isExplosiveDmgType, GameObject user){
        float damage = dmg;
        PlayerStatus s = user.GetComponent<PlayerStatus>();
        if (s != null){
            float bonusPerSpec = GameManager.instance.DmgSpec_MultiplierPer;
            int dmgSpecLvl = s.GetDmgSpecLvl();
            damage = damage * (1 + bonusPerSpec * dmgSpecLvl);
        }
        d?.InflictDamage(damage, isExplosiveDmgType, user);
    }

}
