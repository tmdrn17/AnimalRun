using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingManager : MonoBehaviour
{
    [SerializeField]
    GameObject runnerParent;

    [SerializeField]
    Transform[] allChildren;
    
    [SerializeField]
    GameObject childWeapon;
    
    [SerializeField]
    int previousChildCount;
    
    [SerializeField]
    int currentChildCount;

    void Start()
    {
        previousChildCount = runnerParent.transform.childCount;
    }
}
