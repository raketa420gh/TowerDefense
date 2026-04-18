using UnityEngine;
using Zenject;

public class ExperienceHudController : IInitializable, System.IDisposable
{
    private IExperienceService   _experience;
    private IUpgradeService      _upgradeService;
    private ExperienceBarView    _barView;
    private UpgradeSelectionView _selectionView;

    [Inject]
    public void Construct(
        IExperienceService   experience,
        IUpgradeService      upgradeService,
        ExperienceBarView    barView,
        UpgradeSelectionView selectionView)
    {
        _experience     = experience;
        _upgradeService = upgradeService;
        _barView        = barView;
        _selectionView  = selectionView;
    }

    public void Initialize()
    {
        _experience.OnXpChanged               += HandleXpChanged;
        _experience.OnLevelUp                 += HandleLevelUp;
        _upgradeService.OnUpgradeChoicesReady += HandleChoicesReady;
        _selectionView.OnUpgradeChosen        += HandleUpgradeChosen;

        _barView.SetLevel(_experience.CurrentLevel);
        _barView.SetProgress(0f);
        _selectionView.Hide();
    }

    public void Dispose()
    {
        _experience.OnXpChanged               -= HandleXpChanged;
        _experience.OnLevelUp                 -= HandleLevelUp;
        _upgradeService.OnUpgradeChoicesReady -= HandleChoicesReady;
        _selectionView.OnUpgradeChosen        -= HandleUpgradeChosen;
    }

    private void HandleXpChanged(int _)   => _barView.SetProgress(_experience.NormalizedXp);
    private void HandleLevelUp(int level) => _barView.SetLevel(level);

    private void HandleChoicesReady(UpgradeDefinition[] choices)
    {
        Time.timeScale = 0f;
        _selectionView.Present(choices);
    }

    private void HandleUpgradeChosen(UpgradeDefinition upgrade)
    {
        _upgradeService.ApplyUpgrade(upgrade);
        Time.timeScale = 1f;
    }
}
