using UnityEngine;

public class CrouchState : BaseState {
    public CrouchState (PlayerController player) : base(player) {}

    
    public override void OnEnter(){
        //crossfade animator to Crouching
        player.SetSpeedModifier(PlayerController.k_CrouchSpeedModifier);
    }
    
    public override void Update(){
        player.HandleGroundMovement();
    }
}