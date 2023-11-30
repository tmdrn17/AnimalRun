using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    enum State { Idle, Running }


    [Header(" Settings ")]
    [SerializeField] private float searchRadius;
    [SerializeField] private float moveSpeed;
    [SerializeField] float maxHP;
    [SerializeField] float currentHP;
    [SerializeField] private State state;
    [SerializeField]
    private Transform targetRunner;
    [SerializeField] HealthBar healthBar;

    [Header(" Events ")]
    public static Action onRunnerDied;

    void Awake()
    {
        currentHP = maxHP;
    }

    void Start()
    {
        healthBar.UpdateHealthBar(maxHP, currentHP);
    }

    // Update is called once per frame
    void Update()
    {
        ManageState();
        healthBar.UpdateHealthBar(maxHP, currentHP);
        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void ManageState()
    {
        switch(state)
        {
            case State.Idle:
                //SearchForTarget();
            break;

            case State.Running:
                RunTowardsTarget();
            break;
        }
    }

    public void OnDamage(float damage)
    {
        currentHP -= damage;
    }

    /*    private void SearchForTarget()
        {
            Collider[] detectedColliders = Physics.OverlapSphere(transform.position, searchRadius);

            for (int i = 0; i < detectedColliders.Length; i++)
            {
                if(detectedColliders[i].TryGetComponent(out Runner runner))
                {
                    if (runner.IsTarget())
                        continue;

                    runner.SetTarget();
                    targetRunner = runner.transform;

                    StartRunningTowardsTarget();
                }
            }
        }*/

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

        if(Vector3.Distance(transform.position, targetRunner.position) < .1f)
        {
            onRunnerDied?.Invoke();
            
            Destroy(targetRunner.gameObject);
            Destroy(gameObject);
        }
    }
}
