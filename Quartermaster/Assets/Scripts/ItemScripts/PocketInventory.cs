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

    private Vector3 teleportPosition;
    private Vector3 playerReturnPosition;

    // store time when player teleported to pocket
    public float timeEnteredPocket;
    private static float MAX_TIME_IN_POCKET = 10.0f;

    private GameObject playerInPocket;
    public GameObject droppedPortalKeyInPocket;
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
                returnToPreviousPosition(playerInPocket);
            }
        }
    }

    public GameObject playerInsidePocket(){
        return playerInPocket;
    }

    public void teleportToPocket(GameObject user){

        if (playerInPocket != null)
        {
            Debug.Log ("already a player in pocket: " + playerInPocket);
            return;
        }

        playerReturnPosition = user.transform.position; // save return spot
        teleportUserToPosition (user, teleportPosition);     // teleport 
        playerInPocket = user;
        timeEnteredPocket = Time.time;
    }

    private void teleportUserToPosition(GameObject user, Vector3 position){
        while (user.GetComponent<PlayerController>().toggleCharacterController()){
            // until toggle returns false for toggled off.
        }
        user.transform.position = position;
        while (!user.GetComponent<PlayerController>().toggleCharacterController()){
            // until toggle returns true for toggled on.
        }
    }

    public void returnToPreviousPosition(GameObject user){
        teleportUserToPosition (playerInPocket, playerReturnPosition);
        if (droppedPortalKeyInPocket != null){
            Debug.Log ("dropped key returned at user's position");
            droppedPortalKeyInPocket.transform.position = playerReturnPosition;
        }
        playerInPocket = null; // change to remove from array if multiple players
        droppedPortalKeyInPocket = null;
    }
}
