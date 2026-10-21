using System.Collections.Generic;

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

    public class AugmentDef
    {
        public AugmentType Type;
        public string Name;
        public string Description;
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

        public static readonly IReadOnlyList<AugmentDef> All = new List<AugmentDef>
        {
            new AugmentDef { Type = AugmentType.Damage, Name = "공격력 강화", Description = "공격력 +20%", Value = 0.2f },
            new AugmentDef { Type = AugmentType.AttackSpeed, Name = "연사 강화", Description = "공격 속도 +15%", Value = 0.15f },
            new AugmentDef { Type = AugmentType.Range, Name = "사거리 강화", Description = "사거리 +20%", Value = 0.2f },
            new AugmentDef { Type = AugmentType.BulletSpeed, Name = "탄속 강화", Description = "탄속 +25%", Value = 0.25f },
            new AugmentDef { Type = AugmentType.Splash, Name = "폭발 강화", Description = "폭발 범위 +0.5", Value = 0.5f },
            new AugmentDef { Type = AugmentType.AddRunners, Name = "무리 증원", Description = "러너 +3", Value = 3 },
            new AugmentDef { Type = AugmentType.MultiplyRunners, Name = "무리 번식", Description = "러너 +50%", Value = 0.5f },
        };
    }

    /// <summary>
    /// 이번 판에 획득한 증강 효과의 누적값. 씬을 다시 불러오면 SelectManager가 초기화한다.
    /// </summary>
    public static class AugmentStats
    {
        public static float DamageMultiplier { get; private set; } = 1f;
        public static float AttackSpeedMultiplier { get; private set; } = 1f;
        public static float RangeMultiplier { get; private set; } = 1f;
        public static float BulletSpeedMultiplier { get; private set; } = 1f;
        public static float BonusSplashRadius { get; private set; }

        public static void Reset()
        {
            DamageMultiplier = 1f;
            AttackSpeedMultiplier = 1f;
            RangeMultiplier = 1f;
            BulletSpeedMultiplier = 1f;
            BonusSplashRadius = 0f;
        }

        public static void Apply(AugmentDef augment)
        {
            switch (augment.Type)
            {
                case AugmentType.Damage:
                    DamageMultiplier += augment.Value;
                    break;

                case AugmentType.AttackSpeed:
                    AttackSpeedMultiplier += augment.Value;
                    break;

                case AugmentType.Range:
                    RangeMultiplier += augment.Value;
                    break;

                case AugmentType.BulletSpeed:
                    BulletSpeedMultiplier += augment.Value;
                    break;

                case AugmentType.Splash:
                    BonusSplashRadius += augment.Value;
                    break;
            }
        }
    }
}
