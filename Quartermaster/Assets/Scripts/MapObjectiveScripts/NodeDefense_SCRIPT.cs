using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NodeDefense : NetworkBehaviour
{

    public bool defenseCompleted = false; // completed when players successfully complete the defense.
    private bool _nodeDefenseActive = false; // active with players in range.

    [Tooltip("Duration in seconds to defend the node before it is cleared.")]
    public float nodeDefenseDuration = 60f; // default duration to defend before completed. set in inspector

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


    void Update(){
        if (defenseCompleted){
            return;
        }
        UpdateDefenseTimer();
        UpdateParticle();
    }

    private void UpdateParticle(){
        if (_particleTimer >= _particleInterval){
            SpawnParticle();
        } else {
            _particleTimer += Time.deltaTime;
        }
    }

    private void SpawnParticle(){
        if (defenseCompleted){
            SpawnCompleteParticle();
        } else if (_nodeDefenseActive){
            SpawnActiveParticle();
        }
    }

    private void SpawnCompleteParticle(){
        _particleTimer = 0.0f;
        Vector3 lowPosition = new Vector3(this.transform.position.x, this.transform.position.y - 0.2f, this.transform.position.z);
        Vector3 highPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.2f, this.transform.position.z);
        ParticleManager.instance.SpawnSelfThenAll("RingEmission", lowPosition, Quaternion.Euler(90.0f, 0, 0));
        ParticleManager.instance.SpawnSelfThenAll("RingEmission", highPosition, Quaternion.Euler(90.0f, 0, 0));


    }
    private void SpawnActiveParticle(){
        _particleTimer = 0.0f;
        ParticleManager.instance.SpawnSelfThenAll("RingEmission", this.transform.position, Quaternion.Euler(90.0f, 0, 0));
    }

    private void UpdateDefenseTimer(){
        // increment if hasPlayersinRange, decrement if no players in range
        if (_currentDefenseTimer >= nodeDefenseDuration){
            defenseCompleted = true;
            // _particleInterval = 1000f;
        }
        if (_nodeDefenseActive){
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

    private void UpdateRendererColor(){
        this_renderer.material.color = new Color(_red * GetRatio(), _green * GetRatio(), _blue * GetRatio());
        // float ratio = GetRatio(); // 0 to 1
        // Color baseColor = new Color(_red * ratio, _green * ratio, _blue * ratio); // Adjust RGB brightness

        // // Ensure the material supports emission
        // this_renderer.material.EnableKeyword("_EMISSION");

        // // Set emission color with intensity (multiply color for glow effect)
        // this_renderer.material.SetColor("_EmissionColor", baseColor * Mathf.Lerp(0.2f, 5.0f, ratio));

    }

    private float GetRatio(){
        return _currentDefenseTimer / nodeDefenseDuration;
    }

    bool HasPlayersInRange(){
        return _playersInRange.Count > 0;
    }

    void OnTriggerEnter(Collider other){
        if(other.gameObject.tag == "Player"){
            _playersInRange.Add(other.gameObject);
            _nodeDefenseActive = true;
        }
    }

    void OnTriggerExit(Collider other){
        if(other.gameObject.tag == "Player"){
            _playersInRange.Remove(other.gameObject);
            if(!HasPlayersInRange()){
                _nodeDefenseActive = false;
                _currentDefenseTimer = 0f;
                _particleTimer = 0f;
            }
        }
    }

}
