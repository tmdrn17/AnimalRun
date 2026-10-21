using System.Collections.Generic;
using System.Linq;
using Animal_Run.Scripts.Data;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SelectManager : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private SelectUI selectUI;
    [FormerlySerializedAs("crowdSystem")]
    [SerializeField] private AnimalSystem animalSystem;

    [Header(" Positions ")]
    [SerializeField] private Transform runnerPosition;
    [SerializeField] private Transform poolContainer;

    private DataManager dataManager;

    private AnimalDef[] animalDefs;
    private WeaponDef[] weaponDefs;
    private AugmentDef[] augmentDefs;

    private AnimalDef selectedAnimal;
    private WeaponDef selectedWeapon;
    private Runner firstRunner;

    // 한 번에 여러 레벨이 오르면 그 횟수만큼 연달아 증강을 고른다.
    private int pendingAugments;

    private void Awake()
    {
        AugmentStats.Reset();

        if (animalSystem == null)
            animalSystem = runnerPosition.GetComponentInParent<AnimalSystem>();
    }

    private void Start()
    {
        dataManager = DataManager.instance;
        DataManager.onLevelUp += LevelUpCallback;

        animalDefs = new AnimalDef[selectUI.AnimalButtonCount];
        weaponDefs = new WeaponDef[selectUI.WeaponButtonCount];
        augmentDefs = new AugmentDef[selectUI.AugmentButtonCount];

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

    private void OnDestroy()
    {
        DataManager.onLevelUp -= LevelUpCallback;

        // 증강 선택 중에 씬을 벗어나도 시간이 멈춘 채로 남지 않게 한다.
        if (pendingAugments > 0)
            Time.timeScale = 1.0f;
    }

    public void SelectRunner(int index)
    {
        selectedAnimal = animalDefs[index];
        animalSystem.SetRunnerData(selectedAnimal, null, poolContainer);

        // 무기 선택 화면에서 고른 동물을 미리 보여준다.
        firstRunner = animalSystem.SpawnRunner();

        selectUI.ShowWeaponPanel();
    }

    public void SelectWeapon(int index)
    {
        selectedWeapon = weaponDefs[index];
        animalSystem.SetRunnerData(selectedAnimal, selectedWeapon, poolContainer);
        animalSystem.EquipWeapon(firstRunner);

        selectUI.CloseAll();

        GameManager.instance.SetGameState(GameManager.GameState.Game);
    }

    private void LevelUpCallback()
    {
        if (!GameManager.instance.IsGameState(GameManager.GameState.Game))
            return;

        pendingAugments++;

        if (pendingAugments == 1)
            ShowAugments();
    }

    private void ShowAugments()
    {
        Time.timeScale = 0.0f;

        List<AugmentDef> allAugmentDefs = AugmentDef.All.Where(augment => augment.IsAvailableFor(selectedWeapon)).ToList();

        for (int i = 0; i < selectUI.AugmentButtonCount; i++)
        {
            int randNum = Random.Range(0, allAugmentDefs.Count);
            augmentDefs[i] = allAugmentDefs[randNum];
            Sprite sprite = augmentDefs[i].IsRunnerAugment ? selectedAnimal.Sprite : selectedWeapon.Sprite;
            selectUI.SetAugmentButton(i, sprite, augmentDefs[i].Name, augmentDefs[i].Description);
            allAugmentDefs.RemoveAt(randNum);
        }

        selectUI.ShowAugmentPanel();
    }

    public void SelectAugment(int index)
    {
        if (pendingAugments <= 0)
            return;

        ApplyAugment(augmentDefs[index]);
        pendingAugments--;

        if (pendingAugments > 0)
        {
            ShowAugments();
            return;
        }

        selectUI.CloseAugmentPanel();
        Time.timeScale = 1.0f;
    }

    private void ApplyAugment(AugmentDef augment)
    {
        int runnersCount = animalSystem.GetRunnersCount();

        switch (augment.Type)
        {
            case AugmentType.AddRunners:
                animalSystem.ChangeRunnersCount(runnersCount + (int)augment.Value);
                break;

            case AugmentType.MultiplyRunners:
                int runnersToAdd = Mathf.Max(1, Mathf.RoundToInt(runnersCount * augment.Value));
                animalSystem.ChangeRunnersCount(runnersCount + runnersToAdd);
                break;

            default:
                AugmentStats.Apply(augment);
                break;
        }
    }
}
