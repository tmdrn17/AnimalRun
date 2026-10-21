using UnityEngine;
using System;

public class PlayerDetection : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private CrowdSystem crowdSystem;

    [Header(" Events ")]
    public static Action onDoorsHit;

    private void Update()
    {
        if(GameManager.instance.IsGameState(GameManager.GameState.Game))
            DetectColliders();
    }

    private void DetectColliders()
    {
        Collider[] detectedColliders = Physics.OverlapSphere(transform.position, crowdSystem.GetCrowdRadius());

        for (int i = 0; i < detectedColliders.Length; i++)
        {
            if(detectedColliders[i].TryGetComponent(out Doors doors))
            {
                // hit doors

                int bonusAmount = doors.GetBonusAmount(transform.position.x);
                BonusType bonusType = doors.GetBonusType(transform.position.x);

                doors.Disable();

                onDoorsHit?.Invoke();

                crowdSystem.ApplyBonus(bonusType, bonusAmount);
            }
        
            else if(detectedColliders[i].CompareTag("Finish"))
            {
                Debug.Log("We've hit the finish line");

                PlayerPrefs.SetInt("level", PlayerPrefs.GetInt("level") + 1);

                GameManager.instance.SetGameState(GameManager.GameState.LevelComplete);

                return;
            }

            else if(detectedColliders[i].CompareTag("Coin"))
            {
                // 코인은 경험치로 사용된다. 같은 프레임에 두 번 집계되지 않도록 콜라이더를 먼저 끈다.
                detectedColliders[i].enabled = false;
                Destroy(detectedColliders[i].gameObject);

                DataManager.instance.AddExp(1);
            }
        }
    }
}
