using UnityEngine;

public class Runner : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Animator animator;
    public Transform weaponPosition;

    private bool isDead;

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
        if (isDead)
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || !enemy.TryKillByRunner())
            return;

        isDead = true;
        gameObject.SetActive(false);
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
