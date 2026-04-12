using System.Collections.Generic;

public class PlayerProgress
{
    private readonly PersistenceService _persistence;
    private SaveData _data;

    public int UnlockedLevel => _data.UnlockedLevel;

    public PlayerProgress(PersistenceService persistence)
    {
        _persistence = persistence;
    }

    public void Load()
    {
        _data = _persistence.Load<SaveData>("progress") ?? new SaveData();
    }

    public int GetStars(int levelId)
    {
        foreach (var entry in _data.LevelStars)
            if (entry.LevelId == levelId)
                return entry.Stars;
        return 0;
    }

    public void SetLevelResult(int levelId, int stars)
    {
        var existing = _data.LevelStars.Find(e => e.LevelId == levelId);
        if (existing != null)
            existing.Stars = System.Math.Max(existing.Stars, stars);
        else
            _data.LevelStars.Add(new LevelStarsEntry { LevelId = levelId, Stars = stars });

        if (stars > 0 && levelId + 1 > _data.UnlockedLevel)
            _data.UnlockedLevel = levelId + 1;

        Save();
    }

    public void Save() => _persistence.Save("progress", _data);
}
