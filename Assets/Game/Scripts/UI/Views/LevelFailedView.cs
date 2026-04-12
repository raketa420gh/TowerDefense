using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelFailedView : DisplayableView
{
    public event Action Retry;
    public event Action BackToMenu;

    [SerializeField]
    private Button _retryButton;

    [SerializeField]
    private Button _menuButton;

    protected override void Awake()
    {
        base.Awake();
        _retryButton.onClick.AddListener(() => Retry?.Invoke());
        _menuButton.onClick.AddListener(() => BackToMenu?.Invoke());
        Hide();
    }
}
