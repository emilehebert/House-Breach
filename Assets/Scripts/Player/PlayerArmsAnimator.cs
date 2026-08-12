using UnityEngine;

public class PlayerArmsAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator armsAnimator;

    // 1. Declare the variable here so it can be used in the methods below
    private PlayerController player;

    private void Start()
    {
        // It is safer to find the reference once in Start, rather than every frame
        player = PlayerController.Instance;

        if (armsAnimator == null)
            armsAnimator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Safety check: ensure player exists before trying to read data
        if (player == null)
        {
            player = PlayerController.Instance; // Try to find it again
            if (player == null) return; // If still null, do nothing
        }

        UpdateAnimationStates();
    }

    private void UpdateAnimationStates()
    {
        // 2. Now you can use the 'player' variable safely
        
        // Handle Airing
        armsAnimator.SetBool("Airing", player.isAiring);

        // Handle Grounded
        armsAnimator.SetBool("Grounded", player.isGrounded);

        // Handle Speed`

        if(player.enabled)
            armsAnimator.SetFloat("Speed", player.currentHorizontalSpeed);
        else         
            armsAnimator.SetFloat("Speed", 0f);
    }
}