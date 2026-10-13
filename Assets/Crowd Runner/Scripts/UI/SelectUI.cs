using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectUI : MonoBehaviour
{
    [SerializeField] private GameObject animalSelectPanel;
    [SerializeField] private GameObject weaponSelectPanel;
    
    [SerializeField] Button[] animalButtons;
    [SerializeField] Button[] weaponButtons;

    // 상위 컨트롤러에 알리기 위한 이벤트
    public event Action OnAnimalSelectRequested;
    public event Action OnWeaponSelectRequested;

    private void Start()
    {
        
    }

    public void OnClickAnimalButton()
    {
        OnAnimalSelectRequested?.Invoke();
    }

    public void OnClickWeaponButton()
    {
        OnWeaponSelectRequested?.Invoke();
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