using UnityEngine;

public class SlideState : BaseState {
    public SlideState (PlayerController player) : base(player) {}

    public override void OnEnter(){
        //crossfade animator to sliding
        player.ScalePlayerVelocity(2f);
    }
    
    public override void Update(){
        player.HandleSlideMovement();
    }
}