using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class DI_Manager_SCRIPT : NetworkBehaviour {
    [SerializeField] private DamageIndicator[] damageIndicators;
    private Queue<DamageIndicator> indicatorPool = new Queue<DamageIndicator>();

    public static DI_Manager_SCRIPT Instance { get; private set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }

        foreach (DamageIndicator indicator in damageIndicators) {
            indicator.gameObject.SetActive(false);
            indicatorPool.Enqueue(indicator);
        }
    }

    public void ShowDamageIndicator(Vector3 damageLocation) {
        if (indicatorPool.Count == 0) {
            DamageIndicator oldestIndicator = FindOldestActiveIndicator();

            if (oldestIndicator != null) {
                oldestIndicator.Initialize(damageLocation);
                return;
            }
            else {
                Debug.LogWarning("No available damage indicators.");
                return;
            }
        }

        DamageIndicator indicator = indicatorPool.Dequeue();
        indicator.gameObject.SetActive(true);
        indicator.Initialize(damageLocation);
    }

    private DamageIndicator FindOldestActiveIndicator() {
        DamageIndicator oldestIndicator = null;
        float lowestRemainingTime = float.MaxValue;

        foreach (DamageIndicator indicator in damageIndicators) {
            if (indicator.gameObject.activeSelf && indicator.RemainingFadeTime < lowestRemainingTime) {
                lowestRemainingTime = indicator.RemainingFadeTime;
                oldestIndicator = indicator;
            }
        }

        return oldestIndicator;
    }

    public void ReturnIndicatorToPool(DamageIndicator indicator) {
        indicatorPool.Enqueue(indicator);
    }

    [ClientRpc]
    public void ShowDamageIndicatorClientRpc(Vector3 damageLocation, ClientRpcParams clientRpcParams = default) {
        ShowDamageIndicator(damageLocation);
    }
}
