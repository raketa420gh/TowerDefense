using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "TD/LevelCatalog", fileName = "LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField]
    private LevelDefinition[] _levels;

    public LevelDefinition[] Levels => _levels;

    public LevelDefinition Get(int id) => _levels.First(l => l.Id == id);
}
