using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/*
USE CALLBACKS TO SET VARIABLES, HAVE {GET; PRIVATE SET} FOR DATA VALUES TO BE PASSED TO CONTROLLER WHICH HAS ACTUAL UPDATE()
*/
public class PlayerInputHandler : NetworkBehaviour {
    public Vector3 move_vector { get; private set; }
    public Vector2 look_vector { get; private set; }
    public bool jumped { get; private set; }
    public bool isSprinting { get; private set; }
    public bool isCrouching { get; private set; }
    

    void Start() {
        if (!IsOwner) return;

        //playerInput = new PlayerControls();
        move_vector = new Vector3(1f, 0, 1f);
        look_vector = Vector2.zero;
        jumped = false;
        isSprinting = false;

        // lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Move(InputAction.CallbackContext ctx) {
        if (!IsOwner) return;

        Vector2 move_xy = ctx.ReadValue<Vector2>();

        
        move_vector = new Vector3(move_xy.x, 0f, move_xy.y);
        move_vector = Vector3.ClampMagnitude(move_vector,1);

        //Debug.Log("Move Vector: " + move_vector);
    }

    public void Jump(InputAction.CallbackContext ctx) {
        if (!IsOwner) return;

        // change to ctx.started to disable continuous jumping via holding button
        if (ctx.performed) {
            jumped = true;
        }
        else if (ctx.canceled) {
            jumped = false;
        }

        //Debug.Log("Jumped: " + jumped);

    }

    public void Sprint(InputAction.CallbackContext ctx) {
        if (!IsOwner) return;

        if (ctx.performed) {
            isSprinting = true;
        }
        else if (ctx.canceled) {
            isSprinting = false;
        }
    }

    public void Crouch(InputAction.CallbackContext ctx) {
        if(!IsOwner) return;

        if (ctx.performed) {
            isCrouching = true;
        }
        else if (ctx.canceled) {
            isCrouching = false;
        }
    }

    public void Look(InputAction.CallbackContext ctx) {
        if (!IsOwner) return;

        look_vector = ctx.ReadValue<Vector2>();

        //Debug.Log("Look Vector: " + look_vector);
    }

    public float GetHorizontalLook() {
        return look_vector.x;
    }

    public float GetVerticalLook() {
        return look_vector.y;
    }
}
