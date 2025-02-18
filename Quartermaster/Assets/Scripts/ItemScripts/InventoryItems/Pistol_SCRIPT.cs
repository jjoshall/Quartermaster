using UnityEngine;

public class Pistol : IWeapon
{
    // Backing fields
    private int id;
    private int ammo = 0;
    private float lastUsedTime = float.MinValue;
    private float lastFiredTime = float.MinValue;
    private static float itemCooldown = 0.05f;

    // Abstract overrides
    public override float cooldown
    {
        get => itemCooldown;
        set => itemCooldown = value;
    }
    public override int itemID {
        get => id;
        set => id = value;
    }

    public override float lastUsed {
        get => lastUsedTime;
        set => lastUsedTime = value;
    }

    public override int StackLimit(){
        return 1;
    }

    public override void Use(GameObject user)
    {
        string itemStr = ItemManager.instance.itemEntries[itemID].inventoryItemClass;
        if (lastUsed + cooldown > Time.time){
            Debug.Log(itemStr + " (" + itemID + ") is on cooldown.");
            Debug.Log ("cooldown remaining: " + (lastUsed + cooldown - Time.time));
            return;
        }
        Debug.Log(itemStr + " (" + itemID + ") used");
    
        if (IsConsumable()){
            quantity--;
        }
        lastUsed = Time.time;

        ItemEffect(user);

    }

    private void ItemEffect(GameObject user){
        // Do some kind of alternate attack, or reload.
        fire(user);
    }

    public override void fire(GameObject user){
        // raycast in direction camera is facing

        // int layerMask = layerMask.GetMask("Enemy", "Wall");

        // camera is a child, we do not know which one
        GameObject camera = user.transform.Find("Camera").gameObject;
            Debug.DrawRay(camera.transform.position, camera.transform.forward * 100, Color.yellow, 2f);
        
        RaycastHit[] hits = Physics.RaycastAll(camera.transform.position, camera.transform.forward, 100);
        foreach (RaycastHit hit in hits){
            Debug.Log(hit.transform.name);
            if (hit.transform.tag == "Enemy"){
                Debug.Log ("Enemy hit");
                return;
                // hit.transform.GetComponent<Enemy>().Damage(10);
            }
            if (hit.transform.tag == "Wall"){
                Debug.Log ("Pistol hit wall");
                return;
            }
            if (hit.transform.tag == "Player"){
                Debug.Log ("Pistol hit Player");
                return;
            }
        }
        
    }

}
