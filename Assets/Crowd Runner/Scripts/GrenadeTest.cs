using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeTest : MonoBehaviour
{
    [SerializeField]
    private Transform targetEnemy;
    [SerializeField]
    int damageTest;
    [SerializeField]
    float radius;
    // Start is called before the first frame update
    void Start()
    {

    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif

    // Update is called once per frame
    void Update()
    {
        RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, radius, Vector3.up, 0f, LayerMask.GetMask("Enemy"));
        foreach (RaycastHit hitObjs in rayHits)
        {
            targetEnemy = hitObjs.transform;
        }
    }
}
