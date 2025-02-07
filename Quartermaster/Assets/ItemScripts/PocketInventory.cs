using UnityEngine;

public class PocketInventory : MonoBehaviour
{
    // Script for the pocket inventory prefab.
        // Handles teleportation, return position, cooldown.
        // Singleton (there is only one pocket inventory).

    public static PocketInventory instance;
    void Awake(){
        // Singleton
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

    public Vector3 teleportPosition;
    public Vector3 playerReturnPosition;

    // store time when player teleported to pocket
    public float timeEnteredPocket;
    private static float MAX_TIME_IN_POCKET = 10.0f;
    private static float POCKET_COOLDOWN = 100.0f;

    private GameObject playerInPocket;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInPocket = null;
        timeEnteredPocket = 0;
        teleportPosition = this.transform.position + new Vector3 (0, 2, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerInPocket)
        {
            if (Time.time - timeEnteredPocket > MAX_TIME_IN_POCKET)
            {
                returnToPreviousPosition(GameObject.Find("Player"));
                playerInPocket = null;
            }
        }
    }

    public void teleportToPocket(GameObject user){

        if (playerInPocket != null)
        {
            Debug.Log ("already a player in pocket: " + playerInPocket);
            return;
        }

        float time_since_last_use = Time.time - timeEnteredPocket;
        if (time_since_last_use < POCKET_COOLDOWN)
        {
            Debug.Log ("pocket cooldown not met: " + time_since_last_use);
            return;
        }

        playerReturnPosition = user.transform.position;
        user.transform.position = teleportPosition;
        playerInPocket = user;
        timeEnteredPocket = Time.time;
    }

    private void returnToPreviousPosition(GameObject user){
        user.transform.position = playerReturnPosition;
    }
}
