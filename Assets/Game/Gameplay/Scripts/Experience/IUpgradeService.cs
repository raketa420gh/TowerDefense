using System;

public interface IUpgradeService
{
    public event Action<UpgradeDefinition[]> OnUpgradeChoicesReady;
    public void ApplyUpgrade(UpgradeDefinition upgrade);
}
