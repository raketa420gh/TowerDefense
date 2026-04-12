public abstract class State<T> : BaseState
{
    protected readonly T _stateMachine;

    public T StateMachine => _stateMachine;

    public State(T stateMachine)
    {
        _stateMachine = stateMachine;
    }
}
