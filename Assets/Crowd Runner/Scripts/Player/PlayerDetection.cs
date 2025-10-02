using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class PlayerDetection : MonoBehaviour
{
    [Header(" Elements ")]
    [SerializeField] private CrowdSystem crowdSystem;
    [SerializeField] private Transform runnersParent;

    [Header(" Events ")]
    public static Action onDoorsHit;

    void Update()
    {
        if(GameManager.instance.IsGameState(1))
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
        
            else if(detectedColliders[i].tag == "Finish")
            {
                Debug.Log("We've hit the finish line");

                PlayerPrefs.SetInt("level", PlayerPrefs.GetInt("level") + 1);

                DataManager.instance.AddCoins(runnersParent.childCount);

                GameManager.instance.SetGameState(GameManager.GameState.LevelComplete);
            }
        
            else if(detectedColliders[i].tag == "Coin")
            {
                Destroy(detectedColliders[i].gameObject);

                DataManager.instance.AddCoins(1);
            }
        }
    }
}
