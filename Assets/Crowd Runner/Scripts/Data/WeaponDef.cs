using UnityEngine;

namespace Crowd_Runner.Scripts.Data
{
    public enum WeaponKind
    {
        Throwing,
        Gun,
        Rocket,
        Grenade
    }
        
    public class WeaponDTO
    {
        public int ID;
        public string Name;
        public string PrefabKey;
        public string SpriteKey;
        public WeaponKind Kind;
        public int Damage;
        public float Range;
        public float AttackInterval;
        public float SplashRadius;
    }
    
    [System.Serializable]
    public class WeaponDef
    {
        public int ID;
        public string Name;
        public Weapon Prefab;
        public Sprite Sprite;
        public WeaponKind Kind;
        
        public int Damage;
        public float Range;
        public float AttackInterval;
        public float SplashRadius;
    }
}