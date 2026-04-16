using UnityEngine;

[CreateAssetMenu(menuName = "Config/StaffCombatConfig")]
public class StaffCombatConfig : ScriptableObject
{
    public float detectionRadius  = 8f;
    public float fireRate         = 1f;
    public float projectileSpeed  = 12f;
    public float projectileDamage = 1f;
    public float bobAmplitude     = 0.15f;
    public float bobFrequency     = 1.5f;
    public float followSmoothTime = 0.12f;
    public Vector3 staffOffset    = new(1.2f, 1.0f, 0f);
}
