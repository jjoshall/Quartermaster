
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.PostProcessing;
using Unity.VisualScripting;
using UnityEngine.Localization.SmartFormat.Utilities;
using System;
using TMPro;

public class ObjectiveManager : NetworkBehaviour {

    #region InspectorSettings
    public int objectivesToWin; // = max(foreach objectivetype * minimumtospawn, objectivesToWin)
    public int initialSpawnCount;
    [SerializeField] private TextMeshProUGUI taskList;
    public List<ObjectiveType> minPerObjective; // minimum number of each objective to spawn
    [System.Serializable]
    public struct ObjectiveType {   
        public int minimumToSpawn;
        public GameObject objectivePrefab;
    }



    #endregion




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
    #endregion 

    #region RuntimeVars
    [Header("Runtime")]
    private NetworkVariable<int> n_objectivesToWin 
                            = new NetworkVariable<int>(); // initialized to N. decremented at runtime.
    private NetworkList<int> n_minPerObjective = new NetworkList<int>();
    // validpointsList.
    private List<GameObject> _validPointsList = new List<GameObject>();
    private NetworkList<bool> n_validPointHasObj = new NetworkList<bool>();
    // objectivesList. Populate by spawning
    // private List<GameObject> _objectivesList = new List<GameObject>(); 
    // NetworkList. Track active
    private NetworkList<ulong> n_ActiveObjectives = new NetworkList<ulong>();

    #endregion

    void Start(){
        // InitializeObjectiveManager();
    }
    public override void OnNetworkSpawn(){
        if (IsServer){
            InitializeObjectiveManager();
        } else if (IsClient){
            Debug.Log("ObjectiveManager: OnNetworkSpawn() client.");
            // Spawn n_activeobjectives
            foreach (ulong netId in n_ActiveObjectives)
            {
                NetworkObject netObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[netId];
                if (netObj != null)
                {
                    GameObject instance = Instantiate(netObj.gameObject, netObj.transform.position, netObj.transform.rotation);
                    instance.GetComponent<NetworkObject>().SpawnWithOwnership(netId);
                }
            }
        }
    }

    private void InitializeObjectiveManager(){
        if (!IsServer) return;
        GrabValidPointsServerRpc();
        InitRuntimeNetworkVarsServerRpc();
        LowerBoundWinServerRpc();
        initialSpawnCount = Mathf.Min(initialSpawnCount, objectivesToWin);
        SpawnObjectivesServerRpc(initialSpawnCount);  
    }


