using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[System.Serializable]
public class ButtonElement
{ 
    public Image buttonImage;
    public Text name;
    public Text description;
}

public class SelectUI : MonoBehaviour
{
    [SerializeField] private GameObject animalSelectPanel;
    [SerializeField] private GameObject weaponSelectPanel;

    [SerializeField] private ButtonElement[] animalButtons;
    [SerializeField] private ButtonElement[] weaponButtons;
    
    public int AnimalButtonCount => animalButtons.Length;
    public int WeaponButtonCount => weaponButtons.Length;
    
    public void SetAnimalButton(int index, Sprite sprite, string nameText, string descText) =>
        SetButtonData(animalButtons, index, sprite, nameText, descText);
    public void SetWeaponButton(int index, Sprite sprite, string nameText, string descText) =>
        SetButtonData(weaponButtons, index, sprite, nameText, descText);
    
    // 1. 내부에서 공통으로 처리하는 메인 함수 (중복 제거)
    private void SetButtonData(ButtonElement[] targetButtons, int index, Sprite sprite, string nameText, string descText)
    {
        targetButtons[index].buttonImage.sprite = sprite;
        targetButtons[index].name.text = nameText;
        targetButtons[index].description.text = descText;
    }

    public void ShowWeaponPanel()
    {
        animalSelectPanel.SetActive(false);
        weaponSelectPanel.SetActive(true);
    }

    public void CloseAll()
    {
        animalSelectPanel.SetActive(false);
        weaponSelectPanel.SetActive(false);
    }
}