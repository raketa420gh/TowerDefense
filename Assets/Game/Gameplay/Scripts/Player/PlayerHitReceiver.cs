using UnityEngine;
using Zenject;

public class PlayerHitReceiver : MonoBehaviour
{
    private IPlayerHealthService _healthService;
    private PlayerConfig         _config;
    private float                _lastHitTime = float.MinValue;

    [Inject]
    public void Construct(IPlayerHealthService healthService, PlayerConfig config)
    {
        _healthService = healthService;
        _config        = config;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        TryDamage();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Enemy")) return;
        TryDamage();
    }

    private void TryDamage()
    {
        if (Time.time - _lastHitTime < _config.damageCooldown) return;
        _lastHitTime = Time.time;
        _healthService.TakeDamage(_config.enemyContactDamage);
    }
}
