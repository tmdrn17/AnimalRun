using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireFieldDamage : MonoBehaviour
{
    [SerializeField] float damage;
    [SerializeField] float timeOverDamage;
    [SerializeField] float limitTime;
    [SerializeField] float curTime;

    void Start()
    {
        
    }

    void Update()
    {
        curTime += Time.deltaTime;

        if (curTime > limitTime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            timeOverDamage += Time.deltaTime;
            if(timeOverDamage >= 1)
            {
                other.gameObject.GetComponent<Enemy>().OnDamage(damage);
                timeOverDamage = 0;
            }
        }
    }
}
