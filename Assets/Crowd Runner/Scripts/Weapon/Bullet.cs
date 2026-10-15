using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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
{/*
    [SerializeField] GameObject grenadeMesh;
    [SerializeField] GameObject grenadeParticle;

    public Vector3 startTransform;
    [SerializeField]
    Vector3 curTransform;
    
    [SerializeField]
    float rotationSpeed;
    int hitCount = 0;
    public float specialProb;

    public BulletTyp bType;
    
    [SerializeField]
    float radius = 10;

    [SerializeField]
    GameObject fireField;

    public Weapon weapon;*/
    
    private Rigidbody rb;
    
    private IObjectPool<Bullet> myPool;
    public float speed;
    private float range;
    private int damage;
    private Vector3 startPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        /*BulletElement();
        Debug.Log(transform.position.z - startTransform.z);*/
        
        if (Vector3.Distance(startPosition, transform.position) >= range)
        {
            rb.velocity = Vector3.zero;
            myPool.Release(this);
        }
    }
    private void FixedUpdate()
    {
        rb.velocity = transform.forward * speed;
    }
    
    public void SetPool(IObjectPool<Bullet> pool)
    {
        myPool = pool;
    }
    
    public void Init(int damage, float range, float speed)
    {
        this.damage = damage;
        this.range = range;
        this.speed = speed;
        startPosition = transform.position;
    }
    
    /*public void SetTarget(Transform fireTr)
    {
        startTransform = fireTr.position;
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
    }*/
}
