using UnityEngine;

public class PlayerActivator : MonoBehaviour
{

    [SerializeField] private GameObject player;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Activating Player");
            PlayerController playerController = player.GetComponent<PlayerController>();

            playerController.ManualMovementEnable();

        }
    }
}
