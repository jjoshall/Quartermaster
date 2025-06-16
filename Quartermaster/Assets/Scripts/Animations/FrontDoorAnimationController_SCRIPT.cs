using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System.Threading.Tasks;

public class FrontDoorAnimationController : NetworkBehaviour {
    private Animator animator;
    private bool isPaused = false;
    private float savedSpeed = 1f;

    private bool gatesOpened = false;
    [SerializeField] private GameObject doorText;

    // Public UnityEvents for triggering animation controls externally
    public UnityEvent OnPlay = new UnityEvent();

    private void Awake() {
        animator = GetComponent<Animator>();
        if (animator != null) {
            animator.enabled = false; // Disable animator initially
        }

        // Bind functions to events
        OnPlay.AddListener(PlayAnimation);

    }

    public async void PlayAnimation() {
        await Task.Delay(5000); // wait 5 seconds before closing door
        if (animator != null) {
            animator.enabled = true; // Enable animator
            animator.speed = 1f;
            isPaused = false;
        }

        if (doorText != null) {
            doorText.SetActive(false); // Disable the door text
        }
    }

    // External scripts can call these to trigger the events dynamically
    public void TriggerPlay() => OnPlay.Invoke();
}
