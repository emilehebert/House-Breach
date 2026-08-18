using UnityEngine;

public class ArmAnimatorTrigger : MonoBehaviour
{
    public Animator animator;

    public void SetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }
}
