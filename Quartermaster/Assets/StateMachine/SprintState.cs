using Unity.Services.Lobbies.Models;
using UnityEngine;

public class SprintState : BaseState {
    public SprintState (PlayerController player) : base(player) {}

    
    public override void OnEnter(){
        //crossfade animator to Sprinting
        player.SetSpeedModifier(PlayerController.k_SprintSpeedModifier);
    }
    
    public override void Update(){
        player.HandleGroundMovement();
    }
}