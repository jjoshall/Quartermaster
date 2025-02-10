public class WalkState : BaseState {
    public WalkState (PlayerController player) : base(player) {}

    /*
    public override void OnEnter(){
        crossfade animator to walking
    }
    */
    public override void Update(){
        player.HandleGroundMovement();
    }
}