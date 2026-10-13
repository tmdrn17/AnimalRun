using System;
using Crowd_Runner.Scripts.Data;
using Crowd_Runner.Scripts.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

public class SelectManager : MonoBehaviour
{
    private DataManager dataManager;
    
    [SerializeField] private Transform runnerPosition;
    [SerializeField] private Transform weaponPosition;
    [SerializeField] private SelectUI selectUI;
    
    private void Start()
    {
        dataManager = DataManager.instance;
    }

    private void OnEnable()
    {
        selectUI.OnAnimalSelectRequested += SelectRunner;
        selectUI.OnWeaponSelectRequested += SelectWeapon;
    }

    private void OnDisable()
    {
        selectUI.OnAnimalSelectRequested -= SelectRunner;
        selectUI.OnWeaponSelectRequested -= SelectWeapon;
    }

    public void SelectRunner()
    {
        AnimalDef animalDef = dataManager.animalTable.Get(Random.Range(0, dataManager.animalTable.Count));
        
        Runner runner = Instantiate(animalDef.Prefab, runnerPosition);
        runner.transform.localPosition = Vector3.zero;
        runner.transform.localRotation = Quaternion.identity;
        weaponPosition = runner.weaponPosition;

        selectUI.ShowWeaponPanel();
    }
    
    public void SelectWeapon()
    {
        WeaponDef weaponDef = dataManager.weaponTable.Get(Random.Range(0, dataManager.weaponTable.Count));
        
        Weapon weapon = Instantiate(weaponDef.Prefab, weaponPosition);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        
        selectUI.CloseAll();
        
        Debug.Log(ListManager.Runners[0].gameObject.name);
        GameManager.instance.SetGameState(GameManager.GameState.Game);
    }
}
