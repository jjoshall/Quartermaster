using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class ParticleManager : NetworkBehaviour
{
    #region = Singleton
    // singleton
    public static ParticleManager instance;
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
    #region = Variables 

    [Tooltip("List of particle types prefabs. String is key for object lookup")]
    [SerializeField]
    public List<ParticleType> particleTypesPrefabList;  // Inspector setting.
                                                        // ParticleType struct: key, duration, prefab
    private Dictionary<string, List<GameObject>> particlePool; // separate pool for each type.
                                                               // all pools are local. not networked.
    
    [System.Serializable]
    public struct ParticleType
    {
        public string key;
        public float duration;
        public GameObject particlePrefab;
    }
    #endregion



    // ==============================================================================================
    #region = Initialization
    void Start()
    {
        particlePool = new Dictionary<string, List<GameObject>>();
        // Initialize an object pool list in the dictionary for each particleType
        foreach (ParticleType particleType in particleTypesPrefabList){
            if (particlePool.ContainsKey(particleType.key)){
                Debug.LogError("Duplicate particle key found in ParticleManager: " + particleType.key);
                continue;
            }
            particlePool.Add(particleType.key, new List<GameObject>());
        }

    }
    #endregion



    // ==============================================================================================
    #region = ParticleStuff

    // Calls local spawn for self, then calls serverRpc which tells others to local spawn.
    public void SpawnSelfThenAll(string key, Vector3 position, Quaternion rotation)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        if (!particlePool.ContainsKey(key))
        {
            Debug.LogError("Particle key not found in ParticleManager: " + key);
            return;
        }
        SpawnParticleLocal(key, position, rotation);
        // Call SpawnParticleForOtherClientsServerRpc with the calling player's client id
        SpawnParticleForOthersServerRpc(key, position, rotation, localClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnParticleForOthersServerRpc(string key, Vector3 position, Quaternion rotation, ulong clientId)
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
        // iterate over each client except clientId
        // foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClients.Values)
        // {
        //     if (client.ClientId == clientId) continue;
        // }
        SpawnParticleClientRpc(key, position, rotation, clientRpcParams);
    }

    [ClientRpc]
    public void SpawnParticleClientRpc(string key, Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        // call local spawnparticle for this client
        SpawnParticleLocal(key, position, rotation);
    }

    #endregion



    // ==============================================================================================
    #region = PoolingHelpers
    public void SpawnParticleLocal(string key, Vector3 position, Quaternion rotation)
    {
        if (!particlePool.ContainsKey(key))
        {
            Debug.LogError("Particle key not found in ParticleManager: " + key);
            return;
        }

        GameObject particleObj;

        // If no pooled objects, instantiate.
        if (particlePool[key].Count == 0)
        {
            GameObject typePrefab = particleTypesPrefabList.Find(x => x.key == key).particlePrefab;
            particleObj = Instantiate(typePrefab, position, rotation);
            particleObj.transform.SetParent(this.gameObject.transform);
            float typeDuration = particleTypesPrefabList.Find(x => x.key == key).duration;
            if (key == "SlowTrapAoe"){
                typeDuration = GameManager.instance.SlowTrap_Duration;
                particleObj.GetComponent<BubbleAnimator>().BUBBLE_SCALE_MAX_RANGE = 
                    GameManager.instance.SlowTrap_AoERadius * 2.0f;
                // particleObj.transform.localScale = new Vector3(
                //                                     GameManager.instance.SlowTrap_AoERadius * 2.0f, 
                //                                     GameManager.instance.SlowTrap_AoERadius * 2.0f,
                //                                     GameManager.instance.SlowTrap_AoERadius * 2.0f);
                
            }
            PlayParticle(particleObj, key, typeDuration);
        }
        else // else grab from pool.
        {
            particleObj = particlePool[key][0];
            particlePool[key].RemoveAt(0);

            particleObj.transform.position = position;
            particleObj.transform.rotation = rotation;
            particleObj.SetActive(true);

            float typeDuration = particleTypesPrefabList.Find(x => x.key == key).duration;
            if (key == "SlowTrapAoe"){
                typeDuration = GameManager.instance.SlowTrap_Duration;
                particleObj.transform.localScale = new Vector3(
                                                    GameManager.instance.SlowTrap_AoERadius * 2.0f, 
                                                    GameManager.instance.SlowTrap_AoERadius * 2.0f, 
                                                    GameManager.instance.SlowTrap_AoERadius * 2.0f);

            }
            PlayParticle(particleObj, key, typeDuration);
        }
    }

    public void DespawnParticleLocal(string key, GameObject particleObj)
    {
        if (!particlePool.ContainsKey(key))
        {
            Debug.LogError("Particle key not found in ParticleManager: " + key);
            return;
        }
        particleObj.SetActive(false);
        particlePool[key].Add(particleObj);
    }
    #endregion

    // Reinitialize and do any related logic on the particle obj to get it to animate/play.
    private void PlayParticle(GameObject particle, string key, float duration){
        particle.GetComponent<ParticleEffect>().InitializeParticle(key, duration);
    }


}
