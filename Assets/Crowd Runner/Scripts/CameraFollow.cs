using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float playerDistance; 

    void Update()
    {
        Vector3 newPosition = transform.position;
        newPosition.z = player.position.z - playerDistance; 
        transform.position = newPosition;
    }
}
