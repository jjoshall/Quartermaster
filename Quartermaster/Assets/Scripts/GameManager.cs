using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    #region InspectorSettings
    // Do not update these values during run-time. Set in inspector.
    [Header("Player Settings")]
    // HP and player speed set in the Player prefab settings.

    [Header("Weapon Settings")]
    // Weapons should not scale at run-time. Scale enemy hp instead.
    [SerializeField] private float flame_Damage;
    [SerializeField] private float flame_Range;
    [SerializeField] private float flame_EndRadius; // The radius of the circle at the end of the flamethrower capsulecast
    [SerializeField] private float flame_Cooldown;
    [SerializeField] private string flame_EnemyHitEffect = "Sample"; // effect spawned on center of every enemy hit.
    [SerializeField] private string flame_BarrelEffect = "PistolBarrelFire"; // effect at player

    [SerializeField] private float railgun_Damage;
    [SerializeField] private float railgun_AoeRadius;
    [SerializeField] private float railgun_Cooldown;

    [SerializeField] private float pistol_Damage;
    [SerializeField] private float pistol_Cooldown;

    [Header("Item Settings")]
    [SerializeField] private float portalKey_Cooldown;

    [SerializeField] private float grenade_Damage;
    [SerializeField] private float grenade_AoeRadius;
    [SerializeField] private float grenade_ChargeTime;
    [SerializeField] private float grenade_MinVelocity;
    [SerializeField] private float grenade_MaxVelocity;
    [SerializeField] private float grenade_Cooldown;
    [SerializeField] private int grenade_StackLimit;

    [SerializeField] private float slowTrap_Multiplier;// give enemy a debuff flag. enemy checks for debuff then accesses this value to adjust their own speed.
    [SerializeField] private float slowTrap_Duration;   // duration of the trap itself. 
    [SerializeField] private float slowTrap_Cooldown;
    [SerializeField] private int slowTrap_StackLimit;

    [SerializeField] private float medKit_HealAmount ;
    [SerializeField] private float medKit_Cooldown ;
    [SerializeField] private int medKit_StackLimit;

    [SerializeField] private int questItem_StackLimit;

    [SerializeField] private float[] _playersToEnemyHpMultiplier = new float[10] {  0.1f, 
                                                                                    1.0f, 
                                                                                    1.5f, 
                                                                                    2.0f, 
                                                                                    2.25f, 
                                                                                    2.5f, 
                                                                                    2.675f, 
                                                                                    2.75f, 
                                                                                    2.875f, 
                                                                                    3.0f }; // Multiplier for enemy hp based on player count

    [Header("BurstDropRate")]
    // WIP. MOVE BURST DROP RATE LOGIC HERE FROM ITEMMANAGER.
    public float burstDropRate; // chance of burst weapon dropping from enemy

    [Header("DramaFunction")]
    [SerializeField] private float _dramaOverTimeCoefficient = 0.1f;
    #endregion

    #region ReadonlyAccess
    public float Flame_Damage => flame_Damage;
    public float Flame_Range => flame_Range;
    public float Flame_EndRadius => flame_EndRadius;
    public float Flame_Cooldown => flame_Cooldown;
    public string Flame_EnemyHitEffect => flame_EnemyHitEffect;
    public string Flame_BarrelEffect => flame_BarrelEffect;

    public float Railgun_Damage => railgun_Damage;
    public float Railgun_AoeRadius => railgun_AoeRadius;
    public float Railgun_Cooldown => railgun_Cooldown;

    public float Pistol_Damage => pistol_Damage;
    public float Pistol_Cooldown => pistol_Cooldown;

    public float PortalKey_Cooldown => portalKey_Cooldown;

    public float Grenade_Damage => grenade_Damage;
    public float Grenade_AoeRadius => grenade_AoeRadius;
    public float Grenade_ChargeTime => grenade_ChargeTime;
    public float Grenade_MinVelocity => grenade_MinVelocity;
    public float Grenade_MaxVelocity => grenade_MaxVelocity;
    public float Grenade_Cooldown => grenade_Cooldown;
    public int Grenade_StackLimit => grenade_StackLimit;

    public float SlowTrap_Multiplier => slowTrap_Multiplier;
    public float SlowTrap_Duration => slowTrap_Duration;
    public float SlowTrap_Cooldown => slowTrap_Cooldown;

    public float MedKit_HealAmount => medKit_HealAmount;
    public float MedKit_Cooldown => medKit_Cooldown;

    public int QuestItem_StackLimit => questItem_StackLimit;

    #endregion







    #region RuntimeVariables

    [Header("Enemy Settings")]
    // Enemy hp should scale at run-time with difficulty and playercount. Base values set in the enemy Prefabs.
    public float _enemyHpMultiplier { get; private set; }     // multiplier for all enemy's hp values when spawned.
    public float enemySpeedMultiplier { get; private set; }  // enemy classes will need to have their own logic to propagate this value to all the navmeshagent parameters for speed.
    // individual enemy settings set in their prefabs.






    [Header("DramaFunction")]
    // placeholder variables. nothing set in stone, just brainstorming
    private float _drama = 0.0f;

    private float DramaCurveEnemyHpMultiplier (float drama){
        // Return some function based on time(?)/enemieskilled(?)/drama(?) to increase enemyHp.
        return 0.0f; // placeholder
    }
    private float DramaCurveBurstDropRate (float drama){
        return 0.0f;
    }




    #endregion

    #region GameManagerSetup
    public NetworkList<NetworkObjectReference> n_players { get; private set;}
    public NetworkList<NetworkObjectReference> n_enemies { get; private set;}
    public NetworkList<NetworkObjectReference> n_worldItems { get; private set;}

    // singleton code
    public static GameManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
        InitializeRuntimeVars();
    }

    private void InitializeRuntimeVars(){
        n_players = new NetworkList<NetworkObjectReference>();
        n_enemies = new NetworkList<NetworkObjectReference>();
        n_worldItems = new NetworkList<NetworkObjectReference>();

    }

    #endregion
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
