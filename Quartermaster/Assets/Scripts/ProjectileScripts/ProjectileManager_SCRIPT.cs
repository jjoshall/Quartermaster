using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ProjectileManager : NetworkBehaviour
{
    #region Variables
    #endregion 






    #region Inspector
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

    }
    #endregion

// ==============================================================================================
    #region LineRenderer
    public void SpawnLineRenderer(Transform camera, float velocity){
        if (_localLineRenderer != null){
            Debug.LogError("Local line renderer already exists. Destroying it first.");
            Destroy(_localLineRenderer);
        }

        GameObject lr = Instantiate(_lineRendererPrefab, camera.position, Quaternion.identity);
        lr.transform.SetParent(this.gameObject.transform);

        _localLineRenderer = lr;
        UpdateLineRenderer(camera, velocity);

    }

    public void UpdateLineRenderer(Transform camera, float velocity){
        // Get component
        ArcLineRenderer alr = _localLineRenderer.GetComponent<ArcLineRenderer>();

        // Update variables.
        alr.velocity = velocity;
        alr.launchDirection = camera.forward;
        alr.verticalAngle = ConvertVerticalAngle(camera.rotation.eulerAngles.x);
        _localLineRenderer.transform.position = camera.position + camera.right * 0.1f;

        // Call renderer update.
        alr.UpdateArc();
    }

    private float ConvertVerticalAngle (float verticalAngle){
        return 360.0f - verticalAngle;
    }

    public void DestroyLineRenderer(){
        Destroy(_localLineRenderer);
        _localLineRenderer = null;
    }




    #endregion
// ==============================================================================================
    #region = Projectiles

    // Calls local spawn for self, then calls serverRpc which tells others to local spawn.
    public void SpawnSelfThenAll(string key, Vector3 position, Quaternion rotation, 
                                 Vector3 direction, float velocity)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (!projectilePool.ContainsKey(key))
        {
            Debug.LogError("Projectile key not found in ProjectileManager: " + key);
            return;
        }
        SpawnProjectileLocal(key, position, rotation, direction, velocity);
        // Call SpawnDummyForOtherClientsServerRpc with the calling player's client id
        SpawnDummyForOthersServerRpc(key, position, rotation, direction, velocity, localClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnDummyForOthersServerRpc(string key, Vector3 position, Quaternion rotation, 
                                             Vector3 direction, float velocity,
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
        SpawnDummyClientRpc(key, position, rotation, direction, velocity, clientRpcParams);
    }

    [ClientRpc]
    public void SpawnDummyClientRpc(string key, Vector3 position, Quaternion rotation,
                                    Vector3 direction, float velocity, ClientRpcParams clientRpcParams = default)
    {
        // call local spawnProjectile for this client
        SpawnProjectileLocal(key, position, rotation, direction, velocity);
    }

    #endregion



    // ==============================================================================================
    #region = PoolingHelpers
    public void SpawnProjectileLocal(string key, Vector3 position, Quaternion rotation, 
                                     Vector3 direction, float velocity)
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
    }
    public void SpawnDummyLocal(string key, Vector3 position, Quaternion rotation)
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
