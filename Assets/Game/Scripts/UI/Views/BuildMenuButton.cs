using System;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuButton : MonoBehaviour
{
    public event Action<TowerConfig> Clicked;

    public TowerConfig Config => _config;

    [SerializeField]
    private Button _button;

    [SerializeField]
    private Image _icon;

    [SerializeField]
    private Text _costLabel;

    private TowerConfig _config;

    private void Awake()
    {
        _button.onClick.AddListener(() => Clicked?.Invoke(_config));
    }

    public void Bind(TowerConfig config, bool affordable)
    {
        _config = config;
        _icon.sprite = config.Icon;
        _costLabel.text = config.Cost.ToString();
        _button.interactable = affordable;
    }

    public void SetInteractable(bool value)
    {
        _button.interactable = value;
    }
}
