using UnityEngine;
using Zenject;

public class StaffFloatingBehaviour : MonoBehaviour
{
    private StaffCombatConfig _config;
    private Transform _parent;
    private Vector3 _localVelocity;

    [Inject]
    public void Construct(StaffCombatConfig config)
    {
        _config = config;
        _parent = transform.parent;
        transform.position = _parent.position + Offset;
    }

    private void Update()
    {
        var worldTarget = _parent.position + Offset;
        worldTarget.y += Mathf.Sin(Time.time * _config.bobFrequency * Mathf.PI * 2f) * _config.bobAmplitude;

        transform.position = Vector3.SmoothDamp(
            transform.position, worldTarget, ref _localVelocity, _config.followSmoothTime);

        transform.rotation = Quaternion.identity;
    }

    private Vector3 Offset
        => new(_config.distanceFromPlayer, _config.heightAbovePlayer, 0f);
}
