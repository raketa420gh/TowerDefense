using UnityEngine;

[CreateAssetMenu(menuName = "Config/ExperienceConfig")]
public class ExperienceConfig : ScriptableObject
{
    public int   baseXp       = 100;
    public float growthFactor = 1.4f;
    public int   xpPerKill   = 10;
}
