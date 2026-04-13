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
        if (_stateMachine == null)
        {
            Debug.LogError("[GameRoot] _stateMachine is null — Zenject injection failed. Check ProjectContext / ProjectInstaller for binding errors.");
            return;
        }
        _stateMachine.Initialise(this, GameLoopStateMachine.State.Initialize);
    }

    private void Update()
    {
        _stateMachine?.ActiveState?.Update();
    }
}
