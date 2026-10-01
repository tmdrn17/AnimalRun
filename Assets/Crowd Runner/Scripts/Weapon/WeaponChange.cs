using UnityEngine;

public class WeaponChange : MonoBehaviour
{
    [SerializeField] GameObject weaponParent;

    private void Update()
    {
        SelectWeapon();
    }

    private void SelectWeapon()
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
}