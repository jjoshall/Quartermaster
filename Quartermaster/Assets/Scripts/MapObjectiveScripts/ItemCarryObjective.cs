using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ItemCarryObjective : IObjective
{
    public List<GameObject> destinations; // Populate in inspector.

    public override bool IsComplete()
    {
        for (int i = 0; i < destinations.Count; i++)
        {
            if (destinations[i].GetComponent<ItemCarryTriggerVolume>().GetItemsToDeliver() != 0)
            {
                return false;
            }
        }
        return true;
    }
}
