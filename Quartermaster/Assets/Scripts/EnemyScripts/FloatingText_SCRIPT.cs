using Unity.Netcode;
using TMPro;
using UnityEngine;

public class FloatingText_SCRIPT : NetworkBehaviour {
    [SerializeField] private float _moveSpeed = 20f;
    [SerializeField] private Vector3 _offset = new Vector3(0, 1f, 0);
    [SerializeField] private Vector3 _randomizeIntensity = new Vector3(2f, 0, 0);
    private Transform _playerCamera;

    [SerializeField] private TextMeshPro _text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start() {
        _playerCamera = Camera.main.transform;
        transform.localPosition += _offset;
        transform.localPosition += new Vector3(
            Random.Range(-_randomizeIntensity.x, _randomizeIntensity.x),
            Random.Range(-_randomizeIntensity.y, _randomizeIntensity.y),
            Random.Range(-_randomizeIntensity.z, _randomizeIntensity.z)
        );
    }

    private void Update() {
        if (_playerCamera != null) {
            transform.LookAt(_playerCamera);
            transform.rotation = Quaternion.LookRotation(transform.position - _playerCamera.position);
        }

        transform.position += new Vector3(0, _moveSpeed * Time.deltaTime, 0);
    }

    [ClientRpc]
    public void SetTextClientRpc(float damage) {
        _text.SetText(Mathf.RoundToInt(damage).ToString());
    }
}
