using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float specialProb = 0.3f;
    [SerializeField] private int damage;
    [SerializeField] private int range;
    
    public float bulletTime;
    public int count;
    
    public List<GameObject> bullets = new();
    public GameObject bulletPrefab;
    public GameObject[] bulletFireTransform;

    private void Start()
    {
        bulletSpeed = bulletSpeed + PlayerController.instance.moveSpeed;
        bullets = transform.Find("BulletFireTransform").gameObject.GetComponent<BulletCreate>().bullets;

        bulletPrefab.GetComponent<Bullet>().damage = damage;
        bulletPrefab.GetComponent<Bullet>().bulletRange = range;
    }
}
