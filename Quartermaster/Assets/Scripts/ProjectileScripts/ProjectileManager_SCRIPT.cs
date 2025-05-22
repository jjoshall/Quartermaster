using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ProjectileManager : NetworkBehaviour
{
    #region Variables
    #endregion 






    #region Inspector
    [SerializeField, Range(-1.0f, 1.0f)] private float _offsetLineRight = 0.1f;
    [SerializeField, Range(-1.0f, 1.0f)] private float _offsetLineUp = 0.1f;
    [SerializeField]
    private GameObject _lineRendererPrefab;
    private GameObject _localLineRenderer; // stores the local line renderer. should not be more than one.
    [SerializeField]
    public List<ProjectileType> projectileTypes;    
    private Dictionary<string, List<GameObject>> projectilePool; // separate pool for each type.
                                                               // all pools are local. not networked.
    private Dictionary<string, List<GameObject>> dummyPool;
    
    [System.Serializable]
    public struct ProjectileType
    {
        public string key;
        public GameObject projectile;
        public GameObject networkDummy;
    }
    // Note: - projectile prefab handles actual collision logic 
    //                     and is spawned only on the requesting client.
    //       - networkdummy is spawned on other clients only for the visual effect.
    #endregion





    #region = Singleton
    public static ProjectileManager instance;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Initialization
    void Start(){
        
        projectilePool = new Dictionary<string, List<GameObject>>();
        // Initialize an object pool list in the dictionary for each particleType
        foreach (ProjectileType projectileType in projectileTypes){
            if (projectilePool.ContainsKey(projectileType.key)){
                Debug.LogError("Duplicate particle key found in ParticleManager: " + projectileType.key);
                continue;
            }
            projectilePool.Add(projectileType.key, new List<GameObject>());
        }
        
        _localLineRenderer = Instantiate(_lineRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        _localLineRenderer.SetActive(false);
        

    }
    #endregion

// ==============================================================================================
    #region LineRenderer
    public void SpawnLineRenderer(Transform camera, float velocity){
        if (_localLineRenderer == null){
            _localLineRenderer = Instantiate(_lineRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        } else {
            _localLineRenderer.SetActive(true);
        }

        // GameObject lr = Instantiate(_lineRendererPrefab, camera.position, Quaternion.identity);
        // lr.transform.SetParent(this.gameObject.transform);

        // _localLineRenderer = lr;
        _localLineRenderer.GetComponent<LineRenderer>().SetPositions(new Vector3[0]);
        UpdateLineRenderer(camera, velocity);

    }

    public void UpdateLineRenderer(Transform camera, float velocity){
        if (_localLineRenderer == null){
            _localLineRenderer = Instantiate(_lineRendererPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        } else {
            _localLineRenderer.SetActive(true);
        }
        // Get component
        ArcLineRenderer alr = _localLineRenderer.GetComponent<ArcLineRenderer>();

        // Update variables.
        alr.velocity = velocity;
        alr.launchDirection = camera.forward;
        alr.verticalAngle = ConvertVerticalAngle(camera.rotation.eulerAngles.x);
        // Vector3 cameraUp = camera.down;
        _localLineRenderer.transform.position = 
                    camera.position + 
                    camera.right * _offsetLineRight + 
                    camera.up * _offsetLineUp;

        // Call renderer update.
        alr.UpdateArc();
    }

    private float ConvertVerticalAngle (float verticalAngle){
        return 360.0f - verticalAngle;
    }

    public void DestroyLineRenderer(){
        _localLineRenderer.GetComponent<ArcLineRenderer>().ClearArc();
        _localLineRenderer.SetActive(false);
    }




    #endregion
// ==============================================================================================
    #region = Projectiles

    // Calls local spawn for self, then calls serverRpc which tells others to local spawn.
    public void SpawnSelfThenAll(string key, Vector3 position, Quaternion rotation, 
                                 Vector3 direction, float velocity, float expireTimer,
                                 GameObject user,
                                 params object[] args)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (!projectilePool.ContainsKey(key))
        {
            Debug.LogError("Projectile key not found in ProjectileManager: " + key);
            return;
        }
        SpawnProjectileLocal(key, position, rotation, direction, velocity, expireTimer, user, args);
        // Call SpawnDummyForOtherClientsServerRpc with the calling player's client id
        SpawnDummyForOthersServerRpc(key, position, rotation, direction, velocity, expireTimer, localClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnDummyForOthersServerRpc(string key, Vector3 position, Quaternion rotation, 
                                             Vector3 direction, float velocity, float expireTimer,
                                             ulong clientId)
    {
        List<ulong> targetClients = new List<ulong>();
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id != clientId) // Exclude the given clientId
            {
                targetClients.Add(id);
            }
        }
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targetClients.ToArray()
            }
        };
        SpawnDummyClientRpc(key, position, rotation, direction, velocity, expireTimer, clientRpcParams);
    }

    [ClientRpc]
    public void SpawnDummyClientRpc(string key, Vector3 position, Quaternion rotation,
                                    Vector3 direction, float velocity, float expireTimer, ClientRpcParams clientRpcParams = default)
    {
        // call local spawnProjectile for this client
        SpawnDummyLocal(key, position, rotation, direction, velocity, expireTimer);
    }

    #endregion



    // ==============================================================================================
    #region = PoolingHelpers
    public void SpawnProjectileLocal(string key, Vector3 position, Quaternion rotation, 
                                     Vector3 direction, float velocity, float expireTimer,
                                     GameObject user,
                                     params object[] args)
    {
        if (!projectilePool.ContainsKey(key))
        {
            Debug.LogError("Projectile key not found in ProjectileManager: " + key);
            return;
        }

        GameObject projectileObj;

        // If no pooled objects, instantiate.
        if (projectilePool[key].Count == 0)
        {
            GameObject typePrefab = projectileTypes.Find(x => x.key == key).projectile;
            projectileObj = Instantiate(typePrefab, position, rotation);
            Physics.IgnoreCollision(projectileObj.GetComponent<Collider>(), user.GetComponent<Collider>(), true);
            projectileObj.transform.SetParent(this.gameObject.transform);
        }
        else // else grab from pool.
        {
            projectileObj = projectilePool[key][0];
            projectilePool[key].RemoveAt(0);

            projectileObj.transform.position = position;
            projectileObj.transform.rotation = rotation;
            projectileObj.SetActive(true);
        }

        projectileObj.GetComponent<IProjectile>().sourcePlayer = user;

        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        rb.linearVelocity = direction * velocity;

        projectileObj.GetComponent<IProjectile>().InitializeData(expireTimer, args);
    }
    public void SpawnDummyLocal(string key, Vector3 position, Quaternion rotation,
                                Vector3 direction, float velocity, float expireTimer, params object[] args)
    {
        if (!projectilePool.ContainsKey(key))
        {
            Debug.LogError("Projectile key not found in ProjectileManager: " + key);
            return;
        }

        GameObject projectileObj;

        // If no pooled objects, instantiate.
        if (projectilePool[key].Count == 0)
        {
            GameObject typePrefab = projectileTypes.Find(x => x.key == key).networkDummy;
            projectileObj = Instantiate(typePrefab, position, rotation);
            projectileObj.transform.SetParent(this.gameObject.transform);
        }
        else // else grab from pool.
        {
            projectileObj = projectilePool[key][0];
            projectilePool[key].RemoveAt(0);

            projectileObj.transform.position = position;
            projectileObj.transform.rotation = rotation;
            projectileObj.SetActive(true);

        }
        
        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        rb.linearVelocity = direction * velocity;

        projectileObj.GetComponent<IProjectile>().InitializeData(expireTimer, args);
    }

    public void DespawnProjectileLocal(string key, GameObject projectileObj)
    {
        if (!projectilePool.ContainsKey(key))
        {
            Debug.LogError("Projectile key not found in ProjectileManager: " + key);
            return;
        }
        projectileObj.SetActive(false);
        projectilePool[key].Add(projectileObj);
    }

    public void DespawnDummyLocal(string key, GameObject dummyObj){
        if (!dummyPool.ContainsKey(key))
        {
            Debug.LogError("Dummy key not found in ProjectileManager: " + key);
            return;
        }
        dummyObj.SetActive(false);
        dummyPool[key].Add(dummyObj);
    }
    #endregion

}
