using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // 플레이어의 Transform을 여기에 할당해주세요.
    public float playerDistance; // 플레이어의 Transform을 여기에 할당해주세요.

    void Update()
    {
        Vector3 newPosition = transform.position;
        newPosition.z = player.position.z - playerDistance; // 카메라의 Z 값을 플레이어의 Z 값에 맞춥니다.
        transform.position = newPosition;
    }
}
