using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemAcquisitionRange : MonoBehaviour
{
    private bool DEBUG_FLAG = true;
    private GameObject playerObj;

    private List<GameObject> itemsInRange;
    private GameObject closestItem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerObj = transform.parent.gameObject;
        itemsInRange = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        // updateClosestItem(); // Uncomment this line if we want some kind of real-time effect on closest item. (e.g. a shader)
    }

    void OnTriggerEnter(Collider other)
    {
        if (isAnItem(other.gameObject))
        {
            addItem(other.gameObject);
        }
    }

    public void addItem(GameObject item){
        if (itemsInRange.Contains(item))
        {
            return;
        }
        itemsInRange.Add(item);
        debug_print_items_in_range();
    }
    void OnTriggerExit(Collider other)
    {
        if (isAnItem(other.gameObject))
        {
            removeItem(other.gameObject);
        }
    }

    public void removeItem(GameObject item){
        if (!itemsInRange.Contains(item))
        {
            return;
        }
        itemsInRange.Remove(item);
        debug_print_items_in_range();
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
            float distance = Vector3.Distance(playerObj.transform.position, item.transform.position);
            if (distance < closestDistance)
            {
                localClosest = item;
                closestDistance = distance;
            }
        }
        closestItem = localClosest;
    }

    public GameObject getClosestItem(){
        updateClosestItem();
        return closestItem;
    }   

    bool isAnItem(GameObject obj){
        return obj.CompareTag("Item");
        // return true; 
    }

    private void debug_print_items_in_range(){
        if (DEBUG_FLAG)
        {
            string DEBUG_STRING = "DEBUG: Items in range: ";
            foreach (GameObject item in itemsInRange)
            {
                DEBUG_STRING += item.name + ", ";
            }
            Debug.Log(DEBUG_STRING);
        }
    }
}
