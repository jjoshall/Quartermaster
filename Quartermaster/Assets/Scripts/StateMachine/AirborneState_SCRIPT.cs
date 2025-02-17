public class AirborneState : BaseState {
    public AirborneState (PlayerController player) : base(player) {}

    /*
    public override void OnEnter(){
        crossfade animator to falling if needed
    }
    */
    public override void Update(){
        player.HandleAirMovement();
    }
}