using UnityEngine;

[CreateAssetMenu(menuName = "Config/Enemy/ExplosionConfig")]
public class ExplosionConfig : ScriptableObject
{
    public float triggerRadius      = 2.5f;
    public float blastRadius        = 3f;
    public float damage             = 30f;
    public float countdownDuration  = 1.2f;
}
