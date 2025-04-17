using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemAcquisitionRange : MonoBehaviour {
    private bool DEBUG_FLAG = true;
    private GameObject _playerObj;
    public GameObject _playerCam;

    private List<GameObject> _itemsInRange = new List<GameObject>();
    private GameObject _closestItem;
    
    private GameObject _lastClosestItem;
    public float pickupSphereRadius = 0.5f;

    void Start() {
        _playerObj = transform.parent.gameObject;
        // _itemsInRange = new List<GameObject>();
    }

    void Update() {
        UpdateClosestItem();
    }

    void OnTriggerEnter(Collider other) {
        if (IsAnItem(other.gameObject)) {
            AddItem(other.gameObject);
        }
    }

    public void AddItem(GameObject item) {
        if (_itemsInRange.Contains(item)) { return; }
        _itemsInRange.Add(item);

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
    }

    private void UpdateClosestItem() {
        if (_itemsInRange.Count == 0) {
            if (_lastClosestItem != null) {
                RemoveOutline(_lastClosestItem);
                _lastClosestItem = null;
            }
            _closestItem = null;
            return;
        }

        // Prioritize raycast over closest.
        Physics.Raycast(_playerCam.transform.position, _playerCam.transform.forward, out RaycastHit hit, Mathf.Infinity);
        if (hit.collider != null && hit.collider.gameObject != null
                && _itemsInRange.Contains(hit.collider.gameObject)
                && IsAnItem(hit.collider.gameObject)) {

            _closestItem = hit.collider.gameObject;
        } else {

            GameObject localClosest = null;
            float closestDistance = Mathf.Infinity;

            foreach (GameObject item in _itemsInRange) {
                float distance = Vector3.Distance(_playerObj.transform.position, item.transform.position);
                if (distance < closestDistance && IsAnItem(item)) {
                    localClosest = item;
                    closestDistance = distance;
                }
            }

            _closestItem = localClosest;
        }

        if (_closestItem != _lastClosestItem) {
            if (_lastClosestItem != null) {
                RemoveOutline(_lastClosestItem);
            }
            if (_closestItem != null) {
                ActivateOutlineShader(_closestItem);
            }
            _lastClosestItem = _closestItem;
        }
    }

    private void ActivateOutlineShader(GameObject item) {
        Outline outline = item.GetComponent<Outline>();
        if (outline == null) {
            outline = item.AddComponent<Outline>();
        }
        outline.OutlineWidth = 15f;
    }

    private void RemoveOutline(GameObject item) {
        Outline outline = item.GetComponent<Outline>();
        if (outline != null) {
            Destroy(item.GetComponent<Outline>());
        }
    }

    public GameObject GetClosestItem() {
        UpdateClosestItem();
        return _closestItem;
    }   

    bool IsAnItem(GameObject obj) {
        return obj.CompareTag("Item") && obj.GetComponent<Item>() != null && !obj.GetComponent<Item>().IsPickedUp;
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
