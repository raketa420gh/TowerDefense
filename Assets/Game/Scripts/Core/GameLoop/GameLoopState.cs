public abstract class GameLoopState : State<GameLoopStateMachine>
{
    protected GameLoopState(GameLoopStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void OnStateRegistered()
    {
    }

    public override void Update()
    {
    }
}
