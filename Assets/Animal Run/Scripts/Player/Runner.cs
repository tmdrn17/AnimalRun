using Animal_Run.Scripts.Managers;
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
        // 부모에서 바로 떼어내야 AnimalSystem이 같은 프레임에 남은 러너 수를 정확히 센다.
        transform.SetParent(null);
        Destroy(gameObject);
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
