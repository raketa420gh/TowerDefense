using System;

public class EnemyDeathBus
{
    public event Action<int> OnEnemyKilled;

    public void Notify(int xp) => OnEnemyKilled?.Invoke(xp);
}
