using UnityEngine;

public class GameplayState : GameLoopState
{
    private readonly SceneLoader _sceneLoader;
    private float _autoReturnTime;

    public GameplayState(GameLoopStateMachine stateMachine, SceneLoader sceneLoader) : base(stateMachine)
    {
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[GameplayState] activated (stub — auto-return in 1s)");
        _autoReturnTime = Time.time + 1f;
    }

    public override void Update()
    {
        if (Time.time >= _autoReturnTime)
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu);
    }

    public override void OnStateDisabled()
    {
    }
}
