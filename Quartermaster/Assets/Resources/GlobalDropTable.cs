using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GlobalDropTable", menuName = "Drops/Global Drop Table")]
public class GlobalDropTable : ScriptableObject {
    [Serializable]
    public class StackSizeConfig {
        [Tooltip("Quantity to drop")]
        public int size;
        [Tooltip("Relative weight for this size")]
        public float weight;
    }

    [Serializable]
    public class ItemDropConfig {
        [Tooltip("The prefab of the item to drop")]
        public GameObject itemPrefab;
        [Tooltip("Relative weight of this item in the roll")]
        public float weight;
        [Tooltip("Possible stack sizes and their relative weights (<= item.StackLimit)")]
        public List<StackSizeConfig> stackSizes;
    }

    [Tooltip("Every possible item in the game")]
    public List<ItemDropConfig> itemDropConfigs;
}
