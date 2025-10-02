using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public enum GameState {Menu, Game, LevelComplete, Gameover}

    public GameState gameState { get; private set; }

    public static Action<GameState> onGameStateChanged;

    [SerializeField] UIManager uiManager;

    void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }

    void Start()
    {
        
        Time.timeScale = 1.0f;
    }

    void Update()
    {
        PauseGame();
    }

    public void SetGameState(GameState gameState)
    {
        this.gameState = gameState;
        onGameStateChanged?.Invoke(gameState);
    }

    public bool IsGameState(int stateNum)
    {
        switch (stateNum)
        {
            default: 
                return false;
            case 0:
                return gameState == GameState.Menu;
            case 1:
                return gameState == GameState.Game;
            case 2:
                return gameState == GameState.LevelComplete;
            case 3:
                return gameState == GameState.Gameover;
        }
        
    }

    void PauseGame()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            switch (gameState)
            {
                case GameState.Menu:
                    uiManager.allPanels[7].SetActive(true);
                    break;

                case GameState.Game:
                    Time.timeScale = 0.0f;
                    uiManager.allPanels[6].SetActive(true);
                    break;

                case GameState.LevelComplete:
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                    break;

                case GameState.Gameover:
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                    break;

            }


        }
    }
}