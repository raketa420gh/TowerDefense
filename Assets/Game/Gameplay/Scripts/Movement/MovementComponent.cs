using MagicStaff.Staff;
using UnityEngine;
using Zenject;

public class MovementComponent : MonoBehaviour
{
    private PlayerConfig       _config;
    private PlayerStatsService _stats;
    private Rigidbody          _rb;
    private Vector2            _inputDir;

    [Inject]
    public void Construct(PlayerConfig config, PlayerStatsService stats)
    {
        _config = config;
        _stats  = stats;
    }

    private void Awake() => _rb = GetComponent<Rigidbody>();

    public bool IsMoving => _inputDir.sqrMagnitude > 0.01f;

    public void SetInput(Vector2 dir) => _inputDir = dir.normalized;

    private float EffectiveMoveSpeed
        => _config.moveSpeed * (1f + _stats.GetBonus(StatType.MoveSpeed));

    private void FixedUpdate()
    {
        var move = new Vector3(_inputDir.x, 0, _inputDir.y) * EffectiveMoveSpeed;
        _rb.MovePosition(_rb.position + move * Time.fixedDeltaTime);

        if (move.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(move);
            _rb.rotation = Quaternion.RotateTowards(
                _rb.rotation, targetRot, _config.rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
