using System;

public interface IExperienceService
{
    public int   CurrentLevel   { get; }
    public int   CurrentXp      { get; }
    public int   XpForNextLevel { get; }
    public float NormalizedXp   { get; }

    public event Action<int> OnXpChanged;
    public event Action<int> OnLevelUp;
}
