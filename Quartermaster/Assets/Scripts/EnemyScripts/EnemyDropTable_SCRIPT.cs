using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class EnemyDropTable_SCRIPT : NetworkBehaviour
{
    #region Inspector stuff
    public List<DropEntry> dropEntries = new List<DropEntry>();
    [Serializable]
    public struct DropEntry
    {
        public int itemID;
        public int quantity;
        public float dropChance;
    }
    #endregion 

    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
