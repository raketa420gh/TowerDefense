using MagicStaff.Views;
using System;
using UnityEngine;
using UnityEngine.UI;

public class DefeatView : DisplayableView
{
    public event Action OnContinueClicked;

    [SerializeField]
    private Button _continueButton;

    private void Awake()    => _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());
    private void OnDestroy() => _continueButton.onClick.RemoveAllListeners();
}
