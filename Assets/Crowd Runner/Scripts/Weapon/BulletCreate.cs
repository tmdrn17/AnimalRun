using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCreate : MonoBehaviour
{
    [SerializeField] GameObject bullet;
    public List<GameObject> bullets = new List<GameObject>();
    [SerializeField] int count;
    [SerializeField] float bulletTime;
    [SerializeField] float bulletSpeed;

    void Start()
    {
        bullet = transform.parent.GetComponent<Weapon>().bulletPrefab;
        count = transform.parent.GetComponent<Weapon>().count;
        bulletTime = transform.parent.GetComponent<Weapon>().bulletTime;

        for (int i = 0; i < count; i++)
        {
            GameObject bulletInstance = Instantiate(bullet, transform.position, transform.rotation);
            bulletInstance.SetActive(false);
            bullets.Add(bulletInstance);
        }

        StartCoroutine(FireBullet());
    }

    IEnumerator FireBullet()
    {
        while (true)
        {
            if (GameManager.instance.gameState != GameManager.GameState.Game)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < bullets.Count; i++)
            {
                if (!(bullets[i].activeSelf))
                {
                    bullets[i].transform.position = transform.position;
                    bullets[i].transform.rotation = transform.rotation;
                    bullets[i].GetComponent<Bullet>().SetTarget(transform);
                    bullets[i].SetActive(true);

                    bullets[i].GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, 1) * bulletSpeed, ForceMode.Impulse);
                }
                
                yield return new WaitForSeconds(bulletTime);
            }
        }
    }
}
