using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    public float groundSpeed = 5f;
    private Vector3 playerVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void HandleMovement(){
        // handle rotation first


        // handle physical movement
        
    }
}
