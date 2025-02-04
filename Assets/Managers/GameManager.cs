using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public enum GameState {Playing, Pause}

public class GameManager : SingletonPersistent<GameManager>
{
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoadedDelegate;
        SceneManager.sceneUnloaded += OnSceneUnloadedDelegate;

        OnSceneLoaded += SetDefaultGameState;
        
        SetDefaultGameState();
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedDelegate;
        SceneManager.sceneUnloaded -= OnSceneUnloadedDelegate;
    }
    
    #region States
    
    private const GameState DefaultGameState = GameState.Playing;
    
    public GameState CurrentGameState { get; private set; }
    public GameState PreviousGameState { get; private set; }
    
    public static bool GameIsPaused => Instance.CurrentGameState == GameState.Pause;
    
    public event Action OnPause;
    public event Action OnUnpause;
    
    
    public static void ReturnToPreviousState() => SwitchGameState(Instance.PreviousGameState);
    private static void SetDefaultGameState() => SwitchGameState(DefaultGameState);

    public static void SwitchGameState(GameState newState)
    {
        if (Instance.CurrentGameState == newState) return;
        
        Instance.PreviousGameState = Instance.CurrentGameState;
        Instance.CurrentGameState = newState;

        Instance.HandleStateEvents();
    }
    
    private void HandleStateEvents()
    {
        if (Instance.CurrentGameState == GameState.Pause)
            Instance.OnPause?.Invoke();
        
        if (Instance.PreviousGameState == GameState.Pause)
            Instance.OnUnpause?.Invoke();
    }
    
    #endregion
    
    #region Scene

    // Cambia los eventos static de SceneManager por unos locales
    // Permite encapsular el control del evento en el GameManager para ser el unico en desuscribirse
    // Al no ser static los eventos sustitutos se limpian solos al cerrar el juego
    public event Action OnSceneLoaded;
    public event Action OnSceneUnloaded;
    private void OnSceneLoadedDelegate(Scene scene, LoadSceneMode mode) => OnSceneLoaded?.Invoke();
    private void OnSceneUnloadedDelegate(Scene scene) => OnSceneUnloaded?.Invoke();
    
    public static void ResetScene()
    {
        SetDefaultGameState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    #endregion

    #region Camera

    public static Camera Camera { 
        get => Camera.main;
        set
        {
            if (value == null) Debug.LogError("Camera " + value.name + " is not found or not a Camera");
            
            if (Camera.main != null)
                Camera.main.gameObject.SetActive(false);
            value.gameObject.SetActive(true);
        }
    }


    #endregion
    
    #region Quit Game

    public event Action OnQuitGame;
    
    public static void Quit () 
    {
        Instance.OnQuitGame?.Invoke();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    

    #endregion
}
