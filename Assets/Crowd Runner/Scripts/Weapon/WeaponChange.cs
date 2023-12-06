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
    }
}
