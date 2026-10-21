using Animal_Run.Scripts.Data;
using Animal_Run.Scripts.Managers;
using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    [Header(" Elements ")]
    [Tooltip("폭발형 무기(Rocket, Grenade)의 총알이 터질 때 생성할 이펙트")]
    [SerializeField] private GameObject explosionPrefab;

    [Header(" Settings ")]
    public float speed;

    [Tooltip("프리팹에 콜라이더가 없을 때 자동으로 추가할 충돌 반경")]
    [SerializeField] private float defaultColliderRadius = 0.3f;
    [Tooltip("총알 이동 경로와 적 중심 사이의 수평 거리가 이 값 이하이면 명중으로 본다.")]
    [SerializeField] private float hitRadius = 0.6f;

    [Header(" Arc (Grenade) ")]
    [Tooltip("포물선 최고 높이 = 사정거리 × 이 값")]
    [SerializeField] private float arcHeightPerRange = 0.35f;
    [Tooltip("포물선 총알은 지면에서 이 높이 이하로 내려왔을 때만 적과 닿은 것으로 본다. (적 키)")]
    [SerializeField] private float arcContactHeight = 2.2f;
    [SerializeField] private float arcSpinSpeed = 540f;

    [Header(" Throwing (Axe, Cleaver) ")]
    [Tooltip("던지는 무기(Throwing)의 총알이 X축 기준으로 회전하는 속도 (도/초)")]
    [SerializeField] private float throwSpinSpeed = 1080f;

    [Header(" Explosion ")]
    [Tooltip("폭발 이펙트 크기 = 폭발 반경 × 이 값")]
    [SerializeField] private float explosionScalePerRadius = 1f;

    private Rigidbody rb;
    private IObjectPool<Bullet> myPool;

    private float range;
    private int damage;
    private float splashRadius;
    private bool isArc;
    private bool isSpinning;
    private bool isExplosive;

    private Vector3 startPosition;
    private Vector3 lastPosition;
    private Vector3 direction;
    private float groundY;
    private float traveledDistance;
    private bool isReleased;

    private void Awake()
    {
        // 일부 총알 프리팹(화염병, 수류탄)에는 Rigidbody/Collider가 없어 충돌 판정이 되지 않으므로 보완한다.
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY |
                             RigidbodyConstraints.FreezeRotation;
        }
        rb.useGravity = false;

        if (GetComponentInChildren<Collider>() == null)
        {
            SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = defaultColliderRadius;
        }

        foreach (Collider bulletCollider in GetComponentsInChildren<Collider>())
            bulletCollider.isTrigger = true;
    }

    private void Update()
    {
        if (isReleased)
            return;

        if (isArc)
            MoveAlongArc();

        if (isSpinning)
            Spin();

        // 무기가 높이 달린 동물(기린 등)은 총알이 적 콜라이더 위로 지나가 트리거가 발생하지 않는다.
        // 그래서 물리 충돌과 별개로, 이번 프레임에 지나온 경로와 적 사이의 수평 거리로 명중을 판정한다.
        Vector3 currentPosition = transform.position;
        CheckEnemyOnPath(lastPosition, currentPosition);
        lastPosition = currentPosition;

        if (isReleased)
            return;

        bool reachedRange = isArc
            ? traveledDistance >= range
            : Vector3.Distance(startPosition, currentPosition) >= range;

        if (reachedRange)
            OnReachRange();
    }

    private void FixedUpdate()
    {
        if (!isArc)
            rb.velocity = direction * speed;
    }

    #region 이동

    // 수류탄·화염병: 발사 위치에서 사정거리 지점의 지면까지 포물선을 그리며 날아간다.
    private void MoveAlongArc()
    {
        traveledDistance = Mathf.Min(traveledDistance + speed * Time.deltaTime, range);
        float progress = range > 0f ? traveledDistance / range : 1f;

        Vector3 position = startPosition + direction * traveledDistance;
        position.y = Mathf.Lerp(startPosition.y, groundY, progress)
                     + 4f * range * arcHeightPerRange * progress * (1f - progress);

        transform.position = position;
    }

    // 날아가는 방향은 direction으로 따로 저장해 두었으므로 총알 자체를 돌려도 경로는 바뀌지 않는다.
    private void Spin()
    {
        float spinSpeed = isArc ? arcSpinSpeed : throwSpinSpeed;
        transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);
    }

    #endregion

    #region 명중 판정

    private void CheckEnemyOnPath(Vector3 from, Vector3 to)
    {
        // 포물선 총알은 높이 떠 있는 동안에는 적 머리 위를 지나가므로 명중하지 않는다.
        if (isArc && to.y - groundY > arcContactHeight)
            return;

        Vector2 pathStart = new Vector2(from.x, from.z);
        Vector2 pathEnd = new Vector2(to.x, to.z);

        Enemy closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (Enemy enemy in ListManager.Enemies)
        {
            if (enemy.IsDead)
                continue;

            Vector2 enemyPosition = new Vector2(enemy.transform.position.x, enemy.transform.position.z);
            if (DistanceToSegment(enemyPosition, pathStart, pathEnd) > hitRadius)
                continue;

            // 경로 위에 적이 여럿이면 출발점에서 가장 가까운 적이 맞는다.
            float distanceFromStart = Vector2.Distance(pathStart, enemyPosition);
            if (distanceFromStart >= closestDistance)
                continue;

            closestDistance = distanceFromStart;
            closestEnemy = enemy;
        }

        if (closestEnemy != null)
            HitEnemy(closestEnemy);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0f)
            return Vector2.Distance(point, start);

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isReleased)
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || enemy.IsDead)
            return;

        HitEnemy(enemy);
    }

    private void HitEnemy(Enemy enemy)
    {
        if (isExplosive)
        {
            Explode(enemy);
            return;
        }

        enemy.OnDamage(damage);
        Release();
    }

    private void OnReachRange()
    {
        // 폭발형 총알은 적에게 닿지 않아도 사정거리 끝에서 터진다.
        if (isExplosive)
            Explode(null);
        else
            Release();
    }

    #endregion

    #region 폭발

    private void Explode(Enemy hitEnemy)
    {
        Vector3 explosionPosition = transform.position;

        SpawnExplosionEffect(explosionPosition);

        // 직접 맞은 적은 폭발 중심과 조금 떨어져 있어도 반드시 피해를 받는다.
        if (hitEnemy != null)
            hitEnemy.OnDamage(damage);

        // OnDamage로 적이 죽어도 목록에서 빠지는 시점은 프레임 끝이라 순회 중 목록이 바뀌지 않는다.
        for (int i = ListManager.Enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = ListManager.Enemies[i];
            if (enemy == hitEnemy)
                continue;

            Vector3 offset = enemy.transform.position - explosionPosition;
            offset.y = 0;

            // 높이 차이는 무시하고 수평 거리로만 폭발 범위를 판정한다.
            if (offset.magnitude <= splashRadius)
                enemy.OnDamage(damage);
        }

        Release();
    }

    private void SpawnExplosionEffect(Vector3 position)
    {
        if (explosionPrefab == null)
            return;

        GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.transform.localScale = Vector3.one * Mathf.Max(0.1f, splashRadius * explosionScalePerRadius);

        float lifeTime = 0f;
        foreach (ParticleSystem particle in explosion.GetComponentsInChildren<ParticleSystem>())
        {
            // 하위 파티클까지 폭발 반경에 맞춰 커지도록 계층 스케일을 따르게 한다.
            ParticleSystem.MainModule main = particle.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            lifeTime = Mathf.Max(lifeTime, main.duration + main.startLifetime.constantMax);
        }

        Destroy(explosion, lifeTime > 0f ? lifeTime : 3f);
    }

    #endregion

    private void Release()
    {
        if (isReleased)
            return;

        isReleased = true;
        if (!rb.isKinematic)
            rb.velocity = Vector3.zero;
        myPool.Release(this);
    }

    public void SetPool(IObjectPool<Bullet> pool)
    {
        myPool = pool;
    }

    public void Init(int damage, float range, float speed, float splashRadius, WeaponKind kind)
    {
        this.damage = damage;
        this.range = range;
        this.speed = speed;
        this.splashRadius = splashRadius;

        isArc = kind == WeaponKind.Grenade;
        isSpinning = isArc || kind == WeaponKind.Throwing;
        isExplosive = kind == WeaponKind.Rocket || kind == WeaponKind.Grenade;

        // 포물선 총알은 물리 대신 직접 위치를 계산해서 움직인다.
        rb.isKinematic = isArc;

        direction = transform.forward;
        direction.y = 0;
        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;

        startPosition = transform.position;
        lastPosition = startPosition;
        groundY = PlayerController.instance != null ? PlayerController.instance.transform.position.y : 0f;
        traveledDistance = 0f;
        isReleased = false;
    }
}
