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
    private NetworkList<ulong> n_IObjectivesList = new NetworkList<ulong>();
    
    // objectivesList. Populate in inspector.
    public List<GameObject> objectivesList = new List<GameObject>(); 

    #endregion

    void Start(){
        // Populate the dynamic remainingObjectives list with the netobjrefs of the objectivesList.

        if (!IsServer) return;
        foreach (GameObject objective in objectivesList){
            // get networkobject id
            if (!objective.TryGetComponent(out NetworkObject thisNetObj)){
                Debug.Log("Could not get NetworkObject from objective.");
            }
            ulong netId = thisNetObj.NetworkObjectId;
            if (!n_IObjectivesList.Contains(netId)){
                n_IObjectivesList.Add(netId);
            }
        }
    }

    public override void OnNetworkSpawn(){
        if (!IsServer) return;
        foreach (GameObject objective in objectivesList){
            // get networkobject id
            if (!objective.TryGetComponent(out NetworkObject thisNetObj)){
                Debug.Log("Could not get NetworkObject from objective.");
            }
            ulong netId = thisNetObj.NetworkObjectId;
            if (!n_IObjectivesList.Contains(netId)){
                n_IObjectivesList.Add(netId);
            }
        }

    }
    // ==============================================================================================
    #region = Objectives
    [ServerRpc(RequireOwnership = false)]
    public void CheckAllObjectivesServerRpc(){
        if (!IsServer) return;
        foreach (ulong netId in n_IObjectivesList){
            NetworkObject netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[netId];
            if (netObj == null){
                Debug.Log("NetworkObject is null.");
            }
            if (!netObj.GetComponent<IObjective>().IsComplete()){
                return;
            }
        }
        ClearedAllObjectivesServerRpc();
    }

    #endregion 

    // ==============================================================================================
    #region = NextPhase
    [ServerRpc(RequireOwnership = false)]
    private void ClearedAllObjectivesServerRpc(){
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