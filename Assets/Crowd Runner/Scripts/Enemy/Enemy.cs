using UnityEngine;
using Crowd_Runner.Scripts.Managers;

public class Enemy : MonoBehaviour
{
    private static readonly int RunState = Animator.StringToHash("Run");

    [Header(" Elements ")]
    [SerializeField] private HealthBar healthBar;

    [Header(" Settings ")]
    [SerializeField] private float maxHP;
    [SerializeField] private float searchRadius = 5;
    [SerializeField] private float moveSpeed = 10;

    [Header(" State ")]
    [SerializeField] private float currentHP;

    private Animator animator;
    private Runner targetRunner;
    private bool isDead;

    public bool IsDead => isDead;

    private void Awake()
    {
        currentHP = maxHP;
        animator = GetComponent<Animator>();

        // 러너(Rigidbody)와 총알이 OnTriggerEnter로 적을 감지할 수 있도록 트리거로 사용한다.
        foreach (Collider enemyCollider in GetComponentsInChildren<Collider>())
            enemyCollider.isTrigger = true;
    }

    private void Start()
    {
        healthBar.UpdateHealthBar(maxHP, currentHP);
    }

    private void OnEnable()
    {
        ListManager.Enemies.Add(this);
    }

    // Update is called once per frame
    private void Update()
    {
        if (!GameManager.instance.IsGameState(GameManager.GameState.Game))
            return;

        ChaseRunner();
    }

    private void OnDisable()
    {
        ListManager.Enemies.Remove(this);
    }

    private void ChaseRunner()
    {
        if (targetRunner == null)
        {
            targetRunner = FindRunnerInRange();
            if (targetRunner == null)
                return;

            if (animator != null && animator.HasState(0, RunState))
                animator.Play(RunState);
        }

        Vector3 targetPosition = targetRunner.transform.position;
        targetPosition.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);
    }

    private Runner FindRunnerInRange()
    {
        Runner closestRunner = null;
        float closestDistance = searchRadius;

        foreach (Runner runner in ListManager.Runners)
        {
            float distance = Vector3.Distance(transform.position, runner.transform.position);
            if (distance > closestDistance)
                continue;

            closestDistance = distance;
            closestRunner = runner;
        }

        return closestRunner;
    }

    public void OnDamage(float damage)
    {
        if (isDead)
            return;

        currentHP -= damage;
        healthBar.UpdateHealthBar(maxHP, currentHP);

        if (currentHP <= 0)
            Die();
    }

    /// <summary>
    /// 러너와 부딪혔을 때 호출. 이미 다른 러너와 부딪혀 죽은 적이면 false를 반환한다.
    /// </summary>
    public bool TryKillByRunner()
    {
        if (isDead)
            return false;

        Die();
        return true;
    }

    private void Die()
    {
        isDead = true;
        Destroy(gameObject);
    }
}
