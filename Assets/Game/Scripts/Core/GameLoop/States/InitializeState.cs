using UnityEngine;

public class InitializeState : GameLoopState
{
    private readonly PlayerProgress _progress;

    public InitializeState(GameLoopStateMachine stateMachine, PlayerProgress progress) : base(stateMachine)
    {
        _progress = progress;
    }

    public override void OnStateActivated()
    {
        Debug.Log("[InitializeState] activated");
        _progress.Load();
        StateMachine.SetState(GameLoopStateMachine.State.MainMenu);
    }

    public override void OnStateDisabled()
    {
    }
}
