using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public enum GameState {Menu, Select, Game, LevelComplete, Gameover}

    public GameState gameState { get; private set; }

    public static Action<GameState> onGameStateChanged;

    [SerializeField] UIManager uiManager;

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);
        else
            instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1.0f;
    }

    private void Update()
    {
        PauseGame();
    }

    public void SetGameState(GameState gameState)
    {
        this.gameState = gameState;
        onGameStateChanged?.Invoke(gameState);
    }

    public bool IsGameState(GameState state)
    {
        return gameState == state;
    }

    private void PauseGame()
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