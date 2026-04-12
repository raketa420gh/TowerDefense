using System;
using UnityEngine;
using Zenject;

public class PausePresenter : IInitializable, IDisposable
{
    public event Action Resume;
    public event Action Restart;
    public event Action BackToMenu;

    private readonly PauseView _view;
    private readonly SignalBus _signalBus;

    private bool _isPaused;

    public PausePresenter([InjectOptional] PauseView view, SignalBus signalBus)
    {
        _view = view;
        _signalBus = signalBus;
    }

    public void Initialize()
    {
        if (_view != null)
        {
            _view.Resume += OnResume;
            _view.Restart += OnRestart;
            _view.BackToMenu += OnBackToMenu;
            _view.Hide();
        }
        _signalBus.Subscribe<PauseRequestedSignal>(OnPauseRequested);
    }

    public void Dispose()
    {
        if (_view != null)
        {
            _view.Resume -= OnResume;
            _view.Restart -= OnRestart;
            _view.BackToMenu -= OnBackToMenu;
        }
        _signalBus.TryUnsubscribe<PauseRequestedSignal>(OnPauseRequested);
        if (_isPaused) Time.timeScale = 1f;
    }

    public void ForceHide()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        if (_view != null) _view.Hide();
    }

    private void OnPauseRequested()
    {
        if (_isPaused || _view == null) return;
        _isPaused = true;
        Time.timeScale = 0f;
        _view.Show();
    }

    private void OnResume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        Time.timeScale = 1f;
        _view.Hide();
        _signalBus.Fire(new PauseResumedSignal());
        Resume?.Invoke();
    }

    private void OnRestart()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (_view != null) _view.Hide();
        Restart?.Invoke();
    }

    private void OnBackToMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (_view != null) _view.Hide();
        BackToMenu?.Invoke();
    }
}
