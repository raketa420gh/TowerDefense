using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public event Action OnDied;
    public event Action OnDamaged;

    [SerializeField]
    private float _maxHp;

    private float _current;

    public void Initialize(float maxHp)
    {
        _maxHp   = maxHp;
        _current = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (_current <= 0f) return;
        _current -= amount;
        OnDamaged?.Invoke();
        if (_current <= 0f) OnDied?.Invoke();
    }
}
