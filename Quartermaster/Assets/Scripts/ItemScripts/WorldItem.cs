using UnityEngine;

public class WorldItem : MonoBehaviour
{
    // WorldItem : MonoBehavior is for item objects in the game space.
    //             All game space item prefabs will have this script attached.
    //             All other item scripts correspond to inventory items. 

    public int itemID; // inherited from ItemManager's list.
    void initializeItemID(int id){
    // Called by ItemManager instantiation. ID is index of item in ItemManager's prefab list. 
        itemID = id;
    }

    void Start()
    {
        
    }

    void Update()
    {
        floatyAnimation();
    }

    void floatyAnimation(){
        // Make the item float up and down
    }

}
