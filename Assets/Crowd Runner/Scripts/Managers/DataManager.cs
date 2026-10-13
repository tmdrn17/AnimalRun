using Crowd_Runner.Scripts.Data;
using UnityEngine;
using UnityEngine.UI;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;

    [Header(" Coin Texts ")]
    [SerializeField] private Text[] coinsTexts;
    private int coins;
    
    public WeaponTable weaponTable;
    public AnimalTable animalTable;

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;

        coins = PlayerPrefs.GetInt("coins", 2000);
        
        animalTable = new AnimalTable();
        weaponTable = new WeaponTable();

        animalTable.Load();
        weaponTable.Load();
    }

    private void Start()
    {
        UpdateCoinsTexts();
    }
    
    private void UpdateCoinsTexts()
    {
        foreach(Text coinText in coinsTexts)
        {
            coinText.text = coins.ToString();
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;

        UpdateCoinsTexts();

        PlayerPrefs.SetInt("coins", coins);
    }

    public void UseCoin(int amount)
    {
        coins -= amount;

        UpdateCoinsTexts();

        PlayerPrefs.SetInt("coins", coins);
    }

    public int GetCoins()
    {
        return coins;
    }
}
