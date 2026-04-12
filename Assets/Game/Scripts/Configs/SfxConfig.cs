using UnityEngine;

[CreateAssetMenu(menuName = "TowerDefense/SfxConfig", fileName = "SfxConfig")]
public class SfxConfig : ScriptableObject
{
    [SerializeField]
    private AudioClip _shot;

    [SerializeField]
    private AudioClip _enemyDeath;

    [SerializeField]
    private AudioClip _towerBuilt;

    [SerializeField]
    private AudioClip _levelWin;

    [SerializeField]
    private AudioClip _levelFail;

    public AudioClip Shot => _shot;
    public AudioClip EnemyDeath => _enemyDeath;
    public AudioClip TowerBuilt => _towerBuilt;
    public AudioClip LevelWin => _levelWin;
    public AudioClip LevelFail => _levelFail;
}
