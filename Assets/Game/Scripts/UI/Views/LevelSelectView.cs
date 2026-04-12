using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectView : DisplayableView
{
    public event Action<int> LevelSelected;
    public event Action BackClicked;

    [SerializeField]
    private LevelButton[] _buttons;

    [SerializeField]
    private Button _backButton;

    protected override void Awake()
    {
        base.Awake();
        foreach (var btn in _buttons)
            btn.Clicked += OnButtonClicked;
        _backButton.onClick.AddListener(() => BackClicked?.Invoke());
    }

    public void Refresh(PlayerProgress progress)
    {
        foreach (var btn in _buttons)
        {
            bool unlocked = btn.LevelId <= progress.UnlockedLevel;
            btn.Bind(unlocked, progress.GetStars(btn.LevelId));
        }
    }

    private void OnButtonClicked(int id) => LevelSelected?.Invoke(id);
}
