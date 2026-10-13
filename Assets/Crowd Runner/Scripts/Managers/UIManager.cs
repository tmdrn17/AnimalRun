using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum PanelType
{
    Menu,
    Game,
    GameOver,
    LevelComplete,
    Settings,
    Shop,
    Pause,
    Exit,
    None
}

[System.Serializable]
public struct PanelInfo
{
    public PanelType type;
    public GameObject panelObject;
}

public class UIManager : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] ShopManager shopManager;
    [SerializeField] ShootingManager shootingManager;

    [Header(" Elements ")]
    public GameObject[] allPanels = new GameObject[8];
    [SerializeField] private PanelInfo[] panelInfos;
    private Dictionary<PanelType, GameObject> panelDict = new();

    [SerializeField] Slider progressBar;
    [SerializeField] Text levelText;
    
    public void ToggleSettingsPanel() => TogglePanel(PanelType.Settings);
    public void ToggleShopPanel() => TogglePanel(PanelType.Shop);
    public void TogglePausePanel() => TogglePanel(PanelType.Pause);
    public void ToggleExitPanel() => TogglePanel(PanelType.Exit);
    
    private void Awake()
    {
        foreach (var info in panelInfos)
        {
            if (!panelDict.ContainsKey(info.type))
            {
                panelDict.Add(info.type, info.panelObject);
            }
        }
    }
    
    private void Start()
    {
        progressBar.value = 0;
        
        ClosePanel(PanelType.Game);
        ClosePanel(PanelType.GameOver);
        ClosePanel(PanelType.Settings);
        ClosePanel(PanelType.Shop);

        levelText.text = "Level " + (ChunkManager.instance.GetLevel() + 1);

        GameManager.onGameStateChanged += GameStateChangedCallback;
    }

    private void OnDestroy()
    {
        GameManager.onGameStateChanged -= GameStateChangedCallback;
    }

    private void Update()
    {
        UpdateProgressBar();
    }
    
    public void OpenPanel(PanelType type)
    {
        foreach (var panel in panelDict)
        {
            if (panel.Key == type)
            {
                panel.Value.SetActive(true);
            }
        }
    }
    
    public void ClosePanel(PanelType type = PanelType.None, bool allClose = false)
    {
        if (type == PanelType.None && allClose)
        {
            foreach (var panel in panelDict)
            {
                panel.Value.SetActive(false);
            }
            
            return;
        }
        
        foreach (var panel in panelDict)
        {
            if (panel.Key == type)
            {
                panel.Value.SetActive(false);
            }
        }
    }

    private void GameStateChangedCallback(GameManager.GameState gameState)
    {
        if (gameState == GameManager.GameState.Gameover)
        {
            ClosePanel(PanelType.Game);
            OpenPanel(PanelType.GameOver);
        }

        else if (gameState == GameManager.GameState.LevelComplete)
        {
            ClosePanel(PanelType.Game);
            OpenPanel(PanelType.LevelComplete);
        }
    }

    public void PlayButtonPressed()
    {
        GameManager.instance.SetGameState(GameManager.GameState.Select);

        ClosePanel(PanelType.Menu);
        OpenPanel(PanelType.Game);
    }

    public void RetryButtonPressed()
    {
        SceneManager.LoadScene(0);
    }

    public void UpdateProgressBar()
    {
        if (!GameManager.instance.IsGameState(GameManager.GameState.Game))
            return;

        float progress = PlayerController.instance.transform.position.z / ChunkManager.instance.GetFinishZ();
        progressBar.value = progress;
    }

    public void ShowSettingsPanel(int panelNum)
    {
        if (allPanels[panelNum].activeSelf)
        {
            Time.timeScale = 1.0f;
            allPanels[panelNum].SetActive(false);
        }
        else
        {
            Time.timeScale = 0.0f;
            allPanels[panelNum].SetActive(true);
        }
    }
    
    private void TogglePanel(PanelType type)
    {
        if (panelDict.TryGetValue(type, out GameObject panel))
        {
            if (panel.activeSelf)
            {
                Time.timeScale = 1.0f;
                panel.SetActive(false);
            }
            else
            {
                Time.timeScale = 0.0f;
                panel.SetActive(true);
            }
        }
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
          Application.Quit();
#endif
    }
}
