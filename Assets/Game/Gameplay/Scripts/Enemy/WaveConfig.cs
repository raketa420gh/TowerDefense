using UnityEngine;

[CreateAssetMenu(menuName = "Config/WaveConfig")]
public class WaveConfig : ScriptableObject
{
    [Tooltip("X = время игры (сек), Y = кол-во врагов в волне")]
    public AnimationCurve enemyCountOverTime = AnimationCurve.Linear(0, 3, 300, 30);

    [Tooltip("X = время игры (сек), Y = интервал между волнами (сек)")]
    public AnimationCurve spawnIntervalOverTime = AnimationCurve.Linear(0, 5, 300, 1.5f);

    [Tooltip("Разброс точки спавна вдоль стороны арены")]
    public float spawnEdgeVariance = 8f;

    [Tooltip("Отступ спавн-точки за пределы Floor")]
    public float spawnEdgeOffset = 2f;

    [Tooltip("Половина размера Floor по X и Z (для расчёта точки спавна)")]
    public float arenaHalfSize = 25f;
}
