using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemAcquisitionRange : MonoBehaviour {
    private bool DEBUG_FLAG = true;
    private GameObject _playerObj;
    public GameObject _playerCam;

    private List<GameObject> _itemsInRange = new List<GameObject>();
    private GameObject _closestItem;

    void Start() {
        _playerObj = transform.parent.gameObject;
        // _itemsInRange = new List<GameObject>();
    }

    void Update() {
        // updateClosestItem(); // Uncomment this line if we want some kind of real-time effect on closest item. (e.g. a shader)
    }

    void OnTriggerEnter(Collider other) {
        if (IsAnItem(other.gameObject)) {
            AddItem(other.gameObject);
        }
    }

    public void AddItem(GameObject item) {
        if (_itemsInRange.Contains(item)) { return; }
        _itemsInRange.Add(item);

        string stringID = ItemManager.instance.itemEntries[item.GetComponent<WorldItem>().GetItemID()].inventoryItemClass;
        // Debug_print_items_in_range();
        Debug_print_items_in_range(); 
    }

    void OnTriggerExit(Collider other) {
        if (IsAnItem(other.gameObject)) {
            RemoveItem(other.gameObject);
        }
    }

    public void RemoveItem(GameObject item) {
        if (!_itemsInRange.Contains(item)) { return; }
        _itemsInRange.Remove(item);
        // Debug_print_items_in_range();
    }

    private void UpdateClosestItem() {
        // Call this function every update() if we want to apply a shader to closest item.
        //     otherwise, only call it when we need to get the closest item.

        if (_itemsInRange.Count == 0) {
            _closestItem = null;
            return;
        }
        // Prioritize raycast over closest.
        Physics.Raycast(_playerCam.transform.position, _playerCam.transform.forward, out RaycastHit hit, Mathf.Infinity);
        if (hit.collider != null) {
            if (hit.collider.gameObject.CompareTag("Item")) {
                if (_itemsInRange.Contains(hit.collider.gameObject)) {
                    _closestItem = hit.collider.gameObject;
                    return;
                }
            }
        }

        GameObject localClosest = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject item in _itemsInRange) {
            float distance = Vector3.Distance(_playerObj.transform.position, item.transform.position);
            if (distance < closestDistance) {
                localClosest = item;
                closestDistance = distance;
            }
        }

        _closestItem = localClosest;
    }

    public GameObject GetClosestItem() {
        UpdateClosestItem();
        return _closestItem;
    }   

    bool IsAnItem(GameObject obj) {
        return obj.CompareTag("Item");
        // return true; 
    }

    private void Debug_print_items_in_range() {
        if (DEBUG_FLAG) {
            string DEBUG_STRING = "DEBUG: Items in range: ";
            foreach (GameObject item in _itemsInRange) {
                DEBUG_STRING += item.name + ", ";
            }

            Debug.Log(DEBUG_STRING);
        }
    }
}
