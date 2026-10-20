using UnityEngine;
using Crowd_Runner.Scripts.Managers;

public class Enemy : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private HealthBar healthBar;

    [Header(" Settings ")]
    [SerializeField] private float maxHP;

    [Header(" State ")]
    [SerializeField] private float currentHP;

    private void Awake()
    {
        currentHP = maxHP;
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
}