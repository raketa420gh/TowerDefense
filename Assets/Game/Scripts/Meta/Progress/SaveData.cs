using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int UnlockedLevel = 1;
    public List<LevelStarsEntry> LevelStars = new();
}

[Serializable]
public class LevelStarsEntry
{
    public int LevelId;
    public int Stars;
}
