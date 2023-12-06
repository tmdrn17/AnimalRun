using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum WeaponVariation
{
    Rifle
}

public class Weapon : MonoBehaviour
{
    public float bulletTime;

    public GameObject bulletPrefab;
    public GameObject specialBulletPrefab;
    public GameObject[] bulletFireTransform;
    public float bulletSpeed;
    [SerializeField] float specialBulletSpeed;
    [SerializeField] float specialProb = 0.3f;
    [SerializeField] int damage;
    [SerializeField] int range;
    public List<GameObject> bullets = new List<GameObject>();
    public int count;

    void Start()
    {
        bulletSpeed = bulletSpeed + PlayerController.instance.moveSpeed;
        bullets = transform.Find("BulletFireTransform").gameObject.GetComponent<BulletCreate>().bullets;

        StartCoroutine(FireBullet());

        bulletPrefab.GetComponent<Bullet>().damage = damage;
        bulletPrefab.GetComponent<Bullet>().bulletRange = range;
    }

    public void SpecialSetting()
    {
        //specialProb = specialBulletPrefab.GetComponent<Bullet>().specialProb;

        if(specialBulletPrefab != null)
        {
            switch (specialBulletPrefab.GetComponent<Bullet>().bType)
            {
                case BulletTyp.Grenade:
                    specialBulletSpeed = 4;
                    break;
                case BulletTyp.Throwing:
                    specialBulletSpeed = bulletSpeed - 1;
                    break;
            }
        }
    }

    IEnumerator FireBullet()
    {
        while (true)
        {
            if(GameManager.instance.gameState != GameManager.GameState.Game)
            {
                yield return null;
                continue;
            }

            float randomValue = Random.value;

            if (randomValue <= specialProb && !(specialBulletPrefab == null))
            {
                GameObject specialBullet = Instantiate(specialBulletPrefab, bulletFireTransform[0].transform.position,
                                                bulletFireTransform[0].transform.rotation);

                if (specialBullet.GetComponent<Bullet>().bType == BulletTyp.Grenade || specialBullet.GetComponent<Bullet>().bType == BulletTyp.FireBottle)
                {
                    specialBullet.GetComponent<Rigidbody>().AddForce(bulletFireTransform[0].transform.forward * specialBulletSpeed, ForceMode.Impulse);
                    specialBullet.GetComponent<Rigidbody>().AddForce(bulletFireTransform[0].transform.up * specialBulletSpeed, ForceMode.Impulse);
                    specialBullet.GetComponent<Bullet>().SetTarget(bulletFireTransform[0].transform);
                }

                else
                {
                    specialBullet.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, 1) * specialBulletSpeed, ForceMode.Impulse);
                    specialBullet.GetComponent<Bullet>().SetTarget(bulletFireTransform[0].transform);
                }
            }

            yield return new WaitForSeconds(bulletTime);
        }
    }

}
