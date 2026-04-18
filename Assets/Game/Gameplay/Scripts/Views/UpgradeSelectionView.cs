using System;
using MagicStaff.Views;
using UnityEngine;

public class UpgradeSelectionView : DisplayableView
{
    public event Action<UpgradeDefinition> OnUpgradeChosen;

    [SerializeField]
    private UpgradeCardView[] _cards;

    public void Present(UpgradeDefinition[] choices)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            _cards[i].OnChosen -= HandleChoice;
            _cards[i].Render(choices[i]);
            _cards[i].OnChosen += HandleChoice;
        }
        Show();
    }

    private void HandleChoice(UpgradeDefinition upgrade)
    {
        foreach (var c in _cards) c.OnChosen -= HandleChoice;
        OnUpgradeChosen?.Invoke(upgrade);
        Hide();
    }
}
