namespace Animal_Run.Scripts.Data
{
    public enum AugmentType
    {
        Damage,
        AttackSpeed,
        Range,
        BulletSpeed,
        Splash,
        AddRunners,
        MultiplyRunners
    }

    public class AugmentDTO
    {
        public int ID;
        public string Name;
        public string Description;
        public AugmentType Type;
        public float Value;
    }

    [System.Serializable]
    public class AugmentDef
    {
        public int ID;
        public string Name;
        public string Description;
        public AugmentType Type;
        public float Value;

        // 무기 계열 증강은 무기 이미지를, 러너 계열 증강은 동물 이미지를 버튼에 사용한다.
        public bool IsRunnerAugment => Type == AugmentType.AddRunners || Type == AugmentType.MultiplyRunners;

        /// <summary>
        /// 선택한 무기에서 의미가 있는 증강인지. 폭발 범위 증강은 폭발하는 무기(Rocket, Grenade)에만 등장한다.
        /// </summary>
        public bool IsAvailableFor(WeaponDef weapon)
        {
            if (Type == AugmentType.Splash)
                return weapon.Kind == WeaponKind.Rocket || weapon.Kind == WeaponKind.Grenade;

            return true;
        }
    }
}
