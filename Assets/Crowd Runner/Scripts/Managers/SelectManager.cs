using System.Collections.Generic;
using System.Linq;
using Crowd_Runner.Scripts.Data;
using UnityEngine;
using Random = UnityEngine.Random;

public class SelectManager : MonoBehaviour
{
    private DataManager dataManager;
    
    [SerializeField] private Transform runnerPosition;
    [SerializeField] private Transform weaponPosition;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private SelectUI selectUI;
    
    private AnimalDef[] animalDefs;
    private WeaponDef[] weaponDefs;
    
    private void Start()
    {
        dataManager = DataManager.instance;

        animalDefs = new AnimalDef[selectUI.AnimalButtonCount];
        weaponDefs = new WeaponDef[selectUI.WeaponButtonCount];
        
        List<AnimalDef> allAnimalDefs = dataManager.animalTable.All.ToList();
        List<WeaponDef> allWeaponDefs = dataManager.weaponTable.All.ToList();
        
        for (int i = 0; i < selectUI.AnimalButtonCount; i++)
        {
            int randNum = Random.Range(0, allAnimalDefs.Count);
            animalDefs[i] = allAnimalDefs[randNum];
            selectUI.SetAnimalButton(i, animalDefs[i].Sprite, animalDefs[i].Name, animalDefs[i].Name);
            allAnimalDefs.RemoveAt(randNum);
        }
        
        for (int i = 0; i < selectUI.WeaponButtonCount; i++)
        {
            int randNum = Random.Range(0, allWeaponDefs.Count);
            weaponDefs[i] = allWeaponDefs[randNum];
            string weaponDescription =
                $"공격력: {weaponDefs[i].BulletDamage}\n공격 속도: {weaponDefs[i].AttackInterval}\n사거리: {weaponDefs[i].BulletRange}";
            selectUI.SetWeaponButton(i, weaponDefs[i].Sprite, weaponDefs[i].Name, weaponDescription);
            allWeaponDefs.RemoveAt(randNum);
        }
    }

    public void SelectRunner(int index)
    {
        Runner runner = Instantiate(animalDefs[index].Prefab, runnerPosition);
        runner.transform.localPosition = Vector3.zero;
        runner.transform.localRotation = Quaternion.identity;
        weaponPosition = runner.weaponPosition;

        selectUI.ShowWeaponPanel();
    }
    
    public void SelectWeapon(int index)
    {
        Weapon weapon = Instantiate(weaponDefs[index].Prefab, weaponPosition);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.Init(weaponDefs[index], poolContainer);
        
        selectUI.CloseAll();
        
        GameManager.instance.SetGameState(GameManager.GameState.Game);
    }
}
