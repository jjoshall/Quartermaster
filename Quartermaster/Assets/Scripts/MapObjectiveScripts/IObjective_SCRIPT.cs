using UnityEngine;
using Unity.Netcode;

public abstract class IObjective : NetworkBehaviour
{
    public NetworkVariable<int> indexOfSpawnPoint;
    public abstract bool IsComplete();
    
    public void ClearObjective(){
        ObjectiveManager.instance.ClearObjectiveServerRpc(this.GetComponent<NetworkObject>(), indexOfSpawnPoint.Value);
    }
    public void NodeZoneTextHelper(){
        ObjectiveManager.instance.NodeZoneTextHelperClientRpc();
    }
    public void MailboxTextHelper(int remaining, int total, int previous){
        ObjectiveManager.instance.MailboxTextHelperServerRpc(remaining, total, previous, indexOfSpawnPoint.Value);
    }
}
