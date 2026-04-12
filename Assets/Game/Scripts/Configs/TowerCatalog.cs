using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/TowerCatalog", fileName = "TowerCatalog")]
public class TowerCatalog : ScriptableObject
{
    [SerializeField]
    private List<TowerConfig> _towers = new();

    public IReadOnlyList<TowerConfig> Towers => _towers;
}
