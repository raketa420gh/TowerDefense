using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/LevelConfig", fileName = "LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [SerializeField]
    private int _id;

    [SerializeField]
    private string _displayName;

    [SerializeField]
    private string _sceneName;

    [SerializeField]
    private int _startingGold = 300;

    [SerializeField]
    private int _baseHealth = 20;

    [SerializeField]
    private List<WaveConfig> _waves = new();

    public int Id => _id;
    public string DisplayName => _displayName;
    public string SceneName => _sceneName;
    public int StartingGold => _startingGold;
    public int BaseHealth => _baseHealth;
    public IReadOnlyList<WaveConfig> Waves => _waves;
}
