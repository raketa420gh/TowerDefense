using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public event Action OnDied;

    [SerializeField]
    float _maxHp;

    float _current;

    public void Initialize(float maxHp)
    {
        _maxHp   = maxHp;
        _current = maxHp;
    }

    public void TakeDamage(float amount)
    {
        _current -= amount;
        if (_current <= 0f) OnDied?.Invoke();
    }
}
