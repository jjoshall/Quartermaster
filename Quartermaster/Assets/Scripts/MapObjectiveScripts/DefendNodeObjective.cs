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
            if (nodes[i].GetComponent<NodeDefense>().n_defenseCompleted.Value == false) 
            {
                return false;
            }
        }

        GameManager.instance.AddScoreServerRpc(200);
        Debug.Log("Total score " + GameManager.instance.totalScore.Value);
        return true;
        // throw new System.NotImplementedException();
    }


}
