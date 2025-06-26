using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.Events;

/*
USE CALLBACKS TO SET VARIABLES, HAVE {GET; PRIVATE SET} FOR DATA VALUES TO BE PASSED TO CONTROLLER WHICH HAS ACTUAL UPDATE()
*/
public class PlayerInputHandler : NetworkBehaviour {
    public Vector3 move_vector { get; private set; } = Vector3.zero;
    public Vector2 look_vector { get; private set; } = Vector2.zero;
    public bool jumped { get; private set; } = false;
    public bool isSprinting { get; private set; } = false;
    public bool isCrouching { get; private set; } = false;
    public int inventoryIndex {get; private set;} = 0;
    public bool isInteracting {get; private set;} = false;
    public bool isDropping {get; private set;} = false;

    // Left Click (use item)
    public bool isUsing { get; private set; } = false;
    public UnityAction<bool> OnUse;
    public UnityAction<bool> OnRelease;

    // Right click (alt use)
    public bool isAltUsing { get; private set; } = false;
    public UnityAction<bool> OnAltUse;
    public UnityAction<bool> OnAltRelease;

    // Pick up / open chest / interact
    public UnityAction OnInteract;


    void Start() {
        if (!IsOwner) return;
    }

    // Update to check for button holding
    void Update()
    {
        if (isUsing)
        {
            OnUse?.Invoke(true);
        }
        if (isAltUsing)
        {
            OnAltUse?.Invoke(true);
        }
    }

    public void Move(InputAction.CallbackContext ctx) {
        if (!IsOwner) return;

        Vector2 move_xy = ctx.ReadValue<Vector2>();

        move_vector = new Vector3(move_xy.x, 0f, move_xy.y);
        move_vector = Vector3.ClampMagnitude(move_vector,1);

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
    }

    public float GetHorizontalLook() {
        return look_vector.x;
    }

    public float GetVerticalLook() {
        return look_vector.y;
    }

    public void Interact(InputAction.CallbackContext ctx){
        if (PauseMenuToggler.IsPaused) return;
        if (!IsOwner) return;
        if (ctx.started){
            OnInteract?.Invoke();
        }
        if (ctx.performed){
            isInteracting = true;
        }
        else if (ctx.canceled){
            isInteracting = false;
        }
    }

    public void DropItem(InputAction.CallbackContext ctx){
        if (PauseMenuToggler.IsPaused) return;
        if (!IsOwner) return;
        if (ctx.performed){
            isDropping = true;
        }
        else if (ctx.canceled){
            isDropping = false;
        }
    }

    public void UseItem(InputAction.CallbackContext ctx){
        if (PauseMenuToggler.IsPaused) return;
        if (!IsOwner) return;
        if (ctx.started){
            OnUse?.Invoke(false);
            isUsing = false;
        }
        else if (ctx.performed){
            isUsing = true;
        }
        else if (ctx.canceled){
            OnRelease?.Invoke(false);
            isUsing = false;
        }
    }

    public void AltUseItem(InputAction.CallbackContext ctx)
    {
        if (PauseMenuToggler.IsPaused) return;
        if (!IsOwner) return;
        if (ctx.started){
            OnAltUse?.Invoke(false);
            isAltUsing = false;
        }
        else if (ctx.performed){
            isAltUsing = true;
        }
        else if (ctx.canceled){
            OnAltRelease?.Invoke(false);
            isAltUsing = false;
        }
    }

    public void ItemSlot1(InputAction.CallbackContext ctx)
    {
        if (PauseMenuToggler.IsPaused) return;
        if (ctx.started)
        {
            inventoryIndex = 0;
        }
    }

    public void ItemSlot2(InputAction.CallbackContext ctx){
        if (PauseMenuToggler.IsPaused) return;
        if (ctx.started){
            inventoryIndex = 1;
        }
    }

    public void ItemSlot3(InputAction.CallbackContext ctx){
        if (PauseMenuToggler.IsPaused) return;
        if (ctx.started){
            inventoryIndex = 2;
        }
    }

    public void ItemSlot4(InputAction.CallbackContext ctx){
        if (PauseMenuToggler.IsPaused) return;
        if (ctx.started){
            inventoryIndex = 3;
        }
    }
    
    public void ScrollDown(InputAction.CallbackContext ctx) {
        if (PauseMenuToggler.IsPaused) return;
        if (ctx.started){
            if (inventoryIndex < 3) {
                inventoryIndex += 1;
            }
            else {
                inventoryIndex = 0;
            }
        }
    }

    public void ScrollUp(InputAction.CallbackContext ctx) {
        if (PauseMenuToggler.IsPaused) return;
        if (ctx.started){
            if (inventoryIndex > 0) {
                inventoryIndex -= 1;
            }
            else {
                inventoryIndex = 3;
            }
        }
    }
}
