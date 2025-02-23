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

    private NetworkList<NetworkObjectReference> n_objectiveList = new NetworkList<NetworkObjectReference>();
    private NetworkList<bool> n_objectiveStatus = new NetworkList<bool>();


    #endregion

    void Start(){
        // Populate the dynamic remainingObjectives list with the netobjrefs of the objectivesList.
        foreach (GameObject objective in objectivesList){
            n_objectiveStatus.Add(false);
            n_objectiveList.Add(new NetworkObjectReference(objective.GetComponent<NetworkObject>()));
        }
    }
    // ==============================================================================================
    #region = Objectives

    // An objective should call this when it detects it is cleared.
    public void ClearObjective(NetworkObjectReference netObjRef){
        for (int i = 0; i < n_objectiveList.Count; i++){
            if (n_objectiveList[i].Equals(netObjRef)){
                n_objectiveStatus[i] = true;
                break;
            }
        }
        CheckAllObjectives();
    }

    public void CheckAllObjectives(){
        bool allObjectivesCleared = true;
        foreach (bool status in n_objectiveStatus){
            if (!status){
                allObjectivesCleared = false;
                break;
            }
        }
        if (allObjectivesCleared){
            ClearedAllObjectives();
        }
    }

    #endregion 

    // ==============================================================================================
    #region = NextPhase
    private void ClearedAllObjectives(){
        // Do something here. Boss phase.

    }


    #endregion
}