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
    public List<GameObject> objectivesList = new List<GameObject>();
    private NetworkList<NetworkObjectReference> n_remainingObjectives = new NetworkList<NetworkObjectReference>();

    #endregion

    void Start(){
        // Populate the dynamic remainingObjectives list with the netobjrefs of the objectivesList.
        foreach (GameObject objective in objectivesList){
            NetworkObjectReference netObjRef = new NetworkObjectReference(objective.GetComponent<NetworkObject>());
            n_remainingObjectives.Add(netObjRef);
        }
    }
    // ==============================================================================================
    #region = Objectives
    public void RemoveObjective(NetworkObjectReference netObjRef){
        n_remainingObjectives.Remove(netObjRef);

        if (n_remainingObjectives.Count == 0){
            ClearedAllObjectives();
        }
    }

    #endregion 

    // ==============================================================================================
    #region = NextPhase
    private void ClearedAllObjectives(){
        // Do something here. Boss phase.

    }



}