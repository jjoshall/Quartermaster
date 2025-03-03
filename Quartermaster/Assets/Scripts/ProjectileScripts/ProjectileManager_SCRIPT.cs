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


// ==============================================================================================
    #region LineRenderer
    public void SpawnLineRenderer(Vector3 position, Vector3 direction, float velocity){

        if (_localLineRenderer != null){
            Debug.LogError("Local line renderer already exists. Destroying it first.");
            Destroy(_localLineRenderer);
        }

        GameObject lr = Instantiate(_lineRendererPrefab, position, Quaternion.identity);
        lr.transform.SetParent(this.gameObject.transform);

        _localLineRenderer = lr;
        UpdateLineRenderer(position, direction, velocity);

    }

    public void UpdateLineRenderer(Vector3 position, Vector3 direction, float velocity){
        // Get component
        ArcLineRenderer alr = _localLineRenderer.GetComponent<ArcLineRenderer>();

        // Update variables.
        alr.velocity = velocity;
        alr.launchDirection = direction;
        _localLineRenderer.transform.position = position;

        // Call renderer update.
        alr.UpdateArc();
    }

    public void DestroyLineRenderer(){
        Destroy(_localLineRenderer);
        _localLineRenderer = null;
    }




    #endregion
// ==============================================================================================
    #region = Projectiles

    // Calls local spawn for self, then calls serverRpc which tells others to local spawn.
    public void SpawnSelfThenAll(string key, Vector3 position, Quaternion rotation)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (!projectilePool.ContainsKey(key))
        {
            Debug.LogError("Projectile key not found in ProjectileManager: " + key);
            return;
        }
        SpawnProjectileLocal(key, position, rotation);
        // Call SpawnDummyForOtherClientsServerRpc with the calling player's client id
        SpawnDummyForOthersServerRpc(key, position, rotation, localClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnDummyForOthersServerRpc(string key, Vector3 position, Quaternion rotation, ulong clientId)
    {
        // iterate over each client except clientId
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClients.Values)
        {
            if (client.ClientId == clientId) continue;
            SpawnDummyClientRpc(key, position, rotation);
        }
    }

    [ClientRpc]
    public void SpawnDummyClientRpc(string key, Vector3 position, Quaternion rotation)
    {
        // call local spawnProjectile for this client
        SpawnProjectileLocal(key, position, rotation);
    }

    #endregion



    // ==============================================================================================
    #region = PoolingHelpers
    public void SpawnProjectileLocal(string key, Vector3 position, Quaternion rotation)
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
