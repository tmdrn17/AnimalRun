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

    [SerializeField] GameObject bulletPrefab;
    public GameObject specialBulletPrefab;
    public GameObject[] bulletFireTransform;
    [SerializeField] float bulletSpeed;
    [SerializeField] float specialBulletSpeed;
    [SerializeField] float specialProb = 0.3f;

    [SerializeField] private List<GameObject> bullets = new List<GameObject>();

    void Start()
    {
        for (int i = 0; i < bulletFireTransform.Length; i++)
        {
            GameObject bulletInstance = Instantiate(bulletPrefab, bulletFireTransform[i].transform.position,
                                                    bulletFireTransform[i].transform.rotation);
            bulletInstance.SetActive(false);
            bullets.Add(bulletInstance);
        }

        bulletSpeed = bulletSpeed + PlayerController.instance.moveSpeed;

        StartCoroutine(FireBullet());
    }

    public void SpecialSetting()
    {
        specialProb = specialBulletPrefab.GetComponent<Bullet>().specialProb;

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

    public IEnumerator FireBullet()
    {
        while (true)
        {
            yield return null;

            if(GameManager.instance.gameState != GameManager.GameState.Game)
            {
                yield return null;
                continue;
            }

            float randomValue = Random.value;

            for (int i = 0; i < bullets.Count; i++)
            {
                if (!(bullets[i].activeSelf)) // 비활성화된 총알 인스턴스를 찾음
                {
                    bullets[i].transform.position = bulletFireTransform[i].transform.position;
                    bullets[i].transform.rotation = bulletFireTransform[i].transform.rotation;
                    bullets[i].SetActive(true);
                    bullets[i].GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, 1) * bulletSpeed, ForceMode.Impulse);
                    bullets[i].GetComponent<Bullet>().SetTarget(bulletFireTransform[i].transform);
                }

                if (randomValue <= specialProb && !(specialBulletPrefab == null))
                {
                    GameObject specialBullet = Instantiate(specialBulletPrefab, bulletFireTransform[i].transform.position,
                                                    bulletFireTransform[i].transform.rotation);

                    if (specialBullet.GetComponent<Bullet>().bType == BulletTyp.Grenade || specialBullet.GetComponent<Bullet>().bType == BulletTyp.FireBottle)
                    {
                        specialBullet.GetComponent<Rigidbody>().AddForce(bulletFireTransform[i].transform.forward * specialBulletSpeed, ForceMode.Impulse);
                        specialBullet.GetComponent<Rigidbody>().AddForce(bulletFireTransform[i].transform.up * specialBulletSpeed, ForceMode.Impulse);
                        specialBullet.GetComponent<Bullet>().SetTarget(bulletFireTransform[i].transform);
                    }

                    else
                    {
                        specialBullet.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, 1) * specialBulletSpeed, ForceMode.Impulse);
                        specialBullet.GetComponent<Bullet>().SetTarget(bulletFireTransform[i].transform);
                    }
                }
            }

            yield return new WaitForSeconds(bulletTime);
        }
    }

}
