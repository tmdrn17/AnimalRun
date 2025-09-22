using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WeaponChange : MonoBehaviour
{
    [SerializeField] GameObject weaponParent;
    [SerializeField] GameObject[] specialBullet;

    void Update()
    {
        SelectWeapon();
        SelectSpecialBullet();
    }

    public void SelectWeapon()
    {
        int weaponIndex = PlayerPrefs.GetInt("lastSelectedGun", 0);

        for (int i = 0; i < weaponParent.transform.childCount; i++)
        {
            if (i == weaponIndex)
            {
                weaponParent.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                weaponParent.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }

    public void SelectSpecialBullet()
    {
        // PlayerPrefs에서 인덱스 값을 가져옵니다.
        int weaponIndex = PlayerPrefs.GetInt("lastSelectedSpecial", -1); // 기본값을 -1로 설정하여 유효성 검사 용이하게 함

        // 인덱스가 배열의 범위를 벗어나지 않도록 검사합니다.
        bool isValidIndex = (weaponIndex >= 0 && weaponIndex < specialBullet.Length);

        for (int i = 0; i < weaponParent.transform.childCount; i++)
        {
            // GetComponent를 반복적으로 호출하는 대신 변수에 저장합니다.
            Weapon weaponComponent = weaponParent.transform.GetChild(i).GetComponent<Weapon>();

            if (weaponComponent != null)
            {
                if (isValidIndex)
                {
                    weaponComponent.specialBulletPrefab = specialBullet[weaponIndex];
                    weaponComponent.SpecialSetting();
                }
                else
                {
                    weaponComponent.specialBulletPrefab = null;
                }
            }
        }
    }

    /*   public void SelectSpecialBullet()
       {
           int weaponIndex = PlayerPrefs.GetInt("lastSelectedSpecial");
           for (int i = 0; i < weaponParent.transform.childCount; i++)
           {
               if (weaponIndex >= 0)
               {
                   weaponParent.transform.GetChild(i).GetComponent<Weapon>().specialBulletPrefab = specialBullet[weaponIndex];
                   weaponParent.transform.GetChild(i).GetComponent<Weapon>().SpecialSetting();
               }
               else
               {
                   weaponParent.transform.GetChild(i).GetComponent<Weapon>().specialBulletPrefab = null;
               }
           }
       }*/
}
