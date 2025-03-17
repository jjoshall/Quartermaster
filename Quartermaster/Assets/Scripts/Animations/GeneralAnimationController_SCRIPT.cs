using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class GeneralAnimationController : NetworkBehaviour
{
    private Animator animator;
    private bool isPaused = false;
    private float savedSpeed = 1f;

    private bool gatesOpened = false;

    // Public UnityEvents for triggering animation controls externally
    public UnityEvent OnPlay = new UnityEvent();
    public UnityEvent OnPause = new UnityEvent();
    public UnityEvent OnHold = new UnityEvent();
    public UnityEvent OnResume = new UnityEvent();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false; // Disable animator initially
        }

        // Bind functions to events
        OnPlay.AddListener(PlayAnimation);
        OnPause.AddListener(PauseAnimation);
        OnHold.AddListener(HoldAnimation);
        OnResume.AddListener(ResumeAnimation);
    }

    private void Update()
    {
        // Temporary test trigger to start animation when pressing '['
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            TriggerPlay();


            // trigger poolmanager spawning start.
            EnableSpawnIfGatesNotOpenedYet();

        }
    }

    private void EnableSpawnIfGatesNotOpenedYet(){ // 
        if(!gatesOpened){
            // trigger poolmanager spawning start.
            gatesOpened = true;
        }
    }

    public void PlayAnimation()
    {
        if (animator != null)
        {
            animator.enabled = true; // Enable animator
            animator.speed = 1f;
            isPaused = false;
        }
    }

    public void PauseAnimation()
    {
        if (animator != null && !isPaused)
        {
            savedSpeed = animator.speed;
            animator.speed = 0f;
            isPaused = true;
        }
    }

    public void HoldAnimation()
    {
        if (animator != null)
        {
            animator.speed = 0f; // Completely halt animation
        }
    }

    public void ResumeAnimation()
    {
        if (animator != null)
        {
            animator.speed = savedSpeed;
            isPaused = false;
        }
    }

    // External scripts can call these to trigger the events dynamically
    public void TriggerPlay() => OnPlay.Invoke();
    public void TriggerPause() => OnPause.Invoke();
    public void TriggerHold() => OnHold.Invoke();
    public void TriggerResume() => OnResume.Invoke();
}
