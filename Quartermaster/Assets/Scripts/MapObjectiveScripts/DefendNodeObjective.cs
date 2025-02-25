using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class DefendNodeObjective : IObjective
{
    public List<GameObject> nodes;

    public override bool IsComplete()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].GetComponent<NodeDefense>().defenseCompleted == false) 
            {
                return false;
            }
        }
        return true;
        // throw new System.NotImplementedException();
    }


}
