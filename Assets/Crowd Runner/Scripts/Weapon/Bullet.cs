using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    [Header(" Settings ")]
    public float speed;

    private Rigidbody rb;
    private IObjectPool<Bullet> myPool;

    private float range;
    private int damage;
    private Vector3 startPosition;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
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
}
