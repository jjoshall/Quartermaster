using UnityEngine;
using UnityEngine.InputSystem;


/*
USE CALLBACKS TO SET VARIABLES, HAVE {GET; PRIVATE SET} FOR DATA VALUES TO BE PASSED TO CONTROLLER WHICH HAS ACTUAL UPDATE()
*/
public class PlayerInputHandler : MonoBehaviour
{
    public Vector3 move_vector { get; private set;}
    public Vector2 look_vector { get; private set;}
    public bool jumped {get; private set;}
    public bool IsSprinting {get; private set;}
    

    void Start(){
        //playerInput = new PlayerControls();
        move_vector = Vector3.zero;
        look_vector = Vector2.zero;
        jumped = false;
        IsSprinting = false;
        // lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Move(InputAction.CallbackContext ctx){
        Vector2 move_xy = ctx.ReadValue<Vector2>();
        move_vector = new Vector3(move_xy.x, 0f, move_xy.y);
        move_vector = Vector3.ClampMagnitude(move_vector,1);
    }

    public void Jump(InputAction.CallbackContext ctx){
        // change to ctx.started to disable continuous jumping via holding button
        if (ctx.performed){
            jumped = true;
        }
        else if (ctx.canceled){
            jumped = false;
        }
    }

    public void Sprint(InputAction.CallbackContext ctx){
        if (ctx.performed){
            IsSprinting = true;
        }
        else if (ctx.canceled){
            IsSprinting = false;
        }
    }

    public void Look(InputAction.CallbackContext ctx){
        look_vector = ctx.ReadValue<Vector2>();
    }

    public float GetHorizontalLook(){
        return look_vector.x;
    }

    public float GetVerticalLook(){
        return look_vector.y;
    }
}
