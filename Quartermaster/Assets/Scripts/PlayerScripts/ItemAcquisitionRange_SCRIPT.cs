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

    #region InspectorVariables
    #endregion 
    public float raycastPickupRange = 1.5f; // default val
    public float nolookClosestPickupRange = 0.5f; // default val
    [Tooltip("Offset helps avoid clipping, but also increases range")] public float raycastOffset = 0.5f; // default val

    void Start() {
        _playerObj = transform.parent.gameObject;
        var spherecol = this.GetComponent<SphereCollider>();
        if (spherecol != null) {
            spherecol.radius = raycastPickupRange + raycastOffset;
        } else {
            Debug.LogError("ItemAcquisitionRange: Start() - SphereCollider is null.");
        }
        _itemsInRange = new List<GameObject>();
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
            if (_closestItem != null){
                RemoveOutline(_closestItem);
                _closestItem = null;
            }
            return;
        }

        // Prioritize raycast over closest.
        GameObject raycastedItem = RaycastReturnFirstItem();
        if (raycastedItem != null) {
            _closestItem = raycastedItem;
        } else {
            _closestItem = HighlightClosestItem();
        }


        // Update outlines for prev highlighted and curr highlighted.
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

    private GameObject RaycastReturnFirstItem() {
        Vector3 raycastOrigin = _playerCam.transform.position + _playerCam.transform.forward * raycastOffset; // helps avoid clipping close items outside player fov

        RaycastHit[] hits = Physics.RaycastAll(raycastOrigin, _playerCam.transform.forward, raycastPickupRange);
        Debug.DrawRay (raycastOrigin, _playerCam.transform.forward * raycastPickupRange, Color.red, 0.1f);
        foreach (RaycastHit itemHit in hits) {
            if (itemHit.collider != null && itemHit.collider.gameObject != null
                    // && _itemsInRange.Contains(itemHit.collider.gameObject)
                    && IsAnItem(itemHit.collider.gameObject)
                    && itemHit.collider.gameObject.GetComponent<Item>().IsPickedUp == false) {
                return itemHit.collider.gameObject;
            }
        }
        return null;
    }
    
    private GameObject HighlightClosestItem(){
        GameObject localClosest = null;
        float closestDistance = Mathf.Infinity;

        CleanList();
        foreach (GameObject item in _itemsInRange)
        {
            float distance = Vector3.Distance(_playerObj.transform.position, item.transform.position);
            if (distance > nolookClosestPickupRange)
            {
                continue; // skip if outside of range
            }
            if (distance < closestDistance && IsAnItem(item))
            {
                localClosest = item;
                closestDistance = distance;
            }
        }
        return localClosest;
    }

    private void CleanList()
    {
        List<GameObject> newList = new List<GameObject>();
        foreach (GameObject item in _itemsInRange)
        {
            if (item != null)
            {
                newList.Add(item);
            }
        }
        _itemsInRange = newList;
    }

    private void ActivateOutlineShader(GameObject item)
    {
        Outline outline = item.GetComponent<Outline>();
        if (outline == null)
        {
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
