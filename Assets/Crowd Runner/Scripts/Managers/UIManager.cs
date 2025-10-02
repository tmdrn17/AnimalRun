using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UIManager : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] ShopManager shopManager;
    [SerializeField] ShootingManager shootingManager;

    [Header(" Elements ")]
    /* * 0 = Menu Pannel  1 = Game Pannel
       * 2 = GameOver Pannel  3 = Level Complete Pannel
       * 4 = Settings Pannel  5 = Shop Pannel
       * 6 = Pause Pannel  7 = Exit Pannel */
    public GameObject[] allPanels = new GameObject[8];

    [SerializeField] Slider progressBar;
    [SerializeField] Text levelText;

    // Start is called before the first frame update
    void Start()
    {
        progressBar.value = 0;

        allPanels[1].SetActive(false);
        allPanels[2].SetActive(false);
        allPanels[4].SetActive(false);
        allPanels[5].SetActive(false);

        levelText.text = "Level " + (ChunkManager.instance.GetLevel() + 1);

        GameManager.onGameStateChanged += GameStateChangedCallback;
    }

    private void OnDestroy()
    {
        GameManager.onGameStateChanged -= GameStateChangedCallback;
    }

    void Update()
    {
        UpdateProgressBar();
    }

    private void GameStateChangedCallback(GameManager.GameState gameState)
    {
        if (gameState == GameManager.GameState.Gameover)
        {
            ShowGameover();
        }

        else if (gameState == GameManager.GameState.LevelComplete)
        {
            ShowLevelComplete();
        }
    }

    public void PlayButtonPressed()
    {
        GameManager.instance.SetGameState(GameManager.GameState.Game);

        allPanels[0].SetActive(false);
        allPanels[1].SetActive(true);
    }

    public void RetryButtonPressed()
    {
        SceneManager.LoadScene(0);
    }

    public void ShowGameover()
    {
        allPanels[1].SetActive(false);
        allPanels[2].SetActive(true);
    }

    private void ShowLevelComplete()
    {
        allPanels[1].SetActive(false);
        allPanels[3].SetActive(true);
    }

    public void UpdateProgressBar()
    {
        if (!GameManager.instance.IsGameState(1))
            return;

        float progress = PlayerController.instance.transform.position.z / ChunkManager.instance.GetFinishZ();
        progressBar.value = progress;
    }

    public void ShowSettingsPanel(int pannelNum)
    {
        if (allPanels[pannelNum].activeSelf)
        {
            Time.timeScale = 1.0f;
            allPanels[pannelNum].SetActive(false);
        }
        else
        {
            Time.timeScale = 0.0f;
            allPanels[pannelNum].SetActive(true);
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
