using UnityEngine;

[CreateAssetMenu(menuName = "Config/PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;
}
