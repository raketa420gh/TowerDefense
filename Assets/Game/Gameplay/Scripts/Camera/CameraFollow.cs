using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    Transform _target;
    [SerializeField]
    Vector3 _offset = new(0, 10, -7);

    void LateUpdate() => transform.position = _target.position + _offset;
}
