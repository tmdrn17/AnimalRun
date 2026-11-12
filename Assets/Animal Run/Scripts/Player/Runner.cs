using System.Collections;
using UnityEngine;

public class Runner : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private Animator animator;
    [SerializeField] private Material hitMaterialOrigin;
    [SerializeField] private Material hitMaterialInstance;
    public Transform weaponPosition;

    private bool isDead;
    private Renderer bodyRenderer;
    private Material originMaterial;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        bodyRenderer = GetComponentInChildren<Renderer>();
        originMaterial = bodyRenderer.sharedMaterial;

        // 객체 이니셜라이저: 객체를 만들면서 { } 안에서 속성 값을 바로 넣는 C# 문법이다.
        // 1. new Material(hitMaterialOrigin) 생성자가 먼저 실행돼 원본을 복사한 새 머티리얼을 만든다.
        // 2. 그다음 { } 안의 대입이 그 복사본에 적용된다.
        // 3. 설정이 끝난 객체가 hitMaterialInstance에 대입된다.
        // 아래처럼 풀어 쓴 것과 동작이 같다. { } 안에는 public 필드나 set 가능한 속성만 쓸 수 있고, 여러 개면 쉼표로 구분한다.
        //   hitMaterialInstance = new Material(hitMaterialOrigin);
        //   hitMaterialInstance.mainTexture = originMaterial.mainTexture;
        hitMaterialInstance = new Material(hitMaterialOrigin)
        {
            mainTexture = originMaterial.mainTexture
        };
    }

    private void OnEnable()
    {
        isDead = false;
        bodyRenderer.sharedMaterial = originMaterial;
        ListManager.Runners.Add(this);
    }

    private void OnDisable()
    {
        ListManager.Runners.Remove(this);
    }

    public void Dead(Enemy enemy = null)
    {
        if (isDead)
            return;
        
        if (enemy != null && enemy.TryKillByRunner() == false)
            return;

        StartCoroutine(DieRoutine());
    }

    public Animator GetAnimator()
    {
        return animator;
    }

    public void MaterialChange(bool isHit)
    {
        bodyRenderer.sharedMaterial = isHit ? hitMaterialInstance : originMaterial;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        ListManager.Runners.Remove(this);
        MaterialChange(true);

        Transform currentParent = transform.parent;
        transform.SetParent(null);

        yield return new WaitForSecondsRealtime(1f);

        gameObject.SetActive(false);
        transform.SetParent(currentParent);
    }
}
