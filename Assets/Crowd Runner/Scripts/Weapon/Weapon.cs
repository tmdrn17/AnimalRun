using System.Collections;
using UnityEngine;
using Crowd_Runner.Scripts.Data;
using UnityEngine.Pool;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Transform firePosition;
    
    private Bullet bulletPrefab;
    private int bulletDamage;
    private float bulletRange;
    private float bulletSpeed;
    private float bulletSplashRadius;
    private float attackInterval;
    
    private  ObjectPool<Bullet> bulletPool;
    private Transform bulletContainer;

    public void Init(WeaponDef myDef, Transform poolContainer)
    {
        bulletPrefab = myDef.BulletPrefab;
        bulletDamage = myDef.BulletDamage;
        bulletRange = myDef.BulletRange;
        bulletSpeed = myDef.BulletSpeed;
        bulletSplashRadius = myDef.BulletSplashRadius;
        attackInterval = myDef.AttackInterval;
        bulletContainer = poolContainer;
        
        InitializePool();
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        WaitForSeconds delay = new WaitForSeconds(attackInterval);

        while (true)
        {
            Fire();
            yield return delay;
        }
    }
    
    private void InitializePool()
    {
        bulletPool = new ObjectPool<Bullet>(
            createFunc: () => {
                // 1. 풀에 여유가 없을 때 새로 생성
                Bullet bullet = Instantiate(bulletPrefab, bulletContainer);
                // 총알에게 자신이 속할 풀의 정보를 넘겨주어, 스스로 풀에 반납할 수 있게 합니다.
                bullet.SetPool(bulletPool);
                return bullet;
            },
            actionOnGet: (bullet) => {
                // 2. 풀에서 꺼낼 때
                bullet.gameObject.SetActive(true);
            },
            actionOnRelease: (bullet) => {
                // 3. 풀에 반납할 때
                bullet.gameObject.SetActive(false);
            },
            actionOnDestroy: (bullet) => {
                // 4. 풀이 터지거나 파괴될 때
                Destroy(bullet.gameObject);
            },
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public void Fire()
    {
        Vector3 spawnPos = firePosition.position;
        Quaternion spawnRot = Quaternion.identity;
        
        Bullet bulletObj = bulletPool.Get();
        
        bulletObj.transform.position = spawnPos;
        bulletObj.transform.rotation = spawnRot;
        
        float finalBulletSpeed = bulletSpeed + PlayerController.instance.moveSpeed;
        
        bulletObj.Init(bulletDamage, bulletRange, finalBulletSpeed);
    }
}
