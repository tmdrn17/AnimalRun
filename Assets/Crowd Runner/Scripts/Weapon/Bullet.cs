using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BulletTyp
{
    GunBullet,
    Missile,
    Arrow,
    Throwing,
    Grenade,
    FireBottle
}

public class Bullet : MonoBehaviour
{
    [SerializeField] GameObject grenadeMesh;
    [SerializeField] GameObject grenadeParticle;

    [SerializeField]
    Vector3 startTransform;
    [SerializeField]
    Vector3 curTransform;

    public int bulletRange = 5;
    public int damage;
    [SerializeField]
    float rotationSpeed;
    int hitCount = 0;
    public float specialProb;

    public BulletTyp bType;

    Rigidbody rb;

    [SerializeField]
    float radius = 10;

    [SerializeField]
    GameObject fireField;

    public Weapon weapon;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetTarget(Transform fireTr)
    {
        startTransform = fireTr.position;
    }

    void Update()
    {
        BulletElement();
        Debug.Log(transform.position.z - startTransform.z);
        if (transform.position.z - startTransform.z >= bulletRange)
        {
            rb.velocity = Vector3.zero;
            gameObject.SetActive(false);
        }
    }

    void BulletElement()
    {
        switch(bType)
        {
            case BulletTyp.Missile:
                transform.Rotate(new Vector3(0, 0, rotationSpeed));
                break;
            case BulletTyp.Arrow:
                transform.Rotate(new Vector3(0, 0, rotationSpeed));
                break;
            case BulletTyp.Grenade:
                transform.Rotate(new Vector3(rotationSpeed, 0, 0));
                break;
            case BulletTyp.Throwing:
                transform.Rotate(new Vector3(rotationSpeed, 0, 0));
                break;
            case BulletTyp.FireBottle:
                transform.Rotate(new Vector3(rotationSpeed, 0, 0));
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (bType == BulletTyp.Missile)
            {
                RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, radius, Vector3.up, 0f, LayerMask.GetMask("Enemy"));

                foreach (RaycastHit hitObjs in rayHits)
                {
                    hitObjs.transform.GetComponent<Enemy>().OnDamage(damage);
                    hitCount++;
                    if (hitCount >= rayHits.Length)
                    {
                        gameObject.SetActive(false);
                        rb.velocity = Vector3.zero;
                        hitCount = 0;
                    }
                }
            }

            else if (bType == BulletTyp.Grenade)
            {
                grenadeMesh.SetActive(false);
                grenadeParticle.SetActive(true);
                rb.velocity = Vector3.zero;
                StartCoroutine(GrenadeDamage());
            }

            else if(bType == BulletTyp.FireBottle)
            {
                Instantiate(fireField, new Vector3(other.transform.position.x, other.transform.position.y + 0.33f, other.transform.position.z), Quaternion.identity);
                rb.velocity = Vector3.zero;
                gameObject.SetActive(false);
                Destroy(gameObject, 2f);
            }

            else
            {
                other.gameObject.GetComponent<Enemy>().OnDamage(damage);
                rb.velocity = Vector3.zero;
                gameObject.SetActive(false);
            }
        }
    }

    IEnumerator GrenadeDamage()
    {
        yield return null;

        RaycastHit[] rayHits = Physics.SphereCastAll(transform.position, radius, Vector3.up, 0f, LayerMask.GetMask("Enemy"));

        foreach (RaycastHit hitObjs in rayHits)
        {
            hitObjs.transform.GetComponent<Enemy>().OnDamage(damage);
        }

        yield return new WaitForSeconds(3f);

        gameObject.SetActive(false);
        grenadeMesh.SetActive(true);
        grenadeParticle.SetActive(false);
        rb.velocity = Vector3.zero;
        Destroy(gameObject, 2f);
    }
}
