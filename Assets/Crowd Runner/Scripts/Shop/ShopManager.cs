using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] Button purchaseButton;
    [SerializeField] SkinButton[] skinButtons;

    [Header("Skins")]
    [SerializeField] Sprite[] skins;

    [Header("Princing")]
    [SerializeField] int skinPrice;
    [SerializeField] Text priceText;

    void Awake()
    {
        priceText.text = skinPrice.ToString();
    }

    void Start()
    {
        UpdeatePurchaseButton();

        ConfigureButtons();

        //SelectSkin(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UnlockSkin(Random.Range(0, skinButtons.Length));
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            PlayerPrefs.DeleteAll();
        }

    }

    void ConfigureButtons()
    {
        for (int i = 0; i < skinButtons.Length; i++)
        {
            bool unlocked = PlayerPrefs.GetInt("skinButton" + i) == 1;

            skinButtons[i].Configure(skins[i], unlocked);

            int skinIndex = i;

            skinButtons[i].GetButton().onClick.AddListener(() => SelectSkin(skinIndex));
        }
    }

    public void UnlockSkin(int skinIndex)
    {
        PlayerPrefs.SetInt("skinButton" + skinIndex, 1);
        skinButtons[skinIndex].Unlocked();
    }

    void UnlockSkin(SkinButton skinButton)
    {
        int skinIndex = skinButton.transform.GetSiblingIndex();
        UnlockSkin(skinIndex);
    }

    void SelectSkin(int skinIndex)
    {
        for(int i = 0; i < skinButtons.Length; i++)
        {
            if (skinIndex == i)
            {
                skinButtons[i].Select();
            }
            else
            {
                skinButtons[i].Deselect();
            }
        }
    }

    public void PurChaseSkin()
    {
        List<SkinButton> skinButtonList = new List<SkinButton>();


        for (int i = 0;i < skinButtons.Length;i++)
        {
            if (skinButtons[i].IsUnlocked()) continue;
            skinButtonList.Add(skinButtons[i]);
        }

        if (skinButtonList.Count <= 0)
        {
            return;
        }

        SkinButton randomSkinButton = skinButtonList[Random.Range(0, skinButtonList.Count)];


        UnlockSkin(randomSkinButton);
        SelectSkin(randomSkinButton.transform.GetSiblingIndex());

        DataManager.instance.UseCoin(skinPrice);

        UpdeatePurchaseButton();
    }

    public void UpdeatePurchaseButton()
    {
        Debug.Log(DataManager.instance.GetCoins());
        if (DataManager.instance.GetCoins() < skinPrice)
        {
            purchaseButton.interactable = false;
        }
        else
        {
            purchaseButton.interactable = true;
        }
    }
}
