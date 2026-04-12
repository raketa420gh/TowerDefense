using UnityEngine;
using Zenject;

public class LoadLevelState : GameLoopState
{
    private readonly SceneLoader _sceneLoader;
    private readonly SignalBus _signalBus;
    private readonly LevelCatalog _catalog;

    private int _pendingLevelId;

    public LoadLevelState(GameLoopStateMachine stateMachine, SceneLoader sceneLoader, SignalBus signalBus, LevelCatalog catalog)
        : base(stateMachine)
    {
        _sceneLoader = sceneLoader;
        _signalBus = signalBus;
        _catalog = catalog;
    }

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<LevelStartRequestedSignal>(OnLevelStartRequested);
    }

    public override void OnStateActivated()
    {
        Debug.Log($"[LoadLevelState] loading level {_pendingLevelId}");
        var def = _catalog.Get(_pendingLevelId);
        _sceneLoader.LoadScene(def.SceneName, () =>
        {
            _signalBus.Fire(new LevelLoadedSignal { LevelId = _pendingLevelId });
            StateMachine.SetState(GameLoopStateMachine.State.Gameplay);
        });
    }

    public override void OnStateDisabled()
    {
    }

    private void OnLevelStartRequested(LevelStartRequestedSignal signal)
    {
        _pendingLevelId = signal.LevelId;
        StateMachine.SetState(GameLoopStateMachine.State.LoadLevel);
    }
}
