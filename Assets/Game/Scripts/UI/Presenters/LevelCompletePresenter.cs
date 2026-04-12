using System;
using Zenject;

public class LevelCompletePresenter : IInitializable, IDisposable
{
    public event Action Continue;

    private readonly LevelCompleteView _view;

    public LevelCompletePresenter(LevelCompleteView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        _view.Continue += OnContinue;
        _view.Hide();
    }

    public void Dispose()
    {
        _view.Continue -= OnContinue;
    }

    public void ShowResult(string levelName, int stars)
    {
        _view.Populate(levelName, stars);
        _view.Show();
    }

    public void Hide() => _view.Hide();

    private void OnContinue() => Continue?.Invoke();
}
