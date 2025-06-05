// Assets/Editor/TileRandomizerSettings.cs
using UnityEngine;

[System.Serializable]
public class TileRandomizerSettings : ScriptableObject
{
    public GameObject[] tilePrefabs = new GameObject[4];
    public float heightVariationChance = 0.2f;
    public float rotationVariationChance = 0.3f;
    public float maxHeightVariation = 0.05f;
    public float baseHeight = 0.5f;
    public float gridSize = 2f;
    public bool lockRotationX = true;
    public bool lockRotationY = false;
    public bool lockRotationZ = true;
}
