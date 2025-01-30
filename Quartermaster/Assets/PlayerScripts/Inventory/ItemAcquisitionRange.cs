using System.Collections.Generic;
using UnityEngine;

public class ItemAcquisitionRange : MonoBehaviour
{
    public GameObject parent;

    private List<GameObject> itemsInRange;
    private GameObject closestItem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        itemsInRange = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        // updateClosestItem(); // Uncomment this line if we want some kind of real-time effect on closest item. (e.g. a shader)
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered trigger");
        if (isAnItem(other.gameObject))
        {
            itemsInRange.Add(other.gameObject);
            string inventory = "Inventory: ";
            foreach (GameObject item in itemsInRange)
            {
                inventory += item.name + ", ";
            }
            Debug.Log(inventory);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isAnItem(other.gameObject))
        {
            itemsInRange.Remove(other.gameObject);
            string inventory = "Inventory: ";
            foreach (GameObject item in itemsInRange)
            {
                inventory += item.name + ", ";
            }
            Debug.Log(inventory);
        }
    }

    private void updateClosestItem(){
        // Call this function every update() if we want to apply a shader to closest item.
        //     otherwise, only call it when we need to get the closest item.
        
        if (itemsInRange.Count == 0)
        {
            closestItem = null;
            return;
        }

        GameObject localClosest = null;
        float closestDistance = Mathf.Infinity;
        foreach (GameObject item in itemsInRange)
        {
            float distance = Vector3.Distance(parent.transform.position, item.transform.position);
            if (distance < closestDistance)
            {
                localClosest = item;
                closestDistance = distance;
            }
        }
    }

    public GameObject getClosestItem(){
        updateClosestItem();
        return closestItem;
    }   

    bool isAnItem(GameObject obj){
        return obj.CompareTag("Item");
    }
}
