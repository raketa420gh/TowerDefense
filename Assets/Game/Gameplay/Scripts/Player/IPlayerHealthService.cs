using System;

public interface IPlayerHealthService
{
    float MaxHp        { get; }
    float CurrentHp    { get; }
    float NormalizedHp { get; }
    void  TakeDamage(float amount);
    void  Heal(float amount);
    void  ForceNotify();
    event Action<float> OnHpChanged;
    event Action        OnDied;
}
