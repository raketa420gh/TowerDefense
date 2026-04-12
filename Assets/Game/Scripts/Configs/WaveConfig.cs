using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/WaveConfig", fileName = "WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Serializable]
    public class SubWave
    {
        public EnemyConfig Enemy;
        public int Count = 1;
        public float Interval = 1f;
    }

    [SerializeField]
    private List<SubWave> _subWaves = new();

    [SerializeField]
    private float _delayAfter = 5f;

    public IReadOnlyList<SubWave> SubWaves => _subWaves;
    public float DelayAfter => _delayAfter;
}
