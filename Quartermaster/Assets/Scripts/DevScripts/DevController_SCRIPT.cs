using UnityEngine;
using Unity.Netcode;

public class DevController : NetworkBehaviour
{
    [SerializeField] public Camera cam;
    public NetworkObject n_playerObj;
    private Rigidbody _rb;
    private float _isSprinting = 1.0f;
    [SerializeField] float _sprintMultiplier;
    [SerializeField] private float _devBaseSpeed;

    [SerializeField] private int packSize = 10;
    [SerializeField] private float enemySpread = 2f;
    private EnemySpawner enemySpawner;

    [SerializeField] float mouseSens = 100.0f;
    private float _xRotation = 0.0f;
    private float _yRotation = 0.0f;

    GameObject canvas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemySpawner = EnemySpawner.instance;
    }


    public void InitDevController()
    {
        // find the Player UI Canvas
        if (!canvas){
            canvas = GameObject.Find("Player UI Canvas");
        }
        if (canvas != null)
        {
            // set the player object to the player UI canvas
            canvas.SetActive(false);
        }
        // lock mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (!_rb){
            _rb = GetComponent<Rigidbody>();
        }
        this.transform.rotation = Quaternion.Euler(0, 0, 0);
        

    }
    // Update is called once per frame
    void Update()
    {
        MouseCameraMovement();
        WASD(); // checks wasd hold for movement.
        ItemSpawn(); // checks 123456qert keydown for item spawn at raycast.
        EnemySpawn();
        Sprint();
        ReturnToBeingPlayer();
        IncreaseDecreaseSpeed();

    }

    private void ReturnToBeingPlayer(){
        if (Input.GetKeyDown(KeyCode.RightBracket)){
            n_playerObj.gameObject.SetActive(true);
            n_playerObj.transform.position = transform.position;
            n_playerObj.transform.rotation = transform.rotation;
            this.gameObject.SetActive(false);
            if (canvas){
                canvas.SetActive(true);
            }
        }
    }

    private void MouseCameraMovement(){
        // rotate camera up/down and left/right
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * mouseSens;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * mouseSens;

        // Adjust vertical rotation (pitch)
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);  // Clamp to prevent flipping

        _yRotation += mouseX;

        // Apply pitch rotation to the camera
        cam.transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        // cam.transform.Rotate(Vector3.up * mouseX);
        
    }

    private void EnemySpawn(){
        if (Input.GetKeyDown(KeyCode.G)){
            Vector3 spawnPosition = RaycastGround();

            if (spawnPosition != Vector3.zero) {
                if (enemySpawner != null) {
                    enemySpawner.SpawnEnemyPackAtRandomPointServerRpc(packSize, spawnPosition, enemySpread);
                }
            }
        }
    }

    private void Sprint(){
        if (Input.GetKeyDown(KeyCode.LeftShift)){
            _isSprinting = _sprintMultiplier;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift)){
            _isSprinting = 1.0f;
        }
    }

    private void IncreaseDecreaseSpeed(){
        if (Input.GetKeyDown(KeyCode.Equals)){
            _devBaseSpeed += 1.0f;
        }
        if (Input.GetKeyDown(KeyCode.Minus)){
            _devBaseSpeed -= 1.0f;
        }
    }

    private void SwitchToPlayer(){
        if (Input.GetKeyDown(KeyCode.RightBracket)){
            n_playerObj.gameObject.SetActive(true);
            n_playerObj.transform.position = transform.position;
            n_playerObj.transform.rotation = transform.rotation;
            this.gameObject.SetActive(false);
        }
    }

    private void WASD(){

        // wasd movement
        if (Input.GetKey(KeyCode.W))
        {
            // addforce rigidbody in direction of camera forward 
            _rb.AddForce(cam.transform.forward * _devBaseSpeed * _isSprinting);  
        }
        if (Input.GetKey(KeyCode.S))
        {
            // addforce rigidbody in direction of camera back
            _rb.AddForce(-cam.transform.forward * _devBaseSpeed * _isSprinting);
        }
        if (Input.GetKey(KeyCode.A))
        {
            // addforce rigidbody in direction of camera left
            _rb.AddForce(-cam.transform.right * _devBaseSpeed * _isSprinting);
        }
        if (Input.GetKey(KeyCode.D))
        {
            // addforce rigidbody in direction of camera right
            _rb.AddForce(cam.transform.right * _devBaseSpeed * _isSprinting);
        }
    }

    private void ItemSpawn(){
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // spawn item 1
            ItemManager.instance.DropSpecificItem(0, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // spawn item 2
            ItemManager.instance.DropSpecificItem(1, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // spawn item 3
            ItemManager.instance.DropSpecificItem(2, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // spawn item 4
            ItemManager.instance.DropSpecificItem(3, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // spawn item 5
            ItemManager.instance.DropSpecificItem(4, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            // spawn item 6
            ItemManager.instance.DropSpecificItem(5, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // spawn item 7
            ItemManager.instance.DropSpecificItem(6, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            // spawn item 8
            ItemManager.instance.DropSpecificItem(7, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            // spawn item 9
            ItemManager.instance.DropSpecificItem(8, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            // spawn item 10
            ItemManager.instance.DropSpecificItem(9, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.Y))
        {
            // spawn item 11
            ItemManager.instance.DropSpecificItem(10, RaycastGround());
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            // spawn item 12
            ItemManager.instance.RollDropTable(RaycastGround());
        }
    }


    private Vector3 RaycastGround(){
        // Prioritize raycast over closest.
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, Mathf.Infinity)){
            return hit.point;
        }
        return Vector3.zero;
    }

}
