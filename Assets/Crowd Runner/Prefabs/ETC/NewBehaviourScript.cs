using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] Rigidbody rg;

    void Start()
    {
        rg.AddForce(new Vector3(-speed, 0, 0), ForceMode.Impulse);
    }

    void Update()
    {

    }
}
