using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastEnemy : MonoBehaviour
{
    [SerializeField]
    float rayLength = 10f;
    public Transform targetEnemy;
    void Start()
    {
        
    }

    void Update()
    {
        Aming();
    }

    void Aming()
    {
        Ray ray = new Ray(transform.position, transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayLength))
        {
            targetEnemy = hit.collider.gameObject.transform;
        }
        else
        {
            targetEnemy = null;
        }
    }
}
