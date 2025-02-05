using UnityEngine;
using UnityEngine.InputSystem;


/*
USE CALLBACKS TO SET VARIABLES, HAVE {GET; PRIVATE SET} FOR DATA VALUES TO BE PASSED TO CONTROLLER WHICH HAS ACTUAL UPDATE()
*/
public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 move_vector { get; private set;}
    public Vector2 look_vector { get; private set;}
    public bool jumped {get; private set;}
    

    void Start(){
        //playerInput = new PlayerControls();
        move_vector = Vector2.zero;
        look_vector = Vector2.zero;
        jumped = false;
    }

    public void Move(InputAction.CallbackContext ctx){
        move_vector = ctx.ReadValue<Vector2>();
    }

    public void Jump(InputAction.CallbackContext ctx){
        if (ctx.performed){
            jumped = true;
        }
        if (ctx.canceled){
            jumped = false;
        }
    }

    public void Look(InputAction.CallbackContext ctx){
        look_vector = ctx.ReadValue<Vector2>();
    }
}
