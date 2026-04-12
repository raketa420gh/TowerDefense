using UnityEngine;
using Zenject;

public class LevelFailedState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SceneLoader _sceneLoader;
    private readonly SignalBus _signalBus;

    private LevelFailedPresenter _presenter;
    private int _lastLevelId;

    public LevelFailedState(GameLoopStateMachine stateMachine, DiContainer container,
        SceneLoader sceneLoader, SignalBus signalBus) : base(stateMachine)
    {
        _container = container;
        _sceneLoader = sceneLoader;
        _signalBus = signalBus;
    }

    public override void OnStateActivated()
    {
        Time.timeScale = 1f;

        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        var levelContext = sceneContainer.Resolve<LevelContext>();
        _lastLevelId = levelContext.Config.Id;

        var spawner = sceneContainer.Resolve<WaveSpawner>();
        spawner.Stop();

        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            Object.Destroy(e.gameObject);
        foreach (var p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            Object.Destroy(p.gameObject);

        _presenter = sceneContainer.Resolve<LevelFailedPresenter>();

        _presenter.Retry += OnRetry;
        _presenter.BackToMenu += OnBackToMenu;
        _presenter.ShowResult();
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_presenter != null)
        {
            _presenter.Retry -= OnRetry;
            _presenter.BackToMenu -= OnBackToMenu;
        }
        _presenter = null;
    }

    private void OnRetry()
    {
        _signalBus.Fire(new LevelStartRequestedSignal { LevelId = _lastLevelId });
        StateMachine.SetState(GameLoopStateMachine.State.LoadLevel);
    }

    private void OnBackToMenu()
    {
        _sceneLoader.LoadScene("Menu", () =>
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
    }
}
