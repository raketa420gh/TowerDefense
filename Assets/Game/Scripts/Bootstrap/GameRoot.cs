using UnityEngine;
using Zenject;

public class GameRoot : MonoBehaviour
{
    private GameLoopStateMachine _stateMachine;

    public GameLoopStateMachine StateMachine => _stateMachine;

    [Inject]
    public void Construct(GameLoopStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    private void Start()
    {
        _stateMachine.Initialise(this, GameLoopStateMachine.State.Initialize);
    }

    private void Update()
    {
        _stateMachine.ActiveState?.Update();
    }
}
