using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCardView : MonoBehaviour
{
    public event Action<UpgradeDefinition> OnChosen;

    [SerializeField]
    private Image _icon;
    [SerializeField]
    private TMP_Text _title;
    [SerializeField]
    private TMP_Text _description;
    [SerializeField]
    private Button _button;

    private UpgradeDefinition _data;

    private void Awake() => _button.onClick.AddListener(() => OnChosen?.Invoke(_data));

    public void Render(UpgradeDefinition data)
    {
        _data             = data;
        _icon.sprite      = data.icon;
        _title.text       = data.title;
        _description.text = data.description;
    }
}
