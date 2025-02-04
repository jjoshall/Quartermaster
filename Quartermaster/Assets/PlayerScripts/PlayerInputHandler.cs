using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    //[SerializeField] private PlayerControls playerInput;

    void Start(){
        //playerInput = new PlayerControls();
    }

    public void Move(InputAction.CallbackContext ctx){
        Vector2 move_vector = ctx.ReadValue<Vector2>();
        Debug.Log("move input detected");
        Debug.Log(move_vector);
        //return move_vector;
    }

    public void Jump(InputAction.CallbackContext ctx){
        if (ctx.performed){
            Debug.Log("Jumped");
        }
    }

    public void Look(InputAction.CallbackContext ctx){
        Vector2 look_vector = ctx.ReadValue<Vector2>();
        Debug.Log("look input detected");
        Debug.Log(look_vector);
        //return look_vector;
    }
}
