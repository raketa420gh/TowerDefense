public struct EnemySpawnedSignal { public Enemy Enemy; }
public struct EnemyKilledSignal { public Enemy Enemy; public int Reward; }
public struct EnemyReachedBaseSignal { public int Damage; }
public struct BaseHealthChangedSignal { public int Current; public int Max; }
public struct BaseDestroyedSignal { }

public struct WaveStartedSignal { public int Index; public int Total; }
public struct WaveCompletedSignal { public int Index; public int Reward; }
public struct WaveBreakStartedSignal { public int NextIndex; public float Seconds; }
public struct AllWavesCompletedSignal { }

public struct LevelFailedSignal { }
public struct LevelCompletedSignal { public int LevelId; public int Stars; }

public struct GoldChangedSignal { public int Current; }
public struct TowerBuiltSignal { public Tower Tower; }
public struct TowerSoldSignal { public Tower Tower; public int Refund; }
public struct TowerUpgradedSignal { public Tower Tower; public int Level; }
public struct ProjectileHitSignal { public Enemy Enemy; public int Damage; }

public struct WaveEarlyStartRequestedSignal { }

public struct PauseRequestedSignal { }
public struct PauseResumedSignal { }
