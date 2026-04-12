using UnityEngine;
using Zenject;

public class GameplayState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SignalBus _signalBus;
    private readonly SceneLoader _sceneLoader;

    private PausePresenter _pausePresenter;
    private int _lastLevelId;

    public GameplayState(GameLoopStateMachine stateMachine, DiContainer container,
        SignalBus signalBus, SceneLoader sceneLoader) : base(stateMachine)
    {
        _container = container;
        _signalBus = signalBus;
        _sceneLoader = sceneLoader;
    }

    public override void OnStateRegistered()
    {
        _signalBus.Subscribe<BaseDestroyedSignal>(OnBaseDestroyed);
        _signalBus.Subscribe<AllWavesCompletedSignal>(OnAllWavesCompleted);
        _signalBus.Subscribe<EnemyReachedBaseSignal>(OnEnemyReachedBase);
    }

    public override void OnStateActivated()
    {
        Debug.Log("[GameplayState] activated");
        Time.timeScale = 1f;

        var levelContext = Object.FindFirstObjectByType<LevelContext>();
        if (levelContext == null)
        {
            Debug.LogError("[GameplayState] LevelContext not found in scene");
            return;
        }

        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        _lastLevelId = levelContext.Config.Id;

        levelContext.PlayerBase.Init(levelContext.Config.BaseHealth);

        var wallet = sceneContainer.Resolve<Wallet>();
        wallet.SetStartingGold(levelContext.Config.StartingGold);

        var spawner = sceneContainer.Resolve<WaveSpawner>();
        spawner.Run(levelContext.Config.Waves, levelContext.Paths);

        _pausePresenter = sceneContainer.TryResolve<PausePresenter>();
        if (_pausePresenter != null)
        {
            _pausePresenter.Restart += OnPauseRestart;
            _pausePresenter.BackToMenu += OnPauseBackToMenu;
        }
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_pausePresenter != null)
        {
            _pausePresenter.Restart -= OnPauseRestart;
            _pausePresenter.BackToMenu -= OnPauseBackToMenu;
            _pausePresenter = null;
        }
    }

    private void OnEnemyReachedBase(EnemyReachedBaseSignal _) => OnBaseDestroyed();

    private void OnBaseDestroyed()
    {
        _signalBus.Fire(new LevelFailedSignal());
        StateMachine.SetState(GameLoopStateMachine.State.LevelFailed);
    }

    private void OnAllWavesCompleted()
    {
        StateMachine.SetState(GameLoopStateMachine.State.LevelComplete);
    }

    private void OnPauseRestart()
    {
        _signalBus.Fire(new LevelStartRequestedSignal { LevelId = _lastLevelId });
        StateMachine.SetState(GameLoopStateMachine.State.LoadLevel);
    }

    private void OnPauseBackToMenu()
    {
        _sceneLoader.LoadScene("Menu", () =>
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
    }
}
