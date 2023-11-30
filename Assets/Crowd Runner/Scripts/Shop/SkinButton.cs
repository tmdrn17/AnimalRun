using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkinButton : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] Button thisButton;
    [SerializeField] Image skinImage;
    [SerializeField] GameObject lockImage;
    [SerializeField] GameObject selector;

    [SerializeField]bool unlocked;


    public void Configure(Sprite skinSprite, bool unlocked)
    {
        skinImage.sprite = skinSprite;
        this.unlocked = unlocked;


        if (unlocked)
        {
            Unlocked();
        }
        else
        {
            Lock();
        }
    }

    public void Unlocked()
    {
        thisButton.interactable = true;
        skinImage.gameObject.SetActive(true);
        lockImage.SetActive(false);

        unlocked = true;
    }

    void Lock()
    {
        thisButton.interactable = false;
        skinImage.gameObject.SetActive(false);
        lockImage.SetActive(true);
    }

    public void Select()
    {
        selector.SetActive(true);
    }

    public void Deselect()
    {
        selector.SetActive(false);
    }

    public Button GetButton()
    {
        return thisButton;
    }

    public bool IsUnlocked()
    {
        return unlocked;
    }
}
