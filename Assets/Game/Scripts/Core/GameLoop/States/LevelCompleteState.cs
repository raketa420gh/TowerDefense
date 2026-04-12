using UnityEngine;
using Zenject;

public class LevelCompleteState : GameLoopState
{
    private readonly DiContainer _container;
    private readonly SceneLoader _sceneLoader;

    private LevelCompletePresenter _presenter;

    public LevelCompleteState(GameLoopStateMachine stateMachine, DiContainer container, SceneLoader sceneLoader)
        : base(stateMachine)
    {
        _container = container;
        _sceneLoader = sceneLoader;
    }

    public override void OnStateActivated()
    {
        Time.timeScale = 1f;

        var sceneContext = Object.FindFirstObjectByType<SceneContext>();
        var sceneContainer = sceneContext != null ? sceneContext.Container : _container;

        var resultService = sceneContainer.Resolve<LevelResultService>();
        var levelContext = sceneContainer.Resolve<LevelContext>();
        _presenter = sceneContainer.Resolve<LevelCompletePresenter>();

        var stars = resultService.FinalizeVictory();

        _presenter.Continue += OnContinue;
        _presenter.ShowResult(levelContext.Config.DisplayName, stars);

        Debug.Log($"[LevelCompleteState] stars={stars}");
    }

    public override void Update() { }

    public override void OnStateDisabled()
    {
        if (_presenter != null) _presenter.Continue -= OnContinue;
        _presenter = null;
    }

    private void OnContinue()
    {
        _presenter.Continue -= OnContinue;
        _sceneLoader.LoadScene("Menu", () =>
            StateMachine.SetState(GameLoopStateMachine.State.MainMenu));
    }
}
