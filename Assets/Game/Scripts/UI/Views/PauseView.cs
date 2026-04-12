using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseView : DisplayableView
{
    public event Action Resume;
    public event Action Restart;
    public event Action BackToMenu;

    [SerializeField]
    private Button _resumeButton;

    [SerializeField]
    private Button _restartButton;

    [SerializeField]
    private Button _menuButton;

    protected override void Awake()
    {
        base.Awake();
        if (_resumeButton != null) _resumeButton.onClick.AddListener(() => Resume?.Invoke());
        if (_restartButton != null) _restartButton.onClick.AddListener(() => Restart?.Invoke());
        if (_menuButton != null) _menuButton.onClick.AddListener(() => BackToMenu?.Invoke());
        Hide();
    }
}
