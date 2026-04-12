using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuView : DisplayableView
{
    public event Action<TowerConfig> TowerPicked;
    public event Action Dismissed;

    [SerializeField]
    private RectTransform _root;

    [SerializeField]
    private BuildMenuButton _buttonPrefab;

    [SerializeField]
    private Button _closeButton;

    private readonly List<BuildMenuButton> _spawned = new();

    protected override void Awake()
    {
        base.Awake();
        _closeButton.onClick.AddListener(() => Dismissed?.Invoke());
        Hide();
    }

    public void Populate(IReadOnlyList<TowerConfig> towers, int currentGold)
    {
        foreach (var b in _spawned) Destroy(b.gameObject);
        _spawned.Clear();

        foreach (var config in towers)
        {
            var btn = Instantiate(_buttonPrefab, _root);
            btn.Bind(config, currentGold >= config.Cost);
            btn.Clicked += OnClicked;
            _spawned.Add(btn);
        }
    }

    public void UpdateAffordability(int currentGold)
    {
        foreach (var b in _spawned)
            b.SetInteractable(currentGold >= b.Config.Cost);
    }

    private void OnClicked(TowerConfig config)
    {
        TowerPicked?.Invoke(config);
    }
}
