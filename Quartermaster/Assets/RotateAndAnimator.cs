using System.Collections;
using UnityEngine;

public class RotateAndAnimator : MonoBehaviour{

    private Animator animator;


    void Start() {
        animator = GetComponentInChildren<Animator>();
    }

    void Update() {
        if (Input.GetKeyDown("q")) {
            animator.SetFloat("ForwardSpeed", 4);
        }

        if (Input.GetKeyDown("e")) {
            animator.SetFloat("ForwardSpeed", 0);
        }
    }
}
