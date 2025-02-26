using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NodeDefense : NetworkBehaviour
{

    #region = Variables
    public NetworkVariable<bool> n_defenseCompleted = new NetworkVariable<bool>(); // completed when players successfully complete the defense.
    private NetworkVariable<bool> n_nodeDefenseActive = new NetworkVariable<bool>(); // active with players in range.

    [Tooltip("Duration in seconds to defend the node before it is cleared.")]
    public float nodeDefenseDuration = 60f; // time until node complete.
    private float _currentDefenseTimer = 0f;
    private List<GameObject> _playersInRange = new List<GameObject>();
    public Renderer this_renderer; // set in inspector
    // slider serializable
    [SerializeField, Range(0, 1)]
    private float _red;
    [SerializeField, Range(0, 1)]
    private float _green;
    [SerializeField, Range(0, 1)]
    private float _blue;

    // STRETCH GOAL: Additional node defense constraints.
    //               - Keep track of player. Each player has to contribute to the inRange condition.
    //               - 

    private float _particleTimer = 0f;
    private float _particleInterval = 2.0f;

    #endregion 

    #region = Setup

    void Start()
    {
        n_defenseCompleted.Value = false;
        _currentDefenseTimer = 0f;
        n_nodeDefenseActive.Value = false;
        _particleTimer = 0f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        n_defenseCompleted.Value = false;
        _currentDefenseTimer = 0f;
        n_nodeDefenseActive.Value = false;
        _particleTimer = 0f;
    }

    void Update(){
        UpdateParticle();
        if (n_defenseCompleted.Value){
            return;
        }
        UpdateDefenseTimer();
    }

    #endregion 

    #region = VFX

    private void UpdateParticle(){
        if (_particleTimer >= _particleInterval){
            SpawnParticle();
        } else {
            _particleTimer += Time.deltaTime;
        }
    }

    private void SpawnParticle(){
        if (n_defenseCompleted.Value){
            SpawnCompleteParticle();
        } else if (n_nodeDefenseActive.Value){
            SpawnActiveParticle();
        }
    }

    private void SpawnCompleteParticle(){
        _particleTimer = 0.0f;
        Vector3 lowPosition = this.transform.position;
        lowPosition.y = lowPosition.y - 0.2f;
        Vector3 highPosition = this.transform.position;
        highPosition.y = highPosition.y + 0.2f;
        ParticleManager.instance.SpawnSelfThenAll("RingEmission", lowPosition, Quaternion.Euler(90.0f, 0, 0));
        ParticleManager.instance.SpawnSelfThenAll("RingEmission", highPosition, Quaternion.Euler(90.0f, 0, 0));
        // ParticleManager.instance.SpawnSelfThenAll("RingEmission", this.transform.position, Quaternion.Euler(90.0f, 0, 0));

    }
    private void SpawnActiveParticle(){
        _particleTimer = 0.0f;
        ParticleManager.instance.SpawnSelfThenAll("RingEmission", this.transform.position, Quaternion.Euler(90.0f, 0, 0));
    }
    private void UpdateRendererColor(){
        this_renderer.material.color = new Color(_red * GetRatio(), _green * GetRatio(), _blue * GetRatio());
    }

    #endregion 

    #region = Logic

    private void UpdateDefenseTimer(){
        // increment if hasPlayersinRange, decrement if no players in range
        if (_currentDefenseTimer >= nodeDefenseDuration){
            n_defenseCompleted.Value = true;
            // _particleInterval = 1000f;
        }
        if (n_nodeDefenseActive.Value){
            _currentDefenseTimer += Time.deltaTime;
            UpdateRendererColor();
        } else {
            if (_currentDefenseTimer >= 0){
                _currentDefenseTimer -= Time.deltaTime;
                UpdateRendererColor();
            } else {
                _currentDefenseTimer = 0;
            }
        }
    }



    void OnTriggerEnter(Collider other){
        if(other.gameObject.tag == "Player"){
            _playersInRange.Add(other.gameObject);
            n_nodeDefenseActive.Value = true;
        }
    }

    void OnTriggerExit(Collider other){
        if(other.gameObject.tag == "Player"){
            _playersInRange.Remove(other.gameObject);
            if(!HasPlayersInRange()){
                n_nodeDefenseActive.Value = false;
                _currentDefenseTimer = 0f;
                _particleTimer = 0f;
            }
        }
    }

    #endregion

    #region = Helpers
    private float GetRatio(){
        return _currentDefenseTimer / nodeDefenseDuration;
    }

    bool HasPlayersInRange(){
        return _playersInRange.Count > 0;
    }

    #endregion

}
