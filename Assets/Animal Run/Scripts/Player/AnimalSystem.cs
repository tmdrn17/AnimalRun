using Animal_Run.Scripts.Data;
using UnityEngine;

public class AnimalSystem : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private PlayerAnimator playerAnimator;
    [SerializeField] private Transform runnersParent;

    [Header(" Settings ")]
    [SerializeField] private float radius;
    [SerializeField] private float angle;
    [Tooltip("러너마다 무기가 달려 있으므로 성능을 위해 최대 수를 제한한다.")]
    [SerializeField] private int maxRunners = 150;

    private AnimalDef animalDef;
    private WeaponDef weaponDef;
    private Transform bulletContainer;

    private void Update()
    {
        if (!GameManager.instance.IsGameState(GameManager.GameState.Game))
            return;

        PlaceRunners();

        if (runnersParent.childCount <= 0)
            GameManager.instance.SetGameState(GameManager.GameState.Gameover);
    }

    private void PlaceRunners()
    {
        for (int i = 0; i < runnersParent.childCount; i++)
        {
            Vector3 childLocalPosition = GetRunnerLocalPosition(i);
            runnersParent.GetChild(i).localPosition = childLocalPosition;
        }
    }

    private Vector3 GetRunnerLocalPosition(int index)
    {
        float x = radius * Mathf.Sqrt(index) * Mathf.Cos(Mathf.Deg2Rad * index * angle);
        float z = radius * Mathf.Sqrt(index) * Mathf.Sin(Mathf.Deg2Rad * index * angle);

        return new Vector3(x, 0, z);
    }

    public float GetAnimalGroupRadius()
    {
        return radius * Mathf.Sqrt(runnersParent.childCount);
    }

    public int GetRunnersCount()
    {
        return runnersParent.childCount;
    }

    /// <summary>
    /// 선택한 동물과 무기를 저장해 두고, 이후 증식하는 러너도 같은 동물·무기로 생성한다.
    /// </summary>
    public void SetRunnerData(AnimalDef animal, WeaponDef weapon, Transform poolContainer)
    {
        animalDef = animal;
        weaponDef = weapon;
        bulletContainer = poolContainer;
    }

    public Runner SpawnRunner()
    {
        Runner runner = Instantiate(animalDef.Prefab, runnersParent);
        runner.transform.localPosition = Vector3.zero;
        runner.transform.localRotation = Quaternion.identity;
        return runner;
    }

    public void EquipWeapon(Runner runner)
    {
        Weapon weapon = Instantiate(weaponDef.Prefab, runner.weaponPosition);
        weapon.transform.localPosition = Vector3.zero;
        // 프리팹에 지정된 회전을 그대로 사용한다. (도끼·식칼은 X축 90도로 머리 위에 누워 있다.)
        weapon.transform.localRotation = weaponDef.Prefab.transform.localRotation;
        weapon.Init(weaponDef, bulletContainer);
    }

    public void ApplyBonus(BonusType bonusType, int bonusAmount)
    {
        int currentCount = runnersParent.childCount;
        int targetCount = currentCount;

        // 문의 수식 결과가 음수일 수도 있다. (예: +(-3) 은 3명 감소, -(-3) 은 3명 증가)
        switch(bonusType)
        {
            case BonusType.Addition:
                targetCount = currentCount + bonusAmount;
                break;

            case BonusType.Difference:
                targetCount = currentCount - bonusAmount;
                break;

            case BonusType.Product:
                targetCount = currentCount * bonusAmount;
                break;

            case BonusType.Division:
                if (bonusAmount != 0)
                    targetCount = currentCount / bonusAmount;
                break;
        }

        ChangeRunnersCount(targetCount);
    }

    public void ChangeRunnersCount(int targetCount)
    {
        targetCount = Mathf.Clamp(targetCount, 0, maxRunners);
        int currentCount = runnersParent.childCount;

        if (targetCount > currentCount)
            AddRunners(targetCount - currentCount);
        else if (targetCount < currentCount)
            RemoveRunners(currentCount - targetCount);
    }

    private void AddRunners(int amount)
    {
        if (animalDef == null || weaponDef == null)
            return;

        for (int i = 0; i < amount; i++)
        {
            Runner runner = SpawnRunner();
            EquipWeapon(runner);
        }
        playerAnimator.Run();
    }

    private void RemoveRunners(int amount)
    {
        if (amount > runnersParent.childCount)
            amount = runnersParent.childCount;

        int runnersAmount = runnersParent.childCount;

        for (int i = runnersAmount - 1; i >= runnersAmount - amount; i--)
        {
            Transform runnerToDestroy = runnersParent.GetChild(i);
            runnerToDestroy.SetParent(null);
            Destroy(runnerToDestroy.gameObject);
        }
    }
}
