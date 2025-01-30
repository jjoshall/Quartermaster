using UnityEngine;

public class MedKit : Item
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void use()
    {
        Debug.Log("MedKit used");
    }

    public override void drop()
    {
        Debug.Log("MedKit dropped");
    }
}
