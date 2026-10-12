using Crowd_Runner.Scripts.Data;
using UnityEngine;
using UnityEngine.Serialization;

public class ButtonManager : MonoBehaviour
{
    private WeaponTable weaponTable;
    private AnimalTable animalTable;

    private Transform weaponPosition;
    
    [SerializeField] private Transform position;
    
    private void Awake()
    {
        weaponTable = new WeaponTable();
        animalTable = new AnimalTable();
        
        weaponTable.Load();
        animalTable.Load();
    }
    
    public void SelectRunner()
    {
        AnimalDef animalDef = animalTable.Get(Random.Range(0, animalTable.Count));
        
        Runner runner = Instantiate(animalDef.Prefab, position);
        runner.transform.localPosition = Vector3.zero;
        runner.transform.localRotation = Quaternion.identity;
        weaponPosition = runner.weaponPosition;
     
        Debug.Log(runner.name);
    }
    
    public void SelectWeapon()
    {
        WeaponDef weaponDef = weaponTable.Get(Random.Range(0, weaponTable.Count));
        
        Weapon weapon = Instantiate(weaponDef.Prefab, weaponPosition);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
     
        Debug.Log(weapon.name);
    }
}
