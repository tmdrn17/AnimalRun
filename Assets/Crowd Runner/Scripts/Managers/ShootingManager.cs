using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingManager : MonoBehaviour
{
    [SerializeField] GameObject runnerParent;
    [SerializeField]
    Transform[] allChildren;
    [SerializeField] GameObject childWeapon;
    [SerializeField]
    int previousChildCount;
    [SerializeField]
    int currentChildCount;

    void Start()
    {
        previousChildCount = runnerParent.transform.childCount;
        //Shooting();
    }

    void Update()
    {
        
    }

    public void Shooting()
    {
        currentChildCount = runnerParent.transform.childCount;

        if (currentChildCount >= previousChildCount)
        {
            for (int i = previousChildCount; i <= currentChildCount; i++)
            {
                childWeapon = runnerParent.transform.GetChild(i - 1).gameObject;
                allChildren = childWeapon.GetComponentsInChildren<Transform>();
                Debug.Log(i);
                foreach (Transform child in allChildren)
                {
                    // 자기 자신의 경우엔 무시 
                    // (게임오브젝트명이 다 다르다고 가정했을 때 통하는 코드)
                    if (child.name == transform.name)
                        return;

                    if (child.name == "Weapon")
                    {
                        for (int j = 0; j < child.childCount; j++)
                        {
                            Transform weaponIndex = child.GetChild(j);

                            if ((weaponIndex.gameObject.activeSelf) == true)
                            {
                                Debug.Log(child.parent.parent.parent.parent.parent.name);
                                StartCoroutine(weaponIndex.gameObject.GetComponent<Weapon>().FireBullet());
                                Debug.Log(j);
                            }
                        }
                    }
                }
            }

            previousChildCount = currentChildCount; // 이전 자식 오브젝트 수를 갱신합니다.
        }
    }




}
