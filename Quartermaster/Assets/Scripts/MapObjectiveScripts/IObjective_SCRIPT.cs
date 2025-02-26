using UnityEngine;
using Unity.Netcode;

public abstract class IObjective : NetworkBehaviour
{
    public abstract bool IsComplete();
}
