using System.Collections.Generic;
using System.IO;
using FileIO;
using UnityEngine;

namespace Crowd_Runner.Scripts.Data
{
    public class WeaponTable
    {
        private const string TableName = "Table/Animal Run/Weapons.tsv";
        
        private Dictionary<int, WeaponDef> WeaponDefs = new();

        public int Count => WeaponDefs.Count;

        public IReadOnlyCollection<WeaponDef> All => WeaponDefs.Values;
        public WeaponDef Get(int id) => WeaponDefs[id];
        
        public void Load()
        {
            WeaponDefs.Clear();

            string path = Path.Combine(Application.streamingAssetsPath, TableName);
            List<WeaponDTO> rows = TSVReader.ReadTable<WeaponDTO>(path);

            for (int i = 0; i < rows.Count; i++)
            {
                WeaponDTO dto = rows[i];
                Weapon weaponPrefab = Resources.Load<Weapon>(dto.PrefabKey);
                if(weaponPrefab == null) 
                    Debug.LogWarning($"[UnitTable] {dto.PrefabKey} 경로에 파일 없음");
                Bullet bulletPrefab = Resources.Load<Bullet>(dto.BulletPrefabKey);
                if(bulletPrefab == null) 
                    Debug.LogWarning($"[UnitTable] {dto.BulletPrefabKey} 경로에 파일 없음");
                Sprite sprite = Resources.Load<Sprite>(dto.SpriteKey);
                if(sprite == null) 
                    Debug.LogWarning($"[UnitTable] {dto.SpriteKey} 경로에 파일 없음");

                WeaponDef def = new WeaponDef();
                def.ID = dto.ID;
                def.Name = dto.Name;
                def.Prefab = weaponPrefab;
                def.Sprite = sprite;
                def.Kind = dto.Kind;
                def.BulletPrefab = bulletPrefab;

                def.Damage = dto.Damage;
                def.Range = dto.Range;
                def.AttackInterval = dto.AttackInterval;
                def.SplashRadius = dto.SplashRadius;
                
                WeaponDefs.Add(dto.ID, def);
            }
        }
    }
}