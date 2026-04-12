using Zenject;

public class GameLoopStateMachine : StateMachineController<GameRoot, GameLoopStateMachine.State>
{
    public enum State
    {
        Initialize = 0,
        MainMenu = 1,
        LoadLevel = 2,
        Gameplay = 3,
        Pause = 4,
        LevelComplete = 5,
        LevelFailed = 6
    }

    private readonly DiContainer _container;

    public GameLoopStateMachine(DiContainer container)
    {
        _container = container;
    }

    protected override void RegisterStates()
    {
        RegisterState(_container.Instantiate<InitializeState>(new object[] { this }), State.Initialize);
        RegisterState(_container.Instantiate<MainMenuState>(new object[] { this }), State.MainMenu);
        RegisterState(_container.Instantiate<LoadLevelState>(new object[] { this }), State.LoadLevel);
        RegisterState(_container.Instantiate<GameplayState>(new object[] { this }), State.Gameplay);
        RegisterState(_container.Instantiate<LevelFailedState>(new object[] { this }), State.LevelFailed);
        RegisterState(_container.Instantiate<LevelCompleteState>(new object[] { this }), State.LevelComplete);
    }
}
