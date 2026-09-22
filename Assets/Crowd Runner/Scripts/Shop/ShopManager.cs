using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

using Random = UnityEngine.Random;

public class ShopManager : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] Button purchaseButton;
    [SerializeField] SkinButton[] skinButtons;
    [SerializeField] SkinButton[] gunButtons;
    [SerializeField] SkinButton[] specialButtons;
    [SerializeField] GameObject panel;
    [SerializeField] Button skinTapButton;
    [SerializeField] Button gunTapButton;
    [SerializeField] Button specialTapButton;
    [SerializeField] GameObject skinItem;
    [SerializeField] GameObject gunItem;
    [SerializeField] GameObject specialItem;

    [Header("Skins")]
    [SerializeField] Sprite[] skins;
    [SerializeField] GameObject[] previewSkinObj;

    [Header("Gun")]
    [SerializeField] Sprite[] guns;
    [SerializeField] GameObject[] previewGunObj;

    [Header("Special")]
    [SerializeField] Sprite[] special;
    [SerializeField] GameObject[] previewSpecialObj;

    [Header("Pricing")]
    [SerializeField] int skinPrice;
    [SerializeField] Text priceText;

    [Header("Events")]
    public static Action<int> onSkinSelected;

    void Awake()
    {
        UnlockSkin(0);
        UnlockGun(0);

        priceText.text = skinPrice.ToString();

        if (!(PlayerPrefs.HasKey("lastSelectedSpecial")))
        {
            PlayerPrefs.SetInt("lastSelectedSpecial", -1);
        }        
    }


    IEnumerator Start()
    {
        UpdeatePurchaseButton();

        ConfigureButtons();

        yield return null;

        SelectSkin(GetLastSelecedSkin());
        PreviewSkin(GetLastSelecedSkin());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            UnlockSkin(Random.Range(0, skinButtons.Length));
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            PlayerPrefs.DeleteAll();
        }

        skinTapButton.GetComponent<Button>().onClick.AddListener(SkinTapChange);
        gunTapButton.GetComponent<Button>().onClick.AddListener(GunTapChange);
        specialTapButton.GetComponent<Button>().onClick.AddListener(SpecialTapChange);
    }

    public void ConfigureButtons()
    {
        for (int i = 0; i < skinButtons.Length; i++)
        {
            bool skinUnlocked = PlayerPrefs.GetInt("skinButton" + i) == 1;

            skinButtons[i].Configure(skins[i], skinUnlocked);

            int skinIndex = i;

            skinButtons[i].GetButton().onClick.AddListener(() => SelectSkin(skinIndex));
            skinButtons[i].GetButton().onClick.AddListener(() => PreviewSkin(skinIndex));
        }

        for (int i = 0; i < gunButtons.Length; i++)
        {
            bool gunUnlocked = PlayerPrefs.GetInt("gunButton" + i) == 1;

            gunButtons[i].Configure(guns[i], gunUnlocked);

            int gunIndex = i;

            gunButtons[i].GetButton().onClick.AddListener(() => SelectGun(gunIndex));
            gunButtons[i].GetButton().onClick.AddListener(() => PreviewGun(gunIndex));
        }

        for (int i = 0; i < specialButtons.Length; i++)
        {
            bool specialUnlocked = PlayerPrefs.GetInt("specialButton" + i) == 1;

            specialButtons[i].Configure(special[i], specialUnlocked);

            int specialIndex = i;

            specialButtons[i].GetButton().onClick.AddListener(() => SelectSpecial(specialIndex));
            specialButtons[i].GetButton().onClick.AddListener(() => PreviewSpecial(specialIndex));
        }
    }

    public void UnlockSkin(int skinIndex)
    {
        PlayerPrefs.SetInt("skinButton" + skinIndex, 1);
        skinButtons[skinIndex].Unlocked();
    }

    public void UnlockGun(int gunIndex)
    {
        PlayerPrefs.SetInt("gunButton" + gunIndex, 1);
        gunButtons[gunIndex].Unlocked();
    }

    public void UnlockSpecial(int specialIndex)
    {
        PlayerPrefs.SetInt("specialButton" + specialIndex, 1);
        specialButtons[specialIndex].Unlocked();
    }

    void UnlockSkin(SkinButton skinButton)
    {
        int skinIndex = skinButton.transform.GetSiblingIndex();
        UnlockSkin(skinIndex);
    }

    void UnlockGun(SkinButton gunButton)
    {
        int skinIndex = gunButton.transform.GetSiblingIndex();
        UnlockGun(skinIndex);
    }

    void UnlockSpecial(SkinButton specialButton)
    {
        int skinIndex = specialButton.transform.GetSiblingIndex();
        UnlockSpecial(skinIndex);
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

        onSkinSelected?.Invoke(skinIndex);

        SaveLastSelectedSkin(skinIndex);
    }

    void SelectGun(int skinIndex)
    {
        for (int i = 0; i < gunButtons.Length; i++)
        {
            if (skinIndex == i)
            {
                gunButtons[i].Select();
            }
            else
            {
                gunButtons[i].Deselect();
            }
        }

        SaveLastSelectedSkin(skinIndex);
    }

    void SelectSpecial(int skinIndex)
    {
        for (int i = 0; i < specialButtons.Length; i++)
        {
            if (skinIndex == i)
            {
                specialButtons[i].Select();
            }
            else
            {
                specialButtons[i].Deselect();
            }
        }

        SaveLastSelectedSkin(skinIndex);
    }

    void PreviewSkin(int skinIndex)
    {
        for (int i = 0; i < skinButtons.Length; i++)
        {
            if (skinIndex == i)
            {
                previewSkinObj[i].gameObject.SetActive(true);
            }
            else
            {
                previewSkinObj[i].gameObject.SetActive(false);
            }
        }
        for (int i = 0; i < gunButtons.Length; i++)
        {
            previewGunObj[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < specialButtons.Length; i++)
        {
            previewSpecialObj[i].gameObject.SetActive(false);
        }
    }

    void PreviewGun(int gunIndex)
    {
        for (int i = 0; i < gunButtons.Length; i++)
        {
            if (gunIndex == i)
            {
                previewGunObj[i].gameObject.SetActive(true);
            }
            else
            {
                previewGunObj[i].gameObject.SetActive(false);
            }
        }
        for (int i = 0; i < skinButtons.Length; i++)
        {
            previewSkinObj[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < specialButtons.Length; i++)
        {
            previewSpecialObj[i].gameObject.SetActive(false);
        }
    }

    void PreviewSpecial(int specialIndex)
    {
        for (int i = 0; i < specialButtons.Length; i++)
        {
            if (specialIndex == i)
            {
                if (previewSpecialObj[i] != null)
                {
                    previewSpecialObj[i].gameObject.SetActive(true);
                }
            }
            else
            {
                if (previewSpecialObj[i] != null)
                {
                    previewSpecialObj[i].gameObject.SetActive(false);
                }
            }
            Debug.Log(specialIndex);
        }
        for (int i = 0; i < skinButtons.Length; i++)
        {
            previewSkinObj[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < gunButtons.Length; i++)
        {
            previewGunObj[i].gameObject.SetActive(false);
        }
    }

    public void PurChaseSkin()
    {
        if (skinItem.gameObject.activeSelf)
        {
            List<SkinButton> skinButtonList = new List<SkinButton>();
            for (int i = 0; i < skinButtons.Length; i++)
            {
                if (skinButtons[i].IsUnlocked()) continue;
                skinButtonList.Add(skinButtons[i]);
            }

            SkinButton randomSkinButton = skinButtonList[Random.Range(0, skinButtonList.Count)];


            UnlockSkin(randomSkinButton);
            SelectSkin(randomSkinButton.transform.GetSiblingIndex());
            PreviewSkin(randomSkinButton.transform.GetSiblingIndex());

            if (skinButtonList.Count <= 0)
            {
                return;
            }
        }

        else if(gunItem.gameObject.activeSelf)
        {
            List<SkinButton> gunButtonList = new List<SkinButton>();

            for (int i = 0; i < gunButtons.Length; i++)
            {
                if (gunButtons[i].IsUnlocked()) continue;
                gunButtonList.Add(gunButtons[i]);
            }

            SkinButton randomGunButton = gunButtonList[Random.Range(0, gunButtonList.Count)];

            UnlockGun(randomGunButton);
            SelectGun(randomGunButton.transform.GetSiblingIndex());
            PreviewGun(randomGunButton.transform.GetSiblingIndex());

            if (gunButtonList.Count <= 0)
            {
                return;
            }
        }

        else if (specialItem.gameObject.activeSelf)
        {
            List<SkinButton> specialButtonList = new List<SkinButton>();

            for (int i = 0; i < specialButtons.Length; i++)
            {
                if (specialButtons[i].IsUnlocked()) continue;
                specialButtonList.Add(gunButtons[i]);
            }

            SkinButton randomSpecialButton = specialButtonList[Random.Range(0, specialButtonList.Count)];

            UnlockSpecial(randomSpecialButton);
            SelectSpecial(randomSpecialButton.transform.GetSiblingIndex());
            PreviewSpecial(randomSpecialButton.transform.GetSiblingIndex());

            if (specialButtonList.Count <= 0)
            {
                return;
            }
        }

        DataManager.instance.UseCoin(skinPrice);

        UpdeatePurchaseButton();
    }

    public void UpdeatePurchaseButton() 
    {
        if (DataManager.instance.GetCoins() < skinPrice)
        {
            purchaseButton.interactable = false;
        }
        else
        {
            purchaseButton.interactable = true;
        }
    }

    int GetLastSelecedSkin()
    {
        return PlayerPrefs.GetInt("lastSelectedSkin", 0);
    }

    int GetLastSelecedGun()
    {
        return PlayerPrefs.GetInt("lastSelectedGun", 0);
    }

    int GetLastSelecedSpecial()
    {
        return PlayerPrefs.GetInt("lastSelectedSpecial", 0);
    }

    void SaveLastSelectedSkin(int skinIndex)
    {
        if (skinItem.gameObject.activeSelf)
        {
            PlayerPrefs.SetInt("lastSelectedSkin", skinIndex);
        }
        else if (gunItem.gameObject.activeSelf)
        {
            PlayerPrefs.SetInt("lastSelectedGun", skinIndex);
        }
        else if (specialItem.gameObject.activeSelf)
        {
            PlayerPrefs.SetInt("lastSelectedSpecial", skinIndex);
        }

    }

    public void SkinTapChange()
    {
        Color buttonColor = skinTapButton.gameObject.GetComponent<Image>().color;
        panel.GetComponent<Image>().color = buttonColor;
        gunItem.gameObject.SetActive(false);
        specialItem.gameObject.SetActive(false);
        skinItem.gameObject.SetActive(true);
        PreviewSkin(GetLastSelecedSkin());
        SelectSkin(GetLastSelecedSkin());
    }

    public void GunTapChange()
    {
        Color buttonColor = gunTapButton.gameObject.GetComponent<Image>().color;
        panel.GetComponent<Image>().color = buttonColor;
        skinItem.gameObject.SetActive(false);
        specialItem.gameObject.SetActive(false);
        gunItem.gameObject.SetActive(true);
        PreviewGun(GetLastSelecedGun());
        SelectGun(GetLastSelecedGun());
    }

    public void SpecialTapChange()
    {
        Color buttonColor = specialTapButton.gameObject.GetComponent<Image>().color;
        panel.GetComponent<Image>().color = buttonColor;
        skinItem.gameObject.SetActive(false);
        gunItem.gameObject.SetActive(false);
        specialItem.gameObject.SetActive(true);
        PreviewSpecial(GetLastSelecedSpecial());
        SelectSpecial(GetLastSelecedSpecial());
       // Debug.LogWarning(GetLastSelecedSpecial());
    }
}
