using UnityEngine;

[RequireComponent(typeof(TowerAttack))]
public class Tower : MonoBehaviour
{
    public TowerConfig Config => _config;
    public TowerAttack Attack => _attack;

    private TowerConfig _config;
    private TowerAttack _attack;

    private void Awake()
    {
        _attack = GetComponent<TowerAttack>();
    }

    public void Init(TowerConfig config)
    {
        _config = config;
        _attack.Init(config);
    }
}
