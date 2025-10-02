using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSelector : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] Transform runnersParent;
    [SerializeField] RunnerSelector runnerSelectorPrefab;

    void OnDestroy()
    {
        ShopManager.onSkinSelected -= SelectSkin;
    }

    void Start()
    {
        ShopManager.onSkinSelected += SelectSkin;
    }

    public void SelectSkin(int skinIndex)
    {
        for (int i = 0; i < runnersParent.childCount; i++)
        {
            runnersParent.GetChild(i).GetComponent<RunnerSelector>().SelectRunner(skinIndex);
        }

        runnerSelectorPrefab.SelectRunner(skinIndex);
    }
}