    [ServerRpc(RequireOwnership = false)]
    private void GrabValidPointsServerRpc(){
        // get all children of this object
        foreach (Transform child in transform){
            // add all children to the objectivesList
            _validPointsList.Add(child.gameObject);
            n_validPointHasObj.Add(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitRuntimeNetworkVarsServerRpc(){
        n_objectivesToWin.Value = objectivesToWin;
        for (int i = 0; i < minPerObjective.Count; i++){
            n_minPerObjective.Add(minPerObjective[i].minimumToSpawn);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void LowerBoundWinServerRpc(){
        
        int total = 0;                                         // grab total of min per objective.
        for (int i = 0; i < n_minPerObjective.Count; i++){
            total += n_minPerObjective[i];
        }
        n_objectivesToWin.Value = Mathf.Max(objectivesToWin, total);   // lower bound objectivesToWin
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnObjectivesServerRpc(int count){
        for (int i = 0; i < count; i++){
            if (!TotalMinLessThanObjectivesToWin()){
                CullZeroesServerRpc();
            }
            // spawn a random objective.
            SpawnRandomObjectiveServerRpc();
        }

    }

    // Checks the runtime network variables.
    private bool TotalMinLessThanObjectivesToWin(){
        int total = 0;
        for (int i = 0; i < n_minPerObjective.Count; i++){
            total += n_minPerObjective[i];
        }
        return total < n_objectivesToWin.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CullZeroesServerRpc(){
        if (!IsServer) return;
        for (int i = 0; i < minPerObjective.Count; i++){
            if (minPerObjective[i].minimumToSpawn == 0){
                minPerObjective.RemoveAt(i);
                n_minPerObjective.RemoveAt(i);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnRandomObjectiveServerRpc(){
        int randType = UnityEngine.Random.Range(0, minPerObjective.Count);
        GameObject prefab = minPerObjective[randType].objectivePrefab;
        
        int randValid = GetRandomValidPoint();
        n_validPointHasObj[randValid] = true;
        Vector3 randomValidGround = RaycastToGround(_validPointsList[randValid]);

        GameObject instance = Instantiate(prefab, randomValidGround, Quaternion.identity);
        NetworkObject n_instance = instance.GetComponent<NetworkObject>();
        instance.GetComponent<IObjective>().indexOfSpawnPoint.Value = randValid;
        n_instance.Spawn();

        n_ActiveObjectives.Add(n_instance.NetworkObjectId);

        n_instance.TrySetParent(this.transform);


        n_objectivesToWin.Value--;
        n_minPerObjective[randType]--;

        if (randType == 0) {
            taskList.text += "-Deliver the item to the mailbox. " + $"<size=1%>{randValid + 11}</size>" + "\n";
        }
        else if (randType == 1) {
            taskList.text += "-Locate and defend the node! " + $"<size=1%>{randValid + 11}</size>" + "\n";
        }
        // taskList.text += minPerObjective[randType].objectivePrefab + "\n";
    }

    private int GetRandomValidPoint(){
        int rand = UnityEngine.Random.Range(0, _validPointsList.Count);

        int tries = 1000;
        for (int i = 0; i < tries; i++){
            if (n_validPointHasObj[rand]){
                rand = UnityEngine.Random.Range(0, _validPointsList.Count);
            } else {
                break;
            }
        }

        if (n_validPointHasObj[rand]){
            Debug.Log("ObjectiveManager: GetRandomValidPoint() failed to find a valid point in " + tries + " tries. Returning last rand attempt.");
        }


        return rand;

    }

    private Vector3 RaycastToGround(GameObject validPoint){
        RaycastHit hit;
        LayerMask mask = LayerMask.GetMask("whatIsGround");
        float RAYCAST_DISTANCE = 10.0f;
        if (Physics.Raycast(validPoint.transform.position, Vector3.down, out hit, RAYCAST_DISTANCE, mask)){
            return hit.point;
        }
        if (hit.collider == null){
            return validPoint.transform.position;
        }
        return hit.point;
    }

    // ==============================================================================================
    #region = Objectives
    [ServerRpc(RequireOwnership = false)]
    public void ClearObjectiveServerRpc(NetworkObjectReference refe, int index){
        taskList.text = taskList.text.Replace((index + 11).ToString(), $"<size=100%> Complete!</size>");
        if (!IsServer) return;
        if (!refe.TryGet(out NetworkObject netObj)){
            Debug.LogError("ObjectiveManager: ClearObjective() netObj is null.");
        }
        IObjective obj = netObj.GetComponent<IObjective>();
        if (obj == null){
            Debug.LogError("ObjectiveManager: ClearObjective() obj is null.");
            return;
        }
        n_validPointHasObj[index] = false;
        n_objectivesToWin.Value--;
        n_ActiveObjectives.Remove(netObj.NetworkObjectId);
        netObj.Despawn(true);

        // spawn some kind of projectile?
        if (n_objectivesToWin.Value > 0){
            SpawnObjectivesServerRpc(1);
        } else {
            ClearedAllObjectivesServerRpc();
        }
    }

    #endregion 

    // ==============================================================================================
    #region = BossPhase172
    [ServerRpc(RequireOwnership = false)]
    private void ClearedAllObjectivesServerRpc(){

        if (n_objectivesToWin.Value <= (objectivesToWin * -1)) { taskList.text += "All objectives complete!" + "\n"; } // not sure how this code will interact with increasing the amount of objectives spawned

        // Do something here. Boss phase.
        DebugAllClientRpc("ObjectiveManager: ClearedAllObjectives() placeholder clientRPC msg.");
        TooltipManager.SendTooltip("All objectives cleared. Stay tuned for the boss fight in 172!");
        // CALL END GAME METHOD HERE AND SHOW SCORES FOR STATS
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