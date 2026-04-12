using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public event Action Died;

    public int Current => _current;
    public bool IsDead => _current <= 0;

    private int _current;

    public void Init(int max)
    {
        _current = max;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        _current -= amount;
        if (_current <= 0) Died?.Invoke();
    }
}
