using UnityEngine;
using System;
using Crowd_Runner.Scripts.Managers;

public class Enemy : MonoBehaviour
{
    enum State
    {
        Idle,
        Running
    }
    
    [Header(" Settings ")] 
    [SerializeField]
    private float searchRadius;

    [SerializeField] private float moveSpeed;
    [SerializeField] float maxHP;
    [SerializeField] float currentHP;
    [SerializeField] private State state;
    [SerializeField] private Transform targetRunner;
    [SerializeField] HealthBar healthBar;

    [Header(" Events ")] public static Action onRunnerDied;

    void Awake()
    {
        currentHP = maxHP;
    }

    void Start()
    {
        healthBar.UpdateHealthBar(maxHP, currentHP);
    }

    private void OnEnable()
    {
        ListManager.Enemies.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        // ManageState();
        
        healthBar.UpdateHealthBar(maxHP, currentHP);
        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        ListManager.Enemies.Remove(this);
    }

    public void OnDamage(float damage)
    {
        currentHP -= damage;
    }

    /*private void ManageState()
    {
        switch (state)
        {
            case State.Idle:
                SearchForTarget();
                break;

            case State.Running:
                RunTowardsTarget();
                break;
        }
    }

    private void SearchForTarget()
    {
        for (int i = 0; i < ListManager.Runners.Count; i++)
        {
            Transform target = ListManager.Runners[i].transform;
            
            float targetDistance = Vector3.Distance(transform.position, target.position);

            if (targetDistance <= searchRadius)
            {
                targetRunner = target;
                StartRunningTowardsTarget();
                break;
            }
        }
    }

    private void StartRunningTowardsTarget()
    {
        state = State.Running;
        GetComponent<Animator>().Play("Run");
    }

    private void RunTowardsTarget()
    {
        if (targetRunner == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, targetRunner.position,
            Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, targetRunner.position) < .1f)
        {
            onRunnerDied?.Invoke();

            Destroy(targetRunner.gameObject);
            Destroy(gameObject);
        }
    }*/
}