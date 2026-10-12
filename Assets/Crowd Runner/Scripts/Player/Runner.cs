using System;
using System.Collections;
using System.Collections.Generic;
using Crowd_Runner.Scripts.Managers;
using UnityEngine;

public class Runner : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] Animator animator;
    [SerializeField] RunnerSelector runnerSelector;

    public Transform weaponPosition;

    [Header(" Settings ")]
    private bool isTarget;

    void Awake()
    {
        weaponPosition = transform.Find("Weapon Position");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            int runnerIndex = PlayerPrefs.GetInt("lastSelectedSkin", 0);

            if (i == runnerIndex)
            {
                runnerSelector.SelectRunner(i);
                SetAnimator(transform.GetChild(i).GetComponent<Animator>());
            }
        }
    }

    private void OnEnable()
    {
        ListManager.Runners.Add(this);
    }

    private void OnDisable()
    {
        ListManager.Runners.Remove(this);
    }

    void OnTriggerEnter(Collider other)
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
