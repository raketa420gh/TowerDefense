using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuState : GameLoopState
{
    private const string MenuSceneName = "Menu";

    private readonly SceneLoader _sceneLoader;

    public MainMenuState(GameLoopStateMachine stateMachine, SceneLoader sceneLoader) : base(stateMachine)
    {
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[MainMenuState] activated");
        if (SceneManager.GetActiveScene().name != MenuSceneName)
            _sceneLoader.LoadScene(MenuSceneName);
    }

    public override void OnStateDisabled()
    {
    }
}
