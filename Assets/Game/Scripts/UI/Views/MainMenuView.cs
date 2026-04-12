using System;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : DisplayableView
{
    public event Action PlayClicked;

    [SerializeField]
    private Button _playButton;

    [SerializeField]
    private Button _quitButton;

    protected override void Awake()
    {
        base.Awake();
        _playButton.onClick.AddListener(() => PlayClicked?.Invoke());
        _quitButton.onClick.AddListener(Application.Quit);
    }
}
