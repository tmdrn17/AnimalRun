using Crowd_Runner.Scripts.Managers;
using UnityEngine;

public class Runner : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Animator animator;
    public Transform weaponPosition;

    [Header(" Settings ")]
    private bool isTarget;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        ListManager.Runners.Add(this);
    }

    private void OnDisable()
    {
        ListManager.Runners.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }

    public void SetTarget()
    {
        isTarget = true;
    }

    public bool IsTarget()
    {
        return isTarget;
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    public void SetAnimator(Animator animator)
    {
        this.animator = animator;
    }
}
