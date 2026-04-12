using UnityEngine;

public class LevelFailedState : GameLoopState
{
    private readonly SceneLoader _sceneLoader;
    private float _returnAt;

    public LevelFailedState(GameLoopStateMachine stateMachine, SceneLoader sceneLoader)
        : base(stateMachine)
    {
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[LevelFailedState] возврат в меню через 1.5с");
        _returnAt = Time.time + 1.5f;
    }

    public override void Update()
    {
        if (Time.time >= _returnAt)
        {
            _returnAt = float.MaxValue;
            _sceneLoader.LoadScene("Menu", () =>
                StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
        }
    }

    public override void OnStateDisabled() { }
}
