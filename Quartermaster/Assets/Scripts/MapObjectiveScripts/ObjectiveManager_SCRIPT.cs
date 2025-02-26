using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ObjectiveManager : NetworkBehaviour {
    #region = Setup
    // Singleton
    public static ObjectiveManager instance;
    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);

        }
    }

    // Network list managed during runtime.
    private NetworkList<NetworkObjectReference> n_IObjectivesList = new NetworkList<NetworkObjectReference>();
    
    // objectivesList. Populate in inspector.
    public List<GameObject> objectivesList = new List<GameObject>(); 

    #endregion

    void Start(){
        // Populate the dynamic remainingObjectives list with the netobjrefs of the objectivesList.

    }

    public override void OnNetworkSpawn(){
        foreach (GameObject objective in objectivesList){
            NetworkObjectReference thisNetRef = new NetworkObjectReference(objective.GetComponent<NetworkObject>());
            n_IObjectivesList.Add(thisNetRef);
        }

    }
    // ==============================================================================================
    #region = Objectives
    public void CheckAllObjectives(){
        foreach (NetworkObjectReference netObjRef in n_IObjectivesList){
            if (!netObjRef.TryGet(out NetworkObject netObj)){
                Debug.Log("Could not get NetworkObject from reference.");
            }
            if (netObj == null){
                Debug.Log("NetworkObject is null.");
            }
            if (!netObj.GetComponent<IObjective>().IsComplete()){
                return;
            }
        }
        ClearedAllObjectives();
    }

    #endregion 

    // ==============================================================================================
    #region = NextPhase
    private void ClearedAllObjectives(){
        // Do something here. Boss phase.
        DebugAllClientRpc("ObjectiveManager: ClearedAllObjectives() placeholder clientRPC msg.");
    }

    [ServerRpc]
    private void DebugServerRpc(string msg){
        Debug.Log(msg);

    }
    [ClientRpc]
    private void DebugAllClientRpc(string msg){
        Debug.Log(msg);
    }

    #endregion
}