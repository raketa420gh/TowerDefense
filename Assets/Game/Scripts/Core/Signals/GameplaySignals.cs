public struct EnemySpawnedSignal { public Enemy Enemy; }
public struct EnemyKilledSignal { public Enemy Enemy; public int Reward; }
public struct EnemyReachedBaseSignal { public int Damage; }
public struct BaseHealthChangedSignal { public int Current; public int Max; }
public struct BaseDestroyedSignal { }
public struct WaveStartedSignal { public int Index; }
public struct WaveCompletedSignal { public int Index; }
public struct AllWavesCompletedSignal { }
public struct LevelFailedSignal { }

public struct GoldChangedSignal { public int Current; }
public struct TowerBuiltSignal { public Tower Tower; }
public struct TowerSoldSignal { public Tower Tower; public int Refund; }
public struct ProjectileHitSignal { public Enemy Enemy; public int Damage; }
