using UnityEngine;

[CreateAssetMenu(menuName = "Config/UpgradeLibraryConfig")]
public class UpgradeLibraryConfig : ScriptableObject
{
    [SerializeField]
    private UpgradeDefinition[] _upgrades;

    public UpgradeDefinition[] Upgrades => _upgrades;
}
