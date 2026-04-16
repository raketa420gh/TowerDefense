using UnityEngine;
using Zenject;

public class StaffFloatingBehaviour : MonoBehaviour
{
    private StaffCombatConfig _config;

    private Vector3 _localVelocity;

    [Inject]
    public void Construct(StaffCombatConfig config)
    {
        _config = config;
        transform.localPosition = _config.staffOffset;
    }

    private void Update()
    {
        var target = _config.staffOffset;
        target.y += Mathf.Sin(Time.time * _config.bobFrequency * Mathf.PI * 2f) * _config.bobAmplitude;

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition, target, ref _localVelocity, _config.followSmoothTime);
    }
}
