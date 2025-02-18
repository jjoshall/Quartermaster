public class WalkState : BaseState {
    public WalkState (PlayerController player) : base(player) {}

    
    public override void OnEnter() {
        //crossfade animator to walking
        player.SetSpeedModifier(1f);
    }
    
    public override void Update() {
        player.HandleGroundMovement();
    }
}