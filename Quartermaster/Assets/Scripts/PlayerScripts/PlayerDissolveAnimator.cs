using UnityEngine;
using Unity.Netcode;

public class PlayerDissolveAnimator : NetworkBehaviour
{
    private const float _DISSOLVE_MIN_RANGE = 0.0f;
    private const float _DISSOLVE_MAX_RANGE = 1.0f;

    public Renderer playerRenderer;
    private Material[] _playerMaterials;
    public float dissolveDuration = 1.0f;
    public float solidifyDuration = 1.0f;
    private float _dissolveTimerStart = 0.0f;
    private float _solidifyTimerStart = 0.0f;
    private bool _dissolveStarted = false;
    private bool _solidifyStarted = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // on network spawn
    public override void OnNetworkSpawn()
    {
        if (playerRenderer){
            Material[] newMaterials = new Material[playerRenderer.materials.Length];
            for (int i = 0; i < playerRenderer.materials.Length; i++){
                newMaterials[i] = new Material(playerRenderer.materials[i]);
            }
            _playerMaterials = newMaterials;
            playerRenderer.materials = newMaterials;
        } else {
            Debug.LogError("PlayerRenderer is not set in the PlayerDissolveAnimator script.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_dissolveStarted){
            if (Time.time - _dissolveTimerStart < dissolveDuration){
                LerpDissolveAmount();
            } else {
                _dissolveStarted = false;
            }
        }
        if (_solidifyStarted){
            if (Time.time - _solidifyTimerStart < solidifyDuration){
                LerpSolidifyAmount();
            } else {
                _solidifyStarted = false;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AnimateDissolveServerRpc(){
        AnimateDissolveClientRpc();

    }
    [ClientRpc]
    public void AnimateDissolveClientRpc(){
        AnimateDissolve();
    }

    [ServerRpc(RequireOwnership = false)]
    public void AnimateSolidifyServerRpc(){
        AnimateSolidifyClientRpc();
    }

    [ClientRpc]
    public void AnimateSolidifyClientRpc(){
        AnimateSolidify();
    }

    public void AnimateDissolve(){
        for (int i = 0; i < _playerMaterials.Length; i++){
            _playerMaterials[i].SetFloat("_DissolveStrength", 0.0f);
        }
        _dissolveTimerStart = Time.time;
        _dissolveStarted = true;
    }

    void LerpDissolveAmount(){
        float dissolveAmount;
        // lerp dissolve amount based on time.time, dissolveTimerStart and dissolveDuration
        // set the lerp value to the dissolveAmount
        // set the dissolveAmount to the playerMaterial's "_DissolveAmount" property
        dissolveAmount = Mathf.Lerp(_DISSOLVE_MIN_RANGE, _DISSOLVE_MAX_RANGE, (Time.time - _dissolveTimerStart) / dissolveDuration);
        for (int i = 0; i < _playerMaterials.Length; i++){
            _playerMaterials[i].SetFloat("_DissolveStrength", dissolveAmount);
        }
    }

    public void AnimateSolidify(){
        Debug.Log ("animating solidify function. setting dissolve strength to 1.");
        for (int i = 0; i < _playerMaterials.Length; i++){
            _playerMaterials[i].SetFloat("_DissolveStrength", 1.0f);
        }
        _solidifyTimerStart = Time.time;
        _solidifyStarted = true;

    }

    void LerpSolidifyAmount(){
        float dissolveAmount;
        // lerp dissolve amount based on time.time, dissolveTimerStart and dissolveDuration
        // set the lerp value to the dissolveAmount
        // set the dissolveAmount to the playerMaterial's "_DissolveAmount" property
        dissolveAmount = Mathf.Lerp(_DISSOLVE_MAX_RANGE, _DISSOLVE_MIN_RANGE, (Time.time - _solidifyTimerStart) / solidifyDuration);
        for (int i = 0; i < _playerMaterials.Length; i++){
            _playerMaterials[i].SetFloat("_DissolveStrength", dissolveAmount);
        }
    }
}
