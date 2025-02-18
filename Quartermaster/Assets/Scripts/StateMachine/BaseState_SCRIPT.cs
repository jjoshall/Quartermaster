public abstract class BaseState: IState {
    protected readonly PlayerController player;
    // protected readonly Animator animator
    // protected const float crossFadeDuration = 0.1f;
    // TODO: implement animator code

    protected BaseState(PlayerController player) {
        this.player = player;
    }
    
    public virtual void OnEnter() {
        // filled in by inheriting classes
    }

    public virtual void Update() {
        // filled in by inheriting classes
    }

    public virtual void FixedUpdate() {
        // filled in by inheriting classes
    }

    public virtual void OnExit() {
        // filled in by inheriting classes
    }
}