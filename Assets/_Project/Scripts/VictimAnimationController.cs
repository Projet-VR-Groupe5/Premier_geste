
using UnityEngine;

public class VictimAnimationController : MonoBehaviour
{
    private Animator animator;

    private bool isFallen = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Fall()
    {
        if (isFallen) return;

        isFallen = true;
        animator.SetTrigger("Tomber");
    }
}
