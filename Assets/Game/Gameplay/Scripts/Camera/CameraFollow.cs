using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private Vector3 _offset = new(0, 10, -7);

    private void LateUpdate() => transform.position = _target.position + _offset;
}
