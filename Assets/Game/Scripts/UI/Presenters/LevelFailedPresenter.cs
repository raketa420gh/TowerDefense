using System;
using Zenject;

public class LevelFailedPresenter : IInitializable, IDisposable
{
    public event Action Retry;
    public event Action BackToMenu;

    private readonly LevelFailedView _view;

    public LevelFailedPresenter(LevelFailedView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        _view.Retry += OnRetry;
        _view.BackToMenu += OnBackToMenu;
        _view.Hide();
    }

    public void Dispose()
    {
        _view.Retry -= OnRetry;
        _view.BackToMenu -= OnBackToMenu;
    }

    public void ShowResult() => _view.Show();
    public void Hide() => _view.Hide();

    private void OnRetry() => Retry?.Invoke();
    private void OnBackToMenu() => BackToMenu?.Invoke();
}
