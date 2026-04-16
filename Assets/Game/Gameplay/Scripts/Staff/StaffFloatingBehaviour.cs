using UnityEngine;
using Zenject;

public class StaffFloatingBehaviour : MonoBehaviour
{
    StaffCombatConfig _config;
    Transform         _playerTransform;

    Vector3 _velocity;

    [Inject]
    public void Construct(StaffCombatConfig config, [Inject(Id = "PlayerTransform")] Transform playerTransform)
    {
        _config          = config;
        _playerTransform = playerTransform;
    }

    void Update()
    {
        var targetPos = _playerTransform.position
                      + _playerTransform.TransformDirection(_config.staffOffset)
                      + Vector3.up * Mathf.Sin(Time.time * _config.bobFrequency * Mathf.PI * 2f)
                                   * _config.bobAmplitude;

        transform.position = Vector3.SmoothDamp(
            transform.position, targetPos, ref _velocity, _config.followSmoothTime);
    }
}
