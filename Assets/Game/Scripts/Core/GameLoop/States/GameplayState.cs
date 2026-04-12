using UnityEngine;
using Zenject;

public class GameplayState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;

    public GameplayState(GameLoopStateMachine stateMachine, DiContainer container, SignalBus signalBus)
        : base(stateMachine)
    {
        _container = container;
        _signalBus = signalBus;
    }

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<BaseDestroyedSignal>(OnBaseDestroyed);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
    }

    public override void OnStateActivated()
    {
        Debug.Log("[GameplayState] activated");

        var levelContext = Object.FindFirstObjectByType<LevelContext>();
        if (levelContext == null)
        {
            Debug.LogError("[GameplayState] LevelContext not found in scene");
            return;
        }

        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        levelContext.PlayerBase.Init(levelContext.Config.BaseHealth);

        var wallet = sceneContainer.Resolve<Wallet>();
        wallet.SetStartingGold(levelContext.Config.StartingGold);

        var spawner = sceneContainer.Resolve<WaveSpawner>();
        spawner.Run(levelContext.Config.Waves, levelContext.Path);
    }

    public override void Update() { }

    public override void OnStateDisabled() { }

    private void OnBaseDestroyed()
    {
        _signalBus.Fire(new LevelFailedSignal());
        StateMachine.SetState(GameLoopStateMachine.State.LevelFailed);
    }

    private void OnAllWavesCompleted()
    {
        StateMachine.SetState(GameLoopStateMachine.State.LevelComplete);
    }
}
