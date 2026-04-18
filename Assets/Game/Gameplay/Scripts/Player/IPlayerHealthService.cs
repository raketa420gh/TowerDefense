using System;

public interface IPlayerHealthService
{
    float MaxHp        { get; }
    float CurrentHp    { get; }
    float NormalizedHp { get; }
    void  TakeDamage(float amount);
    event Action<float> OnHpChanged;
    event Action        OnDied;
}
