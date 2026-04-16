using UnityEngine;
using Zenject;

public class MovementComponent : MonoBehaviour
{
    [Inject] PlayerConfig _config;

    Rigidbody _rb;
    Vector2   _inputDir;

    void Awake() => _rb = GetComponent<Rigidbody>();

    public bool IsMoving => _inputDir.sqrMagnitude > 0.01f;

    public void SetInput(Vector2 dir) => _inputDir = dir;

    void FixedUpdate()
    {
        var move = new Vector3(_inputDir.x, 0, _inputDir.y) * _config.moveSpeed;
        _rb.MovePosition(_rb.position + move * Time.fixedDeltaTime);

        if (move.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(move);
            _rb.rotation = Quaternion.RotateTowards(
                _rb.rotation, targetRot, _config.rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
